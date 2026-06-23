#Requires -Version 7
$ErrorActionPreference = 'Stop'

# Exit-code discipline: EAP 'Stop' governs PowerShell errors only -- native
# commands (git, dotnet, ...) never trigger it, and $LASTEXITCODE is
# last-command-wins. Capture the exit code after EVERY native check, report
# failures via Write-Host (Write-Error under EAP Stop throws and aborts the
# caller's compound statement), and end with an explicit exit so callers --
# including GitHub Actions' `shell: pwsh`, which appends `exit $LASTEXITCODE`
# to every run: step -- see the gate's verdict, not the last command's.

$failures = @()

# Universal: catch whitespace errors in the diff.
git diff --check
if ($LASTEXITCODE -ne 0) { $failures += 'git diff --check' }

# Formatting: whitespace-only, mirroring ci.yml. The analyzer phase of full
# `dotnet format` fails CS0246 on the C#->F# cross-language refs; whitespace
# skips it. Analyzer/style rules are still enforced via TreatWarningsAsErrors.
dotnet format whitespace --verify-no-changes
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet format whitespace' }

# BOTH test legs are MANDATORY for #135 and run explicitly, mirroring the
# repo's inline ci.yml (documented deviation, see CLAUDE.md):
#   net10.0         = 872 tests  (cross-platform; excludes #if WINDOWS GUI tests)
#   net10.0-windows = 1524 tests (adds the WPF GUI.Windows tests, e.g.
#                                 SettingsViewModelTests, that net10.0 cannot
#                                 compile). Any touch under src/GUI.Windows
#                                 (e.g. SettingsViewModel.cs) needs this leg.
dotnet test tests/Tests/Tests.csproj --framework net10.0 -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test net10.0' }

dotnet test tests/Tests/Tests.csproj --framework net10.0-windows -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test net10.0-windows' }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "gate.ps1: $($failures.Count) check(s) failed"
    exit 1
}
Write-Host 'gate.ps1: all checks green'
exit 0
