# SagraFacile — Alpine Linux Installation Guide

This guide describes how to install SagraFacile as a native service on **Alpine Linux 3.21+**.  
A one-script installer (`install-alpine.sh` in the repository root) automates all the steps below.

---

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| Alpine Linux | 3.21 or later | amd64 / x86_64 |
| PostgreSQL | 18 | Installed automatically if missing |
| ASP.NET Core Runtime | 10.0 | Installed automatically if missing |
| Root / sudo access | — | Script must run as root |

---

## Architecture overview

SagraFacile ships as a **self-contained single-file executable** for `linux-x64`.  
The .NET runtime is bundled inside the binary, so the application itself needs no external
`dotnet` installation. However, the ASP.NET Core Runtime 10 is still installed on the host
because it provides shared native libraries (ICU, OpenSSL bindings) and lets you run
additional .NET tools (e.g., `dotnet ef` for manual migrations).

```
Alpine Linux host
├── PostgreSQL 18        — database server (OpenRC service)
├── ASP.NET Core 10      — shared libraries + dotnet tooling
└── SagraFacile          — self-contained binary (OpenRC service)
         /opt/sagrafacile/SagraFacile.Web
         /etc/sagrafacile/sagrafacile.env   (configuration)
         /var/log/sagrafacile/              (log output)
```

---

## Manual installation steps

### 1. System update

```sh
apk update && apk upgrade
apk add --no-cache curl ca-certificates libstdc++ libgcc icu-libs krb5-libs zlib openssl
```

### 2. Install PostgreSQL 18

PostgreSQL 18 is available in the Alpine edge/main repository. Check your version:

```sh
apk search postgresql18
```

If the package is not found in the default repositories, enable edge/main temporarily:

```sh
# Temporary edge overlay (does not change your default repos)
apk add --no-cache --repository https://dl-cdn.alpinelinux.org/alpine/edge/main \
    postgresql18 postgresql18-client
```

#### Initialise and start the database server

```sh
# Initialise the data directory (first run only)
mkdir -p /var/lib/postgresql/data
chown postgres:postgres /var/lib/postgresql/data
su -s /bin/sh postgres -c "initdb -D /var/lib/postgresql/data"

# Enable and start via OpenRC
rc-update add postgresql default
rc-service postgresql start

# Verify
pg_isready -U postgres
```

#### Create the application database and user

```sh
su -s /bin/sh postgres -c "psql -c \"CREATE USER sagrafacile WITH PASSWORD 'changeme';\"" 2>/dev/null || true
su -s /bin/sh postgres -c "psql -c \"CREATE DATABASE sagrafacile OWNER sagrafacile;\"" 2>/dev/null || true
```

> **Security note:** Replace `changeme` with a strong random password and record it in
> `/etc/sagrafacile/sagrafacile.env` (see §5).

---

### 3. Install ASP.NET Core Runtime 10

Microsoft distributes the .NET runtime for Alpine (musl libc) via a dedicated package feed.

```sh
# Download and install the Microsoft package source
ALPINE_VER=$(. /etc/os-release && echo "$VERSION_ID" | cut -d. -f1-2)
curl -sSL "https://packages.microsoft.com/config/alpine/${ALPINE_VER}/packages-microsoft-prod.apk" \
    -o /tmp/microsoft-prod.apk
apk add --allow-untrusted /tmp/microsoft-prod.apk
rm /tmp/microsoft-prod.apk

# Install the runtime
apk update
apk add --no-cache aspnetcore-runtime-10.0

# Verify
dotnet --list-runtimes | grep "Microsoft.AspNetCore.App 10"
```

If the Microsoft feed does not carry .NET 10 for your Alpine version, use the official
`dotnet-install.sh` script as a fallback:

```sh
curl -sSL https://dot.net/v1/dotnet-install.sh | sh -s -- \
    --runtime aspnetcore --channel 10.0 --install-dir /usr/share/dotnet
ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
```

---

### 4. Download SagraFacile

Fetch the latest `linux-x64` release archive from GitHub:

```sh
# Replace X.Y.Z with the desired version tag (e.g. v1.0.0)
VERSION="$(curl -sSL https://api.github.com/repos/PrinceEntree8/SagraFacile/releases/latest \
    | grep '"tag_name"' | head -1 | cut -d'"' -f4)"

curl -sSL \
    "https://github.com/PrinceEntree8/SagraFacile/releases/download/${VERSION}/sagrafacile-${VERSION}-linux-x64.tar.gz" \
    -o /tmp/sagrafacile.tar.gz

mkdir -p /opt/sagrafacile
tar -xzf /tmp/sagrafacile.tar.gz -C /opt/sagrafacile
rm /tmp/sagrafacile.tar.gz

chmod +x /opt/sagrafacile/SagraFacile.Web
```

