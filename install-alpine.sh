#!/bin/sh
# install-alpine.sh — SagraFacile one-script installer for Alpine Linux
#
# Usage:
#   wget -qO- https://raw.githubusercontent.com/PrinceEntree8/SagraFacile/main/install-alpine.sh | sh
#   -- OR --
#   chmod +x install-alpine.sh && ./install-alpine.sh
#
# Options (environment variables):
#   DB_NAME          Database name          (default: sagrafacile)
#   DB_USER          Database user          (default: sagrafacile)
#   DB_PASS          Database password      (default: random 16-char)
#   APP_PORT         HTTP port for the app  (default: 8080)
#   ADMIN_USER       Seed admin username    (default: admin)
#   ADMIN_PASS       Seed admin password    (default: Admin@123!)
#   ADMIN_EMAIL      Seed admin e-mail      (default: admin@sagrafacile.local)
#   INSTALL_DIR      Install directory      (default: /opt/sagrafacile)
#   DOTNET_CHANNEL   .NET channel           (default: 10.0)
#
# Requirements:
#   - Alpine Linux 3.21+ (x86_64 / amd64)
#   - Root privileges
#
# What this script does:
#   1. Installs system dependencies
#   2. Checks for PostgreSQL; installs version 18 if absent
#   3. Creates the application database and user
#   4. Checks for ASP.NET Core Runtime 10; installs if absent
#   5. Downloads the latest SagraFacile linux-x64 self-contained release
#   6. Writes /etc/sagrafacile/sagrafacile.env (config file)
#   7. Installs and starts an OpenRC service
#
# The application auto-applies EF Core migrations and seeds the admin user on first start.

set -e

# ---------------------------------------------------------------------------
# Colour helpers
# ---------------------------------------------------------------------------
RED='\033[0;31m'; GRN='\033[0;32m'; YEL='\033[1;33m'; BLU='\033[0;34m'; NC='\033[0m'

info()    { printf "${BLU}[INFO]${NC}  %s\n" "$*"; }
success() { printf "${GRN}[OK]${NC}    %s\n" "$*"; }
warn()    { printf "${YEL}[WARN]${NC}  %s\n" "$*"; }
die()     { printf "${RED}[ERROR]${NC} %s\n" "$*" >&2; exit 1; }

# ---------------------------------------------------------------------------
# Sanity checks
# ---------------------------------------------------------------------------
if [ "$(id -u)" -ne 0 ]; then
    die "This script must be run as root. Try: sudo sh install-alpine.sh"
fi

if ! grep -qi "alpine" /etc/os-release 2>/dev/null; then
    die "This installer targets Alpine Linux only."
fi

ALPINE_VER=$(. /etc/os-release && echo "$VERSION_ID" | cut -d. -f1-2)
ALPINE_MAJOR=$(echo "$ALPINE_VER" | cut -d. -f1)
ALPINE_MINOR=$(echo "$ALPINE_VER" | cut -d. -f2)

if [ "$ALPINE_MAJOR" -lt 3 ] || { [ "$ALPINE_MAJOR" -eq 3 ] && [ "$ALPINE_MINOR" -lt 21 ]; }; then
    die "Alpine Linux 3.21 or later is required (detected: ${ALPINE_VER})."
fi

info "Detected Alpine Linux ${ALPINE_VER}"

# ---------------------------------------------------------------------------
# Configuration (with defaults)
# ---------------------------------------------------------------------------
DB_NAME="${DB_NAME:-sagrafacile}"
DB_USER="${DB_USER:-sagrafacile}"
# Generate a random password if not supplied
if [ -z "${DB_PASS:-}" ]; then
    DB_PASS="$(tr -dc 'A-Za-z0-9' </dev/urandom 2>/dev/null | head -c 24)" || \
    DB_PASS="$(cat /dev/urandom | env LC_ALL=C tr -dc 'A-Za-z0-9' | fold -w 24 | head -n 1)"
fi
APP_PORT="${APP_PORT:-8080}"
ADMIN_USER="${ADMIN_USER:-admin}"
ADMIN_PASS="${ADMIN_PASS:-Admin@123!}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@sagrafacile.local}"
INSTALL_DIR="${INSTALL_DIR:-/opt/sagrafacile}"
DOTNET_CHANNEL="${DOTNET_CHANNEL:-10.0}"

