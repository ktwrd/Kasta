#!/bin/bash
source .env
if [[ -z "${DATABASE_CONNECTION_STRING}" ]]; then
    echo "Missing required environment variable DATABASE_CONNECTION_STRING"
    exit 1
fi
if [[ -z "${1}" ]]; then
    echo "Missing migration name"
    echo "Usage: ./apply-migration.sh [name]"
    exit 1
fi
if [[ "${DEBUG}" -eq 1 ]]; then
    echo "Connection String:   $DATABASE_CONNECTION_STRING"
    echo "   Migration Name:   $1"
fi
dotnet ef database update $1 --project "Kasta.Data" --context "Kasta.Data.ApplicationDbContext" --connection "$DATABASE_CONNECTION_STRING"