#!/usr/bin/env bash
#
# perf-collect.sh — sample server metrics into a report directory.
#
# Usage: perf-collect.sh <output-dir> [interval-seconds]
#
# Every interval (default 5s) appends:
#   - docker-stats.jsonl : one JSON object per sample with per-container
#                          name/cpu/mem/net/block/pids snapshots
#   - pg-activity.csv    : timestamp,total,active,idle,idle_in_txn,waiting,other
#                          from pg_stat_activity (best effort)
#
# Postgres credentials come ONLY from the environment (no hardcoded secrets):
#   POSTGRES_USER, POSTGRES_DB (defaults match docker-compose.yml service env)
#   PGPASSWORD     (standard libpq variable; never hardcode a password here)
# The query runs via `docker compose exec -T postgres psql`; if it fails
# (e.g. auth not configured), the collector logs a warning once and keeps
# collecting docker stats.
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${ROOT}"

OUT_DIR="${1:-}"
INTERVAL="${2:-5}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"
POSTGRES_USER="${POSTGRES_USER:-sagrafacile}"
POSTGRES_DB="${POSTGRES_DB:-sagrafacile}"

[ -n "${OUT_DIR}" ] || { echo "Usage: perf-collect.sh <output-dir> [interval-seconds]" >&2; exit 2; }
mkdir -p "${OUT_DIR}"

STATS_FILE="${OUT_DIR}/docker-stats.jsonl"
PG_FILE="${OUT_DIR}/pg-activity.csv"

[ -f "${PG_FILE}" ] || echo "timestamp,total,active,idle,idle_in_transaction,waiting,other" > "${PG_FILE}"

log() { printf '[perf-collect] %s\n' "$*"; }
log "Writing docker stats to ${STATS_FILE} and pg activity to ${PG_FILE} every ${INTERVAL}s."

pg_warned=0
trap 'log "collector stopping."; exit 0' TERM INT

while true; do
  ts="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

  # --- docker stats snapshot (one JSON object per sample) ---
  if command -v docker >/dev/null 2>&1; then
    snapshot="$(docker stats --no-stream \
      --format '{"container":"{{.Name}}","cpu":"{{.CPUPerc}}","mem_usage":"{{.MemUsage}}","mem_perc":"{{.MemPerc}}","net":"{{.NetIO}}","block":"{{.BlockIO}}","pids":"{{.PIDs}}"}' 2>/dev/null || true)"
    if [ -n "${snapshot}" ]; then
      # shellcheck disable=SC2001
      containers="$(echo "${snapshot}" | sed 's/$/,/' | tr -d '\n' | sed 's/,$//')"
      printf '{"timestamp":"%s","containers":[%s]}\n' "${ts}" "${containers}" >> "${STATS_FILE}"
    else
      printf '{"timestamp":"%s","containers":[],"warning":"docker stats unavailable"}\n' "${ts}" >> "${STATS_FILE}"
    fi
  else
    printf '{"timestamp":"%s","containers":[],"warning":"docker CLI missing"}\n' "${ts}" >> "${STATS_FILE}"
  fi

  # --- pg_stat_activity counts (best effort) ---
  pg_row=""
  if command -v docker >/dev/null 2>&1 && [ -f "${COMPOSE_FILE}" ]; then
    pg_row="$(COMPOSE_FILE="${COMPOSE_FILE}" docker compose exec -T postgres \
      psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" -At -c \
      "SELECT count(*), count(*) FILTER (WHERE state='active'), count(*) FILTER (WHERE state='idle'), count(*) FILTER (WHERE state='idle in transaction'), count(*) FILTER (WHERE wait_event_type IS NOT NULL), count(*) FILTER (WHERE state NOT IN ('active','idle','idle in transaction') OR state IS NULL);" 2>/dev/null | tr -d ' \r\n' || true)"
  fi
  if [ -n "${pg_row}" ]; then
    # psql -At separates columns with '|' -> convert to CSV
    echo "${ts},$(echo "${pg_row}" | tr '|' ',')" >> "${PG_FILE}"
  else
    if [ "${pg_warned}" = "0" ]; then
      log "WARN: pg_stat_activity query failed (check POSTGRES_USER/POSTGRES_DB/PGPASSWORD env). Continuing with docker stats only."
      pg_warned=1
    fi
    echo "${ts},,,,,," >> "${PG_FILE}"
  fi

  sleep "${INTERVAL}"
done
