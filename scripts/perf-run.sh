#!/usr/bin/env bash
#
# perf-run.sh — orchestrate a perf run: prepare stack, collect server metrics,
# run NBomber load + Playwright vitals sampling in parallel, save everything
# under reports/perf-YYYYMMDD-HHMM/.
#
# All configuration via environment (no hardcoded secrets):
#   BASE_URL       default http://localhost:5000
#   COMPOSE_FILE   default docker-compose.yml
#   REPORTS_BASE   default reports
#   SAMPLE_SECS    metrics sampling interval, default 5
#   NBOMBER_CMD    override for the NBomber invocation (default auto-detected)
#   VITALS_CMD     override for the Playwright vitals invocation (default auto-detected)
#   SKIP_PREPARE   set to 1 to skip perf-prepare.sh (stack already up + warm)
#   SMOKETEST__*   passed through to the NBomber smoke/perf host (base URL, etc.)
#
# Exit code is non-zero if either workload fails; the report dir is kept either way.
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${ROOT}"

BASE_URL="${BASE_URL:-http://localhost:5000}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"
REPORTS_BASE="${REPORTS_BASE:-reports}"
SAMPLE_SECS="${SAMPLE_SECS:-5}"
SKIP_PREPARE="${SKIP_PREPARE:-0}"

log() { printf '[perf-run] %s\n' "$*"; }
die() { printf '[perf-run] ERROR: %s\n' "$*" >&2; exit 1; }

# --- 1. Prepare stack (compose up + readiness + warmup) ---
if [ "${SKIP_PREPARE}" != "1" ]; then
  log "Preparing stack..."
  BASE_URL="${BASE_URL}" COMPOSE_FILE="${COMPOSE_FILE}" bash scripts/perf-prepare.sh
else
  log "SKIP_PREPARE=1 — assuming stack is already up and warm."
fi

# --- 2. Report directory ---
STAMP="$(date +%Y%m%d-%H%M)"
REPORT_DIR="${REPORTS_BASE}/perf-${STAMP}"
mkdir -p "${REPORT_DIR}"
log "Report directory: ${REPORT_DIR}"

# --- 3. Start metrics collector in background ---
log "Starting metrics collector (interval ${SAMPLE_SECS}s)..."
COMPOSE_FILE="${COMPOSE_FILE}" bash scripts/perf-collect.sh "${REPORT_DIR}" "${SAMPLE_SECS}" \
  > "${REPORT_DIR}/perf-collect.log" 2>&1 &
COLLECT_PID=$!
cleanup_collector() {
  if kill -0 "${COLLECT_PID}" 2>/dev/null; then
    kill "${COLLECT_PID}" 2>/dev/null || true
    wait "${COLLECT_PID}" 2>/dev/null || true
  fi
}
trap cleanup_collector EXIT
log "Collector PID: ${COLLECT_PID}"

# --- 4. Resolve workload commands (overridable) ---
# NBomber: teammate-owned perf profile under tests/SagraFacile.Tests.Smoke
# (appsettings.perf.json / DOTNET_ENVIRONMENT=Perf wiring). Fall back to the
# plain smoke host with SMOKETEST__ env overrides if no perf profile exists yet.
resolve_nbomber_cmd() {
  if [ -n "${NBOMBER_CMD:-}" ]; then
    echo "${NBOMBER_CMD}"
    return
  fi
  local proj="tests/SagraFacile.Tests.Smoke"
  if [ -f "${proj}/appsettings.perf.json" ]; then
    echo "SMOKETEST__SmokeTest__BaseUrl=${BASE_URL} DOTNET_ENVIRONMENT=Perf dotnet run --project ${proj} -c Release"
  elif [ -d "${proj}" ]; then
    echo "SMOKETEST__SmokeTest__BaseUrl=${BASE_URL} dotnet run --project ${proj} -c Release"
  else
    echo ""
  fi
}

