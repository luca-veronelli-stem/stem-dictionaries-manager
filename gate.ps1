#Requires -Version 7
$ErrorActionPreference = 'Stop'

# Exit-code discipline: EAP 'Stop' governs PowerShell errors only -- native
# commands (git, dotnet, ...) never trigger it, and $LASTEXITCODE is
# last-command-wins. Capture the exit code after EVERY native check, report
# failures via Write-Host, and end with an explicit exit so callers see the
# gate's verdict, not the last command's.

$failures = @()

# Universal: catch whitespace errors in the diff.
git diff --check
if ($LASTEXITCODE -ne 0) { $failures += 'git diff --check' }

# Restore once up front so the build/test legs and the format check can run
# --no-restore / --no-build, matching CI.
dotnet restore
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet restore' }

# Format check is whitespace-only, exactly as CI runs it (the analyzer phase of
# `dotnet format` trips CS0246 on C# -> F# cross-language refs on hosted
# runners). Analyzer/style rules are still enforced via TreatWarningsAsErrors.
dotnet format whitespace --verify-no-changes --no-restore
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet format whitespace' }

# Cross-platform leg (mirrors the CI Linux runner): a plain solution build picks
# up the root Directory.Build.props singular <TargetFramework>net10.0, so the
# whole solution -- including GUI.Windows with its #if WINDOWS WPF code excluded
# -- builds as net10.0. The net10.0-windows leg is built explicitly below.
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build (net10.0 leg)' }

# net10.0 test leg: the cross-platform contract (expects 872 tests).
dotnet test --framework net10.0 --configuration Release --no-build --logger 'trx;LogFileName=test-results-net10.0.trx'
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test (net10.0 leg)' }

# Windows leg (mirrors the CI Windows runner): the root's singular
# <TargetFramework> otherwise suppresses the multi-targeted Tests.csproj
# net10.0-windows leg, so build it explicitly with --framework. It pulls in
# GUI.Windows plus the #if WINDOWS WPF GUI tests (expects 1524 tests).
dotnet build tests/Tests/Tests.csproj --framework net10.0-windows --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build (net10.0-windows leg)' }

dotnet test tests/Tests/Tests.csproj --framework net10.0-windows --configuration Release --no-build --logger 'trx;LogFileName=test-results-net10.0-windows.trx'
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test (net10.0-windows leg)' }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "gate.ps1: $($failures.Count) check(s) failed"
    exit 1
}
Write-Host 'gate.ps1: all checks green'
exit 0