---

### 5. Configure the application

Create the environment file that the OpenRC service will load:

```sh
mkdir -p /etc/sagrafacile
cat > /etc/sagrafacile/sagrafacile.env <<'EOF'
# Database
ConnectionStrings__DefaultConnection=Host=127.0.0.1;Port=5432;Database=sagrafacile;Username=sagrafacile;Password=changeme

# Web server
ASPNETCORE_URLS=http://+:8080
ASPNETCORE_ENVIRONMENT=Production
AllowHttp=true

# JWT — CHANGE THIS to a random 32-byte base64 string
Jwt__Key=CHANGE_ME_RANDOM_32_BYTE_BASE64_STRING==
Jwt__Issuer=SagraFacile
Jwt__Audience=SagraFacile

# Seed admin credentials (used only on first run)
Seed__AdminUsername=admin
Seed__AdminPassword=Admin@123!
Seed__AdminEmail=admin@sagrafacile.local
EOF
chmod 600 /etc/sagrafacile/sagrafacile.env
```

> **Important:** Set `Jwt__Key` to a stable secret. If left empty the app auto-generates one per
> restart, invalidating all active sessions on every reboot.
>
> Generate a secure key:
> ```sh
> openssl rand -base64 32
> ```

---

### 6. Create an OpenRC service

Alpine uses **OpenRC**, not systemd. Create a service script:

```sh
cat > /etc/init.d/sagrafacile <<'EOF'
#!/sbin/openrc-run

name="SagraFacile"
description="SagraFacile web application"
command="/opt/sagrafacile/SagraFacile.Web"
command_background=true
pidfile="/run/${RC_SVCNAME}.pid"
output_log="/var/log/sagrafacile/app.log"
error_log="/var/log/sagrafacile/app.log"
env_file="/etc/sagrafacile/sagrafacile.env"

depend() {
    need net postgresql
    after postgresql
}

start_pre() {
    mkdir -p /var/log/sagrafacile
}
EOF
chmod +x /etc/init.d/sagrafacile
```

Enable and start the service:

```sh
mkdir -p /var/log/sagrafacile
rc-update add sagrafacile default
rc-service sagrafacile start
```

---

### 7. Verify

```sh
# Service status
rc-service sagrafacile status

# Application log (migrations + seeding run at startup)
tail -f /var/log/sagrafacile/app.log

# HTTP check
curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/
```

The application runs on port **8080** by default. Point a reverse proxy (nginx, caddy) or
firewall rule at it to expose on port 80/443.

---

## Reverse proxy with nginx (optional)

```sh
apk add --no-cache nginx
```

`/etc/nginx/http.d/sagrafacile.conf`:

```nginx
server {
    listen 80;
    server_name _;

    location / {
        proxy_pass         http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade           $http_upgrade;
        proxy_set_header   Connection        "upgrade";
        proxy_set_header   Host              $host;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400;   # required for SignalR long-polling fallback
    }
}
```

```sh
rc-update add nginx default
rc-service nginx start
```

---

## Upgrading

```sh
rc-service sagrafacile stop

VERSION="$(curl -sSL https://api.github.com/repos/PrinceEntree8/SagraFacile/releases/latest \
    | grep '"tag_name"' | head -1 | cut -d'"' -f4)"

curl -sSL \
    "https://github.com/PrinceEntree8/SagraFacile/releases/download/${VERSION}/sagrafacile-${VERSION}-linux-x64.tar.gz" \
    -o /tmp/sagrafacile.tar.gz

tar -xzf /tmp/sagrafacile.tar.gz -C /opt/sagrafacile
chmod +x /opt/sagrafacile/SagraFacile.Web
rm /tmp/sagrafacile.tar.gz

rc-service sagrafacile start
```

Database migrations are applied automatically on startup.

---

## Uninstall

```sh
rc-service sagrafacile stop
rc-update del sagrafacile default
rm -f /etc/init.d/sagrafacile
rm -rf /opt/sagrafacile /etc/sagrafacile /var/log/sagrafacile

# Optional: drop the database
su -s /bin/sh postgres -c "psql -c 'DROP DATABASE sagrafacile;'"
su -s /bin/sh postgres -c "psql -c 'DROP USER sagrafacile;'"
```

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `pg_isready` fails | PostgreSQL not running | `rc-service postgresql start` |
| App exits immediately | Bad connection string | Check `/var/log/sagrafacile/app.log` |
| Sessions reset on restart | `Jwt__Key` not set | Set a fixed key in `sagrafacile.env` |
| 502 from nginx | App not listening | `rc-service sagrafacile status` |
| Port 8080 already in use | Conflict | Change `ASPNETCORE_URLS` in `sagrafacile.env` |