GITHUB_REPO="PrinceEntree8/SagraFacile"
ENV_FILE="/etc/sagrafacile/sagrafacile.env"
INIT_SCRIPT="/etc/init.d/sagrafacile"
LOG_DIR="/var/log/sagrafacile"

# ---------------------------------------------------------------------------
# Step 1 — System dependencies
# ---------------------------------------------------------------------------
info "Installing system dependencies..."
apk update --quiet
apk add --no-cache --quiet \
    curl ca-certificates \
    libstdc++ libgcc \
    icu-libs \
    krb5-libs \
    zlib \
    openssl
success "System dependencies installed."

# ---------------------------------------------------------------------------
# Step 2 — PostgreSQL 18
# ---------------------------------------------------------------------------
info "Checking PostgreSQL..."

PG_INSTALLED=false
PG_RUNNING=false

if command -v pg_isready >/dev/null 2>&1; then
    PG_INSTALLED=true
    PG_VERSION=$(pg_isready --version 2>/dev/null | grep -oE '[0-9]+' | head -1 || echo "unknown")
    info "PostgreSQL is already installed (pg_isready reports version family: ${PG_VERSION})."

    if pg_isready -q -t 3 2>/dev/null; then
        PG_RUNNING=true
        success "PostgreSQL server is running."
    else
        warn "PostgreSQL is installed but the server is not running. Will attempt to start it."
    fi
fi

if [ "$PG_INSTALLED" = false ]; then
    info "PostgreSQL not found. Installing PostgreSQL 18..."

    # Try the stable repo first; fall back to edge/main for pg18
    if apk info postgresql18 >/dev/null 2>&1 || \
       apk search --quiet postgresql18 2>/dev/null | grep -q "^postgresql18-"; then
        apk add --no-cache --quiet postgresql18 postgresql18-client
    else
        info "postgresql18 not in default repos. Trying Alpine edge/main..."
        apk add --no-cache --quiet \
            --repository https://dl-cdn.alpinelinux.org/alpine/edge/main \
            postgresql18 postgresql18-client
    fi

    success "PostgreSQL 18 installed."

    # Create data directory and initialise the cluster
    PGDATA="/var/lib/postgresql/data"
    if [ ! -f "${PGDATA}/PG_VERSION" ]; then
        info "Initialising PostgreSQL data directory..."
        mkdir -p "$PGDATA"
        chown -R postgres:postgres "$PGDATA"
        su -s /bin/sh postgres -c "initdb -D ${PGDATA} --auth-local=trust --auth-host=md5"
        success "PostgreSQL cluster initialised."
    else
        info "Existing data directory found — skipping initdb."
    fi

    # Enable and start via OpenRC
    rc-update add postgresql default 2>/dev/null || true
    rc-service postgresql start
    PG_RUNNING=true
    success "PostgreSQL 18 started and enabled."
fi

# Start PostgreSQL if installed but not running
if [ "$PG_INSTALLED" = true ] && [ "$PG_RUNNING" = false ]; then
    warn "Starting PostgreSQL..."
    rc-service postgresql start || die "Could not start PostgreSQL. Check: rc-service postgresql status"
    PG_RUNNING=true
    success "PostgreSQL started."
fi

# Wait for PG to accept connections
for i in $(seq 1 15); do
    pg_isready -q -t 2 && break
    info "Waiting for PostgreSQL to become ready... (${i}/15)"
    sleep 2
done
pg_isready -q || die "PostgreSQL did not become ready in time."

# ---------------------------------------------------------------------------
# Step 2b — Create database user and database
# ---------------------------------------------------------------------------
info "Provisioning database user '${DB_USER}' and database '${DB_NAME}'..."

# Create role (ignore error if already exists)
su -s /bin/sh postgres -c \
    "psql -c \"DO \\\$\\\$ BEGIN
       IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${DB_USER}') THEN
         CREATE USER ${DB_USER} WITH PASSWORD '${DB_PASS}';
       ELSE
         ALTER USER ${DB_USER} WITH PASSWORD '${DB_PASS}';
       END IF;
     END \\\$\\\$;\"" 2>&1 | grep -v "^DO$" || true

