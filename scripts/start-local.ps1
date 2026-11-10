$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
 dotnet build RunWayy.sln
 if($LASTEXITCODE -ne 0){throw 'Build failed.'}
 $env:ASPNETCORE_ENVIRONMENT='Development'
 $apiProcess=Start-Process dotnet -ArgumentList @('run','--project','src/RunWay.Api','--no-build','--no-launch-profile','--urls','http://localhost:5100') -PassThru -WindowStyle Hidden
 try {
  Write-Host 'RaceDay web: http://localhost:5200 ; API docs: http://localhost:5100/swagger'
  dotnet run --project src/RunWay.Web --no-build --no-launch-profile --urls http://localhost:5200
 } finally {if(-not $apiProcess.HasExited){Stop-Process -Id $apiProcess.Id}}
} finally {Pop-Location}
