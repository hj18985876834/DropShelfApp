param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$PublishDir = "artifacts\publish\win-x64",
    [string]$InnoSetupCompiler = "D:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$certPath = $env:EDGE_TUCK_SIGN_CERT_PATH
$certPassword = $env:EDGE_TUCK_SIGN_CERT_PASSWORD
$timestampUrl = $env:EDGE_TUCK_TIMESTAMP_URL
$signToolPath = $env:EDGE_TUCK_SIGNTOOL_PATH

if ([string]::IsNullOrWhiteSpace($timestampUrl)) {
    $timestampUrl = "http://timestamp.digicert.com"
}

if ([string]::IsNullOrWhiteSpace($certPath)) {
    throw "EDGE_TUCK_SIGN_CERT_PATH is required."
}

if (-not (Test-Path $certPath)) {
    throw "Signing certificate was not found: $certPath"
}

if ([string]::IsNullOrWhiteSpace($signToolPath)) {
    $command = Get-Command "signtool.exe" -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        throw "signtool.exe was not found. Set EDGE_TUCK_SIGNTOOL_PATH or add signtool.exe to PATH."
    }

    $signToolPath = $command.Source
}

function Invoke-CodeSign {
    param([Parameter(Mandatory = $true)][string]$Path)

    $arguments = @(
        "sign",
        "/fd", "SHA256",
        "/tr", $timestampUrl,
        "/td", "SHA256",
        "/f", $certPath
    )

    if (-not [string]::IsNullOrWhiteSpace($certPassword)) {
        $arguments += @("/p", $certPassword)
    }

    $arguments += $Path
    & $signToolPath @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Signing failed: $Path"
    }
}

function Invoke-SignatureVerify {
    param([Parameter(Mandatory = $true)][string]$Path)

    & $signToolPath verify /pa /tw /v $Path
    if ($LASTEXITCODE -ne 0) {
        throw "Signature verification failed: $Path"
    }
}

Push-Location $repoRoot
try {
    $publishPath = Join-Path $repoRoot $PublishDir
    $appPath = Join-Path $publishPath "DropShelf.App.exe"
    $installerPath = Join-Path $repoRoot "installer\Output\EdgeTuckSetup.exe"

    dotnet publish ".\src\DropShelf.App\DropShelf.App.csproj" -c $Configuration -r $Runtime --self-contained true -o $publishPath
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed."
    }

    if (-not (Test-Path $appPath)) {
        throw "Published app executable was not found: $appPath"
    }

    Invoke-CodeSign -Path $appPath
    Invoke-SignatureVerify -Path $appPath

    & $InnoSetupCompiler ".\installer\DropShelf.iss"
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup compiler failed."
    }

    if (-not (Test-Path $installerPath)) {
        throw "Installer was not found: $installerPath"
    }

    Invoke-CodeSign -Path $installerPath
    Invoke-SignatureVerify -Path $installerPath

    $hash = Get-FileHash -Algorithm SHA256 $installerPath
    $size = (Get-Item $installerPath).Length
    [PSCustomObject]@{
        Installer = $installerPath
        SizeBytes = $size
        Sha256 = $hash.Hash.ToLowerInvariant()
    } | Format-List
}
finally {
    Pop-Location
}
