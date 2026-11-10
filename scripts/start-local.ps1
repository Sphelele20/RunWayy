$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
 dotnet build RunWayy.sln
 if($LASTEXITCODE -ne 0){throw 'Build failed.'}
 $env:ASPNETCORE_ENVIRONMENT='Development'
 $apiProcess=Start-Process dotnet -ArgumentList @('run','--project','RunWay.Api','--no-build','--no-launch-profile','--urls','http://localhost:5001') -WindowStyle Hidden -PassThru
 try {
  Write-Host 'RaceDay: http://localhost:5000 ; API: http://localhost:5001/swagger'
  dotnet run --project RunWay.Web --no-build --no-launch-profile --urls http://localhost:5000
 } finally {if(-not $apiProcess.HasExited){Stop-Process -Id $apiProcess.Id}}
}finally{Pop-Location}
