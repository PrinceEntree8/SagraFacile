#!/usr/bin/env bash
#
# perf-prepare.sh — ensure the perf stack is up and warm.
#
# - Verifies docker + compose availability.
# - Runs `docker compose up -d` (compose file overridable via COMPOSE_FILE).
# - Waits for the web app to answer HTTP 200 on / (timeout overridable).
# - Warms up / and /menu with a few curl passes.
#
# All configuration via environment (no hardcoded secrets):
#   BASE_URL        default http://localhost:5000
#   COMPOSE_FILE    default docker-compose.yml
#   WAIT_TIMEOUT_S  default 180
#   WARMUP_ROUNDS   default 3
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${ROOT}"

BASE_URL="${BASE_URL:-http://localhost:5000}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"
WAIT_TIMEOUT_S="${WAIT_TIMEOUT_S:-180}"
WARMUP_ROUNDS="${WARMUP_ROUNDS:-3}"

log() { printf '[perf-prepare] %s\n' "$*"; }
die() { printf '[perf-prepare] ERROR: %s\n' "$*" >&2; exit 1; }

command -v docker >/dev/null 2>&1 || die "docker CLI not found in PATH"
docker info >/dev/null 2>&1 || die "'docker info' failed — is the Docker daemon running?"
docker compose version >/dev/null 2>&1 || die "'docker compose' plugin not available"
command -v curl >/dev/null 2>&1 || die "curl not found in PATH"
[ -f "${COMPOSE_FILE}" ] || die "compose file '${COMPOSE_FILE}' not found (set COMPOSE_FILE=...)"

log "Using compose file: ${COMPOSE_FILE}"
log "Using base URL: ${BASE_URL}"

log "Starting stack (docker compose up -d)..."
COMPOSE_FILE="${COMPOSE_FILE}" docker compose up -d
log "Container status:"
COMPOSE_FILE="${COMPOSE_FILE}" docker compose ps || true

log "Waiting for ${BASE_URL}/ to return HTTP 200 (timeout ${WAIT_TIMEOUT_S}s)..."
deadline=$((SECONDS + WAIT_TIMEOUT_S))
ready=0
while [ "${SECONDS}" -lt "${deadline}" ]; do
  code="$(curl -s -o /dev/null -w '%{http_code}' --max-time 10 "${BASE_URL}/" || true)"
  if [ "${code}" = "200" ]; then
    ready=1
    break
  fi
  sleep 5
done
[ "${ready}" = "1" ] || die "web app did not become ready at ${BASE_URL}/ within ${WAIT_TIMEOUT_S}s"
log "Web app is ready."

log "Warming up / and /menu (${WARMUP_ROUNDS} rounds)..."
for i in $(seq 1 "${WARMUP_ROUNDS}"); do
  curl -s -o /dev/null --max-time 30 "${BASE_URL}/" || log "WARN: warmup GET / failed (round ${i})"
  curl -s -o /dev/null --max-time 30 "${BASE_URL}/menu" || log "WARN: warmup GET /menu failed (round ${i})"
done
log "Warmup done."
