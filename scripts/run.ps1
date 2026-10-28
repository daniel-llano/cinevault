# API (http://localhost:5268/docs) and web app (http://localhost:5173).
Set-Location (Join-Path $PSScriptRoot "..")
docker compose -f infra/docker-compose.yml up --build @args
