# Hayt.CloudServer

Local ASP.NET Core 8 server for the Hayt WPF application.

## URLs

- HTTP: http://localhost:5088
- HTTPS: https://localhost:7088
- Health: http://localhost:5088/api/health
- Online users: http://localhost:5088/api/online/users
- SignalR: http://localhost:5088/hubs/online

## Run

.\Start-LocalServer.ps1

## Test

Run the server first, then in a second PowerShell window:

.\Test-LocalServer.ps1

## Important

Authentication tokens in this project are development-only tokens.
They must be replaced by real JWT authentication before production deployment.
