$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Set-Location $repoRoot

Get-Process ClipboardSync.Windows -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600

Write-Host "[1/4] Build Windows release..."
dotnet build apps/windows/ClipboardSync.Windows.csproj -c Release
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed."
}

Write-Host "[2/4] Publish Windows artifact..."
dotnet publish apps/windows/ClipboardSync.Windows.csproj -c Release -o artifacts/windows
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

$exePath = Join-Path $repoRoot "artifacts/windows/ClipboardSync.Windows.exe"
if (-not (Test-Path $exePath)) {
    throw "Published executable not found: $exePath"
}

Write-Host "[3/4] Startup smoke test for published exe..."
$p = Start-Process -FilePath $exePath -PassThru
Start-Sleep -Seconds 4
$alive = -not $p.HasExited
if ($alive) {
    Stop-Process -Id $p.Id -Force
}
if (-not $alive) {
    throw "Published exe exited unexpectedly during startup smoke test."
}
Write-Host "Startup smoke test passed."

Write-Host "[4/4] Run scenario runner (configuration/pairing persistence/sync/restart/exception)..."
dotnet run --project scripts/validation/windows-scenario-runner/windows-scenario-runner.csproj -- $repoRoot
if ($LASTEXITCODE -ne 0) {
    throw "Scenario runner reported failures."
}

Write-Host "Windows user-scenario validation completed successfully."
