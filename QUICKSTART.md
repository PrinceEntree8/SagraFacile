# Quick Start Guide

## Prerequisites

- [Docker](https://www.docker.com/get-started) and [Docker Compose](https://docs.docker.com/compose/) (v2.x or later)
- Git

---

## Deploying with Docker Compose

SagraFacile ships with three Docker Compose files to cover the most common deployment scenarios:

| File | Purpose |
|---|---|
| `docker-compose.yml` | Local development (builds image from source) |
| `docker-compose.release.yml` | Production / internet-facing |
| `docker-compose.lan.yml` | LAN / offline events (HTTP only, no HTTPS redirect) |

### 1. Clone the repository

```bash
git clone https://github.com/PrinceEntree8/SagraFacile.git
cd SagraFacile
```

### 2. Create your environment file

Copy the example and edit the values before the first run:

```bash
cp .env.example .env
```

Open `.env` and set at least:

```env
POSTGRES_PASSWORD=<strong-db-password>
Jwt__Key=<long-random-string-at-least-32-characters>
Seed__AdminPassword=<initial-admin-password>
```

> **Security note**: Never leave the default placeholder values in a production or LAN deployment.

---

## Linux deployment

### Development (build from source)

```bash
docker compose up -d
```

The app is available at **http://localhost:5000**.

To stop:

```bash
docker compose down
```

### Production (internet-facing, from pre-built image)

```bash
docker compose -f docker-compose.release.yml up -d
```

The app listens on `WEB_PORT` (default **80**). Place a reverse proxy (nginx, Caddy, Traefik) in front for TLS termination.

To stop:

```bash
docker compose -f docker-compose.release.yml down
```

### LAN / offline events (HTTP only)

Use this when the server and all tablets/phones are on the same local network with no internet access:

```bash
docker compose -f docker-compose.lan.yml up -d
```

All devices on the network can reach the app at `http://<server-ip>:<WEB_PORT>` (default port **80**).

---

## Windows deployment

The commands are identical to Linux. Run them in **PowerShell** or **Command Prompt** with [Docker Desktop](https://docs.docker.com/desktop/install/windows-install/) installed.

### Development (build from source)

```powershell
docker compose up -d
```

The app is available at **http://localhost:5000**.

To stop:

```powershell
docker compose down
```

### Production

```powershell
docker compose -f docker-compose.release.yml up -d
```

### LAN / offline events

```powershell
docker compose -f docker-compose.lan.yml up -d
```

> **Windows firewall**: If other devices on your network cannot reach the server, add an inbound firewall rule to allow TCP traffic on `WEB_PORT` (default 80).

---

## First-run setup

1. Wait for the health-check to pass (usually 10–20 seconds).
2. Open a browser and navigate to the app URL.
3. Log in with the admin credentials you set in `.env` (`Seed__AdminUsername` / `Seed__AdminPassword`).
4. Go to **Events** → create an event and activate it.
5. Go to **Menu Management** → add menu categories and items.
6. The home page (`/`) now shows the active event's menu to visitors.

---

## Viewing logs

```bash
# All services
docker compose logs -f

# Web app only
docker compose logs -f web

# PostgreSQL only
docker compose logs -f postgres
```

---

## Upgrading

```bash
# Pull the latest image (production)
docker compose -f docker-compose.release.yml pull
docker compose -f docker-compose.release.yml up -d
```

Database migrations run automatically on startup.

---

## Getting Help

- **Development guide**: `docs/DEVELOPMENT.md`
- **Issues**: Open an issue on GitHub
- **README**: `README.md`