# Playwright vitals: teammate-owned harness; probe well-known locations.
resolve_vitals_cmd() {
  if [ -n "${VITALS_CMD:-}" ]; then
    echo "${VITALS_CMD}"
    return
  fi
  for candidate in \
    "tests/browser/playwright.perf.config.ts" \
    "tests/SagraFacile.Tests.Vitals/vitals.mjs" \
    "tests/SagraFacile.Tests.Vitals/vitals.js" \
    "tests/SagraFacile.Tests.Vitals/vitals.ts" \
    "tests/vitals/vitals.mjs" \
    "tests/perf/vitals.mjs"; do
    if [ -f "${candidate}" ]; then
      if [ "${candidate}" = "tests/browser/playwright.perf.config.ts" ]; then
        echo "BASE_URL=${BASE_URL} npm --prefix tests/browser run perf:vitals"
        return
      fi
      echo "BASE_URL=${BASE_URL} node ${candidate}"
      return
    fi
  done
  echo ""
}

NBOMBER_CMD_RESOLVED="$(resolve_nbomber_cmd)"
VITALS_CMD_RESOLVED="$(resolve_vitals_cmd)"

[ -n "${NBOMBER_CMD_RESOLVED}" ] || log "WARN: no NBomber project found — skipping load workload."
[ -n "${VITALS_CMD_RESOLVED}" ] || log "WARN: no Playwright vitals harness found — skipping vitals workload."
if [ -z "${NBOMBER_CMD_RESOLVED}" ] && [ -z "${VITALS_CMD_RESOLVED}" ]; then
  die "nothing to run: set NBOMBER_CMD and/or VITALS_CMD explicitly."
fi

echo "${NBOMBER_CMD_RESOLVED}" > "${REPORT_DIR}/nbomber.cmd.txt" 2>/dev/null || true
echo "${VITALS_CMD_RESOLVED}" > "${REPORT_DIR}/vitals.cmd.txt" 2>/dev/null || true

# --- 5. Run both workloads in parallel ---
nbomber_status=0
vitals_status=0

if [ -n "${NBOMBER_CMD_RESOLVED}" ]; then
  log "Starting NBomber workload in background..."
  ( bash -c "${NBOMBER_CMD_RESOLVED}" > "${REPORT_DIR}/nbomber.log" 2>&1; echo "$?" > "${REPORT_DIR}/nbomber.exit" ) &
  NBOMBER_PID=$!
else
  NBOMBER_PID=""
fi

if [ -n "${VITALS_CMD_RESOLVED}" ]; then
  log "Starting Playwright vitals workload in background..."
  ( bash -c "${VITALS_CMD_RESOLVED}" > "${REPORT_DIR}/vitals.log" 2>&1; echo "$?" > "${REPORT_DIR}/vitals.exit" ) &
  VITALS_PID=$!
else
  VITALS_PID=""
fi

if [ -n "${NBOMBER_PID}" ]; then
  wait "${NBOMBER_PID}" || true
  nbomber_status="$(cat "${REPORT_DIR}/nbomber.exit" 2>/dev/null || echo 1)"
  log "NBomber finished with exit code ${nbomber_status}."
fi
if [ -n "${VITALS_PID}" ]; then
  wait "${VITALS_PID}" || true
  vitals_status="$(cat "${REPORT_DIR}/vitals.exit" 2>/dev/null || echo 1)"
  log "Vitals finished with exit code ${vitals_status}."
fi

# --- 6. Stop collector, summarize ---
cleanup_collector
trap - EXIT

{
  echo "# perf run ${STAMP}"
  echo ""
  echo "- base_url: ${BASE_URL}"
  echo "- compose_file: ${COMPOSE_FILE}"
  echo "- nbomber_exit: ${nbomber_status}"
  echo "- vitals_exit: ${vitals_status}"
  echo ""
  echo "Files: nbomber.log, vitals.log, docker-stats.jsonl, pg-activity.csv, perf-collect.log"
} > "${REPORT_DIR}/summary.md"

log "Done. Report: ${REPORT_DIR} (nbomber=${nbomber_status}, vitals=${vitals_status})"

if [ "${nbomber_status}" != "0" ] || [ "${vitals_status}" != "0" ]; then
  exit 1
fi
