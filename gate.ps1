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

# Full Release build (Windows host -> includes the net10.0-windows GUI test
# leg). This is the collision safety valve: a ctor change that breaks GUI
# test COMPILATION fails here even though those tests are owned by #119.
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build -c Release' }

# Cross-platform test leg only (net10.0). The net10.0-windows GUI assertion
# leg is sibling #119's to green; this gate gates the cross-platform contract.
dotnet test tests/Tests/Tests.csproj --framework net10.0 -c Release --no-build
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test --framework net10.0' }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "gate.ps1: $($failures.Count) check(s) failed"
    exit 1
}
Write-Host 'gate.ps1: all checks green'
exit 0
