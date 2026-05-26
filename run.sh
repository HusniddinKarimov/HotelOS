#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# HotelOS launcher — starts the broker, all four microservices and the
# dashboard in one command. Ctrl+C stops everything cleanly.
# ---------------------------------------------------------------------------
set -euo pipefail
cd "$(dirname "$0")"

PIDS=()
cleanup() {
  echo ""
  echo "Stopping HotelOS..."
  for pid in "${PIDS[@]}"; do kill "$pid" 2>/dev/null || true; done
  wait 2>/dev/null || true
  exit 0
}
trap cleanup INT TERM

start() {
  local name=$1 path=$2
  echo "Starting $name..."
  # Process substitution keeps $! pointing at dotnet (not the log filter),
  # so cleanup can stop each service reliably.
  dotnet run --project "$path" > >(sed "s/^/[$name] /") 2>&1 &
  PIDS+=($!)
}

# Broker first so services can connect; the clients auto-retry anyway.
start broker       src/HotelOS.Broker
sleep 2
start reception    src/HotelOS.Reception
start housekeeping src/HotelOS.Housekeeping
start roomservice  src/HotelOS.RoomService
start maintenance  src/HotelOS.Maintenance
start dashboard    src/HotelOS.Dashboard

echo ""
echo "HotelOS is starting up."
echo "  Dashboard:  http://localhost:5005   (token: grandstay2026)"
echo "  Press Ctrl+C to stop all services."
echo ""
wait
