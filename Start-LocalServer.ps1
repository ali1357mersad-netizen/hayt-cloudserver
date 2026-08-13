$ErrorActionPreference = "Stop"

$ProjectPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ProjectPath

Write-Host "Starting Hayt Cloud Server..." -ForegroundColor Cyan
Write-Host "Health: http://localhost:5088/api/health" -ForegroundColor Yellow
Write-Host "SignalR: http://localhost:5088/hubs/online" -ForegroundColor Yellow

dotnet run --launch-profile "Hayt.CloudServer.HTTP"
