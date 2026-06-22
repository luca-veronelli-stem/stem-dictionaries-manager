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

# Release build (warnings-as-errors via Directory.Build.props)
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build' }

# Cross-platform test leg (mirrors the CI Linux runner). The Tests project
# multi-targets net10.0;net10.0-windows -- net10.0 is the portable leg.
dotnet test tests/Tests/Tests.csproj --framework net10.0 -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test (net10.0)' }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "gate.ps1: $($failures.Count) check(s) failed"
    exit 1
}
Write-Host 'gate.ps1: all checks green'
exit 0