# Create database (ignore error if already exists)
su -s /bin/sh postgres -c \
    "psql -c \"SELECT 1 FROM pg_database WHERE datname='${DB_NAME}'\" \
     | grep -q 1 || \
     psql -c \"CREATE DATABASE ${DB_NAME} OWNER ${DB_USER};\"" 2>/dev/null || true

success "Database '${DB_NAME}' ready."

# ---------------------------------------------------------------------------
# Step 3 — ASP.NET Core Runtime 10
# ---------------------------------------------------------------------------
info "Checking ASP.NET Core Runtime ${DOTNET_CHANNEL}..."

DOTNET_OK=false

if command -v dotnet >/dev/null 2>&1; then
    if dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.AspNetCore.App ${DOTNET_CHANNEL}"; then
        DOTNET_OK=true
        success "ASP.NET Core Runtime ${DOTNET_CHANNEL} is already installed."
    else
        warn "dotnet found but ASP.NET Core ${DOTNET_CHANNEL} runtime is missing. Will install."
    fi
fi

if [ "$DOTNET_OK" = false ]; then
    # Try the Microsoft package feed for Alpine
    MS_PKG_URL="https://packages.microsoft.com/config/alpine/${ALPINE_VER}/packages-microsoft-prod.apk"

    info "Attempting to install ASP.NET Core Runtime via Microsoft package feed..."
    if curl -sSLf "$MS_PKG_URL" -o /tmp/microsoft-prod.apk 2>/dev/null; then
        apk add --allow-untrusted --quiet /tmp/microsoft-prod.apk
        rm -f /tmp/microsoft-prod.apk
        apk update --quiet
        if apk add --no-cache --quiet aspnetcore-runtime-10.0 2>/dev/null; then
            DOTNET_OK=true
            success "ASP.NET Core Runtime ${DOTNET_CHANNEL} installed via Microsoft package feed."
        fi
    fi

    # Fallback: dotnet-install.sh (works on any Alpine with musl)
    if [ "$DOTNET_OK" = false ]; then
        info "Package feed install failed. Falling back to dotnet-install.sh..."
        DOTNET_INSTALL_DIR="/usr/share/dotnet"
        curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
        chmod +x /tmp/dotnet-install.sh
        /tmp/dotnet-install.sh \
            --runtime aspnetcore \
            --channel "$DOTNET_CHANNEL" \
            --install-dir "$DOTNET_INSTALL_DIR" \
            --no-path
        rm -f /tmp/dotnet-install.sh
        ln -sf "${DOTNET_INSTALL_DIR}/dotnet" /usr/local/bin/dotnet
        if dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.AspNetCore.App ${DOTNET_CHANNEL}"; then
            DOTNET_OK=true
            success "ASP.NET Core Runtime ${DOTNET_CHANNEL} installed via dotnet-install.sh."
        else
            warn "ASP.NET Core Runtime installation could not be verified, but continuing."
            warn "The self-contained binary does not require a separate runtime on the host."
        fi
    fi
fi

# ---------------------------------------------------------------------------
# Step 4 — Download latest SagraFacile release
# ---------------------------------------------------------------------------
info "Fetching latest SagraFacile release from GitHub..."

LATEST_TAG="$(curl -sSL \
    "https://api.github.com/repos/${GITHUB_REPO}/releases/latest" \
    2>/dev/null | grep '"tag_name"' | head -1 | cut -d'"' -f4)"

if [ -z "$LATEST_TAG" ]; then
    die "Could not determine the latest release tag. Check your internet connection and try again."
fi

info "Latest release: ${LATEST_TAG}"

ARCHIVE="sagrafacile-${LATEST_TAG}-linux-x64.tar.gz"
DOWNLOAD_URL="https://github.com/${GITHUB_REPO}/releases/download/${LATEST_TAG}/${ARCHIVE}"

info "Downloading ${ARCHIVE}..."
curl -sSL "$DOWNLOAD_URL" -o /tmp/sagrafacile.tar.gz || \
    die "Failed to download ${DOWNLOAD_URL}"

