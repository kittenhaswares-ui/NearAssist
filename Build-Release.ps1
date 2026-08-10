$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot "NearAssist\NearAssist.csproj"
$dllPath = Join-Path $repoRoot "NearAssist\bin\x64\Release\NearAssist.dll"
$packagePath = Join-Path $repoRoot "NearAssist\bin\x64\Release\NearAssist\latest.zip"

& dotnet restore $projectPath --locked-mode -p:Platform=x64
if ($LASTEXITCODE -ne 0) { throw "dotnet restore fehlgeschlagen." }

& dotnet build $projectPath --configuration Release --no-restore -p:Platform=x64
if ($LASTEXITCODE -ne 0) { throw "dotnet build fehlgeschlagen." }

if (-not (Test-Path $dllPath)) { throw "Plugin-DLL fehlt: $dllPath" }

Write-Host "Near Assist erfolgreich gebaut:" -ForegroundColor Green
Write-Host $dllPath -ForegroundColor Yellow
if (Test-Path $packagePath) { Write-Host $packagePath -ForegroundColor Yellow }
