#Requires -Version 7
$ErrorActionPreference = 'Stop'

# Exit-code discipline: EAP 'Stop' governs PowerShell errors only -- native
# commands (git, dotnet, lake, ...) never trigger it, and $LASTEXITCODE is
# last-command-wins. Capture the exit code after EVERY native check, report
# failures via Write-Host (Write-Error under EAP Stop throws and aborts the
# caller's compound statement), and end with an explicit exit so callers --
# including GitHub Actions' `shell: pwsh`, which appends `exit $LASTEXITCODE`
# to every run: step -- see the gate's verdict, not the last command's.

$failures = @()

# Universal: catch whitespace errors in the diff
git diff --check
if ($LASTEXITCODE -ne 0) { $failures += 'git diff --check' }

# Full solution build (Release). The implicit restore here runs NuGetAudit;
# with TreatWarningsAsErrors a live NU1903 fails the build, so a green build
# is part of the proof the SQLitePCLRaw advisory (#107) is cleared. On this
# Windows box the build also covers the net10.0-windows leg (GUI.Windows).
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build' }

# Test the suite. The single Tests project multi-targets net10.0 +
# net10.0-windows; running without --framework exercises both TFMs (the
# Windows CI full leg), including every SQLite-backed Infrastructure /
# Integration test that proves the 2.x -> 3.x SQLitePCLRaw bump is safe.
dotnet test tests/Tests/Tests.csproj -c Release --no-build
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test' }

# Formatting gate mirrors CI (whitespace-only; analyzer phase fails on
# cross-language refs on hosted runners -- see docs/Standards/CI.md).
dotnet format whitespace --verify-no-changes --no-restore
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet format whitespace' }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "gate.ps1: $($failures.Count) check(s) failed"
    exit 1
}
Write-Host 'gate.ps1: all checks green'
exit 0