info "Extracting to ${INSTALL_DIR}..."
mkdir -p "$INSTALL_DIR"
tar -xzf /tmp/sagrafacile.tar.gz -C "$INSTALL_DIR"
rm -f /tmp/sagrafacile.tar.gz
chmod +x "${INSTALL_DIR}/SagraFacile.Web"
success "SagraFacile ${LATEST_TAG} installed to ${INSTALL_DIR}."

# ---------------------------------------------------------------------------
# Step 5 — Write configuration file
# ---------------------------------------------------------------------------
info "Writing configuration to ${ENV_FILE}..."
mkdir -p /etc/sagrafacile

# Generate a stable JWT key
JWT_KEY="$(openssl rand -base64 32 2>/dev/null || \
           tr -dc 'A-Za-z0-9+/=' </dev/urandom | head -c 44)"

cat > "$ENV_FILE" <<EOF
# SagraFacile configuration — generated by install-alpine.sh
# Edit this file and run: rc-service sagrafacile restart

# ---- Database ----
ConnectionStrings__DefaultConnection=Host=127.0.0.1;Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASS}

# ---- Web server ----
ASPNETCORE_URLS=http://+:${APP_PORT}
ASPNETCORE_ENVIRONMENT=Production
AllowHttp=true

# ---- JWT (do NOT change after first start; changing it invalidates all sessions) ----
Jwt__Key=${JWT_KEY}
Jwt__Issuer=SagraFacile
Jwt__Audience=SagraFacile

# ---- Seed admin (used only when no admin user exists in the database) ----
Seed__AdminUsername=${ADMIN_USER}
Seed__AdminPassword=${ADMIN_PASS}
Seed__AdminEmail=${ADMIN_EMAIL}
EOF

chmod 600 "$ENV_FILE"
success "Configuration written."

# ---------------------------------------------------------------------------
# Step 6 — OpenRC service
# ---------------------------------------------------------------------------
info "Installing OpenRC service..."

cat > "$INIT_SCRIPT" <<EOFINIT
#!/sbin/openrc-run

name="SagraFacile"
description="SagraFacile — sagra/festa management web application"
command="${INSTALL_DIR}/SagraFacile.Web"
command_background=true
pidfile="/run/\${RC_SVCNAME}.pid"
output_log="${LOG_DIR}/app.log"
error_log="${LOG_DIR}/app.log"
env_file="${ENV_FILE}"

depend() {
    need net postgresql
    after postgresql
}

start_pre() {
    mkdir -p "${LOG_DIR}"
    checkpath --directory --owner root:root --mode 0755 "${LOG_DIR}"
}
EOFINIT

chmod +x "$INIT_SCRIPT"

mkdir -p "$LOG_DIR"
rc-update add sagrafacile default

info "Starting SagraFacile service..."
rc-service sagrafacile start

success "SagraFacile service started and enabled."

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
APP_URL="http://$(hostname -I 2>/dev/null | awk '{print $1}' || echo "localhost"):${APP_PORT}"

cat <<SUMMARY

${GRN}═══════════════════════════════════════════════════════════════${NC}
${GRN}  SagraFacile installed successfully!${NC}
${GRN}═══════════════════════════════════════════════════════════════${NC}

  Application URL : ${BLU}${APP_URL}${NC}
  Install dir     : ${INSTALL_DIR}
  Config file     : ${ENV_FILE}
  Log file        : ${LOG_DIR}/app.log
  Version         : ${LATEST_TAG}

  Admin login     : ${ADMIN_USER} / ${ADMIN_PASS}
  DB name         : ${DB_NAME}
  DB user         : ${DB_USER}
  DB password     : ${DB_PASS}

${YEL}  ⚠  Save the credentials above — they will not be shown again.${NC}
${YEL}  ⚠  Change the admin password after first login.${NC}

  Useful commands:
    rc-service sagrafacile status   — check service status
    rc-service sagrafacile restart  — restart after config changes
    tail -f ${LOG_DIR}/app.log   — follow application log

${GRN}═══════════════════════════════════════════════════════════════${NC}
SUMMARY
