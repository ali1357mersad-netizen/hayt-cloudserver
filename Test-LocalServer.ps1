[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5088"
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param(
        [string]$Text
    )
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host $Text -ForegroundColor Yellow
    Write-Host "==================================================" -ForegroundColor Cyan
}

function Invoke-JsonGet {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url
    )

    Invoke-RestMethod -Uri $Url -Method Get
}

function Invoke-JsonPost {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url,

        [Parameter(Mandatory = $true)]
        [object]$Body
    )

    $json = $Body | ConvertTo-Json -Depth 30

    Invoke-RestMethod `
        -Uri $Url `
        -Method Post `
        -ContentType "application/json; charset=utf-8" `
        -Body $json
}

try {
    Write-Host "Testing Hayt Cloud Server..." -ForegroundColor Green
    Write-Host "Base URL: $BaseUrl" -ForegroundColor Green

    # --------------------------------------------------
    # 1) Root
    # --------------------------------------------------
    Write-Step "[1] Root endpoint"

    $root = Invoke-JsonGet -Url "$BaseUrl/"
    $root | ConvertTo-Json -Depth 20

    if (-not $root.success) {
        throw "Root endpoint returned success = false"
    }

    # --------------------------------------------------
    # 2) Health
    # --------------------------------------------------
    Write-Step "[2] Health endpoint"

    $health = Invoke-JsonGet -Url "$BaseUrl/api/health"
    $health | ConvertTo-Json -Depth 20

    if ($health.status -ne "Healthy") {
        throw "Health endpoint is not healthy."
    }

    # --------------------------------------------------
    # 3) Online users
    # --------------------------------------------------
    Write-Step "[3] Online users endpoint"

    $onlineUsers = Invoke-JsonGet -Url "$BaseUrl/api/online/users"
    $onlineUsers | ConvertTo-Json -Depth 20

    # --------------------------------------------------
    # 4) Login
    # --------------------------------------------------
    Write-Step "[4] Development login"

    $loginBody = @{
        userId   = "local-admin"
        deviceId = "powershell-test"
        loginKey = "local-test-key"
    }

    $login = Invoke-JsonPost -Url "$BaseUrl/api/auth/login" -Body $loginBody
    $login | ConvertTo-Json -Depth 20

    if (-not $login.success) {
        throw "Login returned success = false"
    }

    if ([string]::IsNullOrWhiteSpace($login.accessToken)) {
        throw "Login did not return accessToken."
    }

    # --------------------------------------------------
    # 5) CloudSync Push
    # --------------------------------------------------
    Write-Step "[5] CloudSync push"

    $pushBody = @{
        userId   = "local-admin"
        deviceId = "powershell-test"
        items    = @(
            @{
                id            = [guid]::NewGuid().ToString()
                entityType    = "lesson-progress"
                entityId      = "lesson-001"
                operationType = "upsert"
                payloadJson   = (@{
                    progress  = 75
                    completed = $false
                    updatedBy = "powershell"
                } | ConvertTo-Json -Compress)
                createdAtUtc  = [DateTimeOffset]::UtcNow.ToString("o")
            }
        )
    }

    $push = Invoke-JsonPost -Url "$BaseUrl/api/cloudsync/push" -Body $pushBody
    $push | ConvertTo-Json -Depth 20

    if (-not $push.success) {
        throw "CloudSync push returned success = false"
    }

    # --------------------------------------------------
    # 6) CloudSync Pull
    # --------------------------------------------------
    Write-Step "[6] CloudSync pull"

    $pullBody = @{
        userId   = "local-admin"
        deviceId = "powershell-test"
        sinceUtc = ([DateTimeOffset]::UtcNow.AddDays(-1).ToString("o"))
    }

    $pull = Invoke-JsonPost -Url "$BaseUrl/api/cloudsync/pull" -Body $pullBody
    $pull | ConvertTo-Json -Depth 20

    if (-not $pull.success) {
        throw "CloudSync pull returned success = false"
    }

    # --------------------------------------------------
    # 7) Broadcast message
    # --------------------------------------------------
    Write-Step "[7] Broadcast message"

    $broadcastBody = @{
        userId  = "local-admin"
        message = "Hello from PowerShell test"
        type    = "test"
        payload = @{
            source = "Test-LocalServer.ps1"
            sentAt = [DateTimeOffset]::UtcNow.ToString("o")
        }
    }

    $broadcast = Invoke-JsonPost -Url "$BaseUrl/api/messages/broadcast" -Body $broadcastBody
    $broadcast | ConvertTo-Json -Depth 20

    if (-not $broadcast.success) {
        throw "Broadcast endpoint returned success = false"
    }

    # --------------------------------------------------
    # 8) Final summary
    # --------------------------------------------------
    Write-Step "[8] Final summary"

    $summary = [PSCustomObject]@{
        RootOk            = $root.success
        HealthStatus      = $health.status
        OnlineUsersCount  = if ($onlineUsers.data) { $onlineUsers.data.count } else { $null }
        LoginOk           = $login.success
        HasAccessToken    = -not [string]::IsNullOrWhiteSpace($login.accessToken)
        PushOk            = $push.success
        PullOk            = $pull.success
        BroadcastOk       = $broadcast.success
        ServerTimeUtc     = [DateTimeOffset]::UtcNow.ToString("o")
    }

    $summary | Format-List

    Write-Host ""
    Write-Host "All local server tests passed successfully." -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Local server test failed." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    throw
}
