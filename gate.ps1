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

# Formatting gate, mirroring CI (whitespace-only: the analyzer phase of
# `dotnet format` fails CS0246 on cross-language C# -> F# refs on hosted
# runners; analyzer/style rules are still enforced via TreatWarningsAsErrors).
dotnet format whitespace --verify-no-changes
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet format whitespace' }

# Full Release build (Windows host -> compiles the net10.0-windows GUI leg too).
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build -c Release' }

# Cross-platform test leg (net10.0): confirms the GUI ctor changes do not
# disturb the cross-platform contract owned by the other slices.
dotnet test tests/Tests/Tests.csproj --framework net10.0 -c Release --no-build
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test --framework net10.0' }

# GUI test leg (net10.0-windows): this PR owns the GUI view-model logger
# injection, so the Windows assertion leg is THIS gate's real proof. CI's
# windows-latest job is the matching gate on the PR.
dotnet test tests/Tests/Tests.csproj --framework net10.0-windows -c Release --no-build
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test --framework net10.0-windows' }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "gate.ps1: $($failures.Count) check(s) failed"
    exit 1
}
Write-Host 'gate.ps1: all checks green'
exit 0
