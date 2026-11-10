$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
 dotnet build RunWayy.sln
 if($LASTEXITCODE -ne 0){throw 'Build failed.'}
 dotnet run --project tests/RunWay.Smoke --no-build -- $repoRoot
 if($LASTEXITCODE -ne 0){throw 'Integration checks failed.'}
} finally {Pop-Location}
