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

# Universal: catch whitespace errors in the diff
git diff --check
if ($LASTEXITCODE -ne 0) { $failures += 'git diff --check' }

# Full Release build. Note: root Directory.Build.props sets a singular
# <TargetFramework>net10.0</TargetFramework>, which leaks into the multi-targeted
# Tests.csproj and suppresses its net10.0-windows leg on a normal solution build
# (MSBuild cross-targeting only triggers when TargetFramework is empty). So the
# solution build below produces only the net10.0 Tests leg; the net10.0-windows
# Tests leg must be built explicitly with --framework (next step).
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build (solution)' }

# Build the net10.0-windows Tests leg explicitly (incl. the WPF GUI tests).
# This is also where removing the IDE0008/IDE0011 .editorconfig relaxations
# (item 3) turns any residual var/brace violations in the GUI test files into
# build errors.
dotnet build tests/Tests/Tests.csproj --framework net10.0-windows -c Release --no-restore
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build (net10.0-windows leg)' }

# Cross-platform leg (mirrors the Linux CI leg).
dotnet test tests/Tests/Tests.csproj --framework net10.0 -c Release --no-build
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test net10.0' }

# Windows GUI leg -- the proof for this ticket. The net10.0 leg above does NOT
# run the #if WINDOWS GUI tests; this leg exercises the full suite (all
# cross-platform tests plus the GUI tests) for net10.0-windows.
dotnet test tests/Tests/Tests.csproj --framework net10.0-windows -c Release --no-build
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test net10.0-windows' }

# Whitespace-only format check (mirrors CI; full dotnet format breaks on the
# C# -> F# cross-language refs on hosted runners).
dotnet format whitespace --verify-no-changes --no-restore
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet format whitespace' }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "gate.ps1: $($failures.Count) check(s) failed"
    exit 1
}
Write-Host 'gate.ps1: all checks green'
exit 0
