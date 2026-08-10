$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot "NearAssist\NearAssist.csproj"
$manifestPath = Join-Path $repoRoot "NearAssist\NearAssist.json"
$packagePath = Join-Path $repoRoot "NearAssist\bin\x64\Release\NearAssist\latest.zip"
$distPackagePath = Join-Path $repoRoot "dist\latest.zip"
$repositoryPath = Join-Path $repoRoot "repo.json"

& (Join-Path $repoRoot "Build-Release.ps1")
if (-not (Test-Path $packagePath)) { throw "Dalamud-Paket fehlt: $packagePath" }

[xml]$project = Get-Content -Raw $projectPath
$version = [string]$project.Project.PropertyGroup.Version
$manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json
$repository = Get-Content -Raw $repositoryPath | ConvertFrom-Json
$entry = @($repository)[0]

$entry.Author = $manifest.Author
$entry.Name = $manifest.Name
$entry.Punchline = $manifest.Punchline
$entry.Description = $manifest.Description
$entry.RepoUrl = $manifest.RepoUrl
$entry.ApplicableVersion = $manifest.ApplicableVersion
$entry.Tags = $manifest.Tags
$entry.AssemblyVersion = $version
$entry.TestingAssemblyVersion = $version
$entry.LastUpdate = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds().ToString()

Copy-Item -Force $packagePath $distPackagePath
$json = ConvertTo-Json -InputObject @($entry) -Depth 10
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText($repositoryPath, $json + [Environment]::NewLine, $utf8WithoutBom)

Write-Host "Custom Repository aktualisiert:" -ForegroundColor Green
Write-Host "https://raw.githubusercontent.com/kittenhaswares-ui/NearAssist/main/repo.json" -ForegroundColor Cyan
