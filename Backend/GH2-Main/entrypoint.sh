#!/bin/bash
set -e

echo "=== Running DB Migrations ==="
./efbundle --connection "$AUTH_CONN_STR"
echo "=== Migrations Done ==="

echo "=== Starting Backend ==="
exec dotnet GH2-Main.dll