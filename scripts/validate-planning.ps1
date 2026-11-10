$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
foreach($name in @('RaceDay_ERD.png','RaceDay_ERD.svg','RaceDay_Endpoint_Plan.md','RaceDay_Schema.sql','schema.json','REQUIREMENTS-AUDIT.md')){
 $path=Join-Path $repoRoot ('docs/'+$name)
 if(!(Test-Path -LiteralPath $path) -or (Get-Item -LiteralPath $path).Length -eq 0){throw "Missing or empty planning file: $name"}
}
$entities=Get-Content (Join-Path $repoRoot 'docs/schema.json') -Raw|ConvertFrom-Json
if($entities.Count -lt 6){throw 'ERD metadata must include at least six entities.'}
$plan=Get-Content (Join-Path $repoRoot 'docs/RaceDay_Endpoint_Plan.md') -Raw
foreach($term in @('register','login','profile','events','categories','enrolments','results')){if($plan -notmatch $term){throw "Endpoint plan missing: $term"}}
Write-Host "Planning package valid: $($entities.Count) entities and required files present."
