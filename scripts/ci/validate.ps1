Push-Location "$PSScriptRoot\..\.."
npm run build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

npm run validate:protocol
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

npm run sim:test
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "CI validation passed"
Pop-Location
