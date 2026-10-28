#!/usr/bin/env bash
# API (http://localhost:5268/docs) and web app (http://localhost:5173).
set -euo pipefail
cd "$(dirname "$0")/.."
docker compose -f infra/docker-compose.yml up --build "$@"
