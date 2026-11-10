$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $Root "UniLauncher\UniLauncher.csproj"
$Output = Join-Path $Root "dist\UniLauncher"
$ZipPath = Join-Path $Root "dist\UniLauncher_v1.0_RC_win-x64.zip"

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "          UniLauncher Windows Builder         " -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

if ($env:OS -ne "Windows_NT") {
    throw "This build script must be run on Windows."
}
if (-not (Test-Path $Project)) {
    throw "Project file not found: $Project"
}
$Dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $Dotnet) {
    throw ".NET SDK not found. Install .NET 10 SDK, then run BUILD.bat again."
}

$SdkList = & dotnet --list-sdks
if ($LASTEXITCODE -ne 0) {
    throw "Unable to query installed .NET SDKs."
}
$HasNet10 = $false
foreach ($Line in $SdkList) {
    if ($Line -match '^\s*10\.') { $HasNet10 = $true }
}
if (-not $HasNet10) {
    throw ".NET 10 SDK is required. Install it from https://dotnet.microsoft.com/download/dotnet/10.0"
}

Write-Host "[1/4] .NET SDK check passed." -ForegroundColor Green
Write-Host "[2/4] Restoring project..." -ForegroundColor Yellow
& dotnet restore $Project
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE"
}

if (Test-Path $Output) {
    Remove-Item $Output -Recurse -Force
}
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}
New-Item -ItemType Directory -Path $Output -Force | Out-Null

Write-Host "[3/4] Publishing self-contained Windows x64 application..." -ForegroundColor Yellow
& dotnet publish $Project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $Output `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$Exe = Join-Path $Output "UniLauncher.exe"
if (-not (Test-Path $Exe)) {
    throw "Publish completed but UniLauncher.exe was not found in $Output"
}

Copy-Item (Join-Path $Root "docs\USER_GUIDE_RU.md") (Join-Path $Output "ИНСТРУКЦИЯ.md") -Force
Copy-Item (Join-Path $Root "README.md") (Join-Path $Output "О проекте.md") -Force

Write-Host "[4/4] Creating ZIP package..." -ForegroundColor Yellow
Compress-Archive -Path (Join-Path $Output "*") -DestinationPath $ZipPath -Force

$ExeInfo = Get-Item $Exe
$Hash = (Get-FileHash $Exe -Algorithm SHA256).Hash

Write-Host ""
Write-Host "BUILD SUCCESSFUL" -ForegroundColor Green
Write-Host "EXE: $Exe" -ForegroundColor Green
Write-Host ("EXE size: {0:N2} MB" -f ($ExeInfo.Length / 1MB))
Write-Host "ZIP: $ZipPath" -ForegroundColor Green
Write-Host "SHA256: $Hash"
Write-Host ""
Write-Host "Copy the complete 'dist\UniLauncher' folder or its ZIP to the university network drive."
Write-Host "Do not copy only settings.json: the app creates it on first run."
