# ---------------------------------------------------------------------------
# HotelOS launcher for Windows (PowerShell). Starts the broker, all four
# microservices and the dashboard, each in its own window.
# Run from the repository root:  ./run.ps1
# ---------------------------------------------------------------------------
$root = $PSScriptRoot

function Start-Svc($name, $path) {
    Write-Host "Starting $name..."
    Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$root\$path`"" -WindowStyle Normal
}

Start-Svc "broker"       "src\HotelOS.Broker"
Start-Sleep -Seconds 2
Start-Svc "reception"    "src\HotelOS.Reception"
Start-Svc "housekeeping" "src\HotelOS.Housekeeping"
Start-Svc "roomservice"  "src\HotelOS.RoomService"
Start-Svc "maintenance"  "src\HotelOS.Maintenance"
Start-Svc "dashboard"    "src\HotelOS.Dashboard"

Write-Host ""
Write-Host "HotelOS is starting up."
Write-Host "  Dashboard:  http://localhost:5005   (token: grandstay2026)"
Write-Host "  Close the spawned windows to stop the services."
