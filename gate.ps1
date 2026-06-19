#Requires -Version 7
$ErrorActionPreference = 'Stop'

# Mechanical gate for the refactor/services-validation PR (#9, #10, #11).
# Lightweight resolve-ticket flow (no speckit / tasks.md): the commit-message
# check enforces Conventional Commits + a non-empty body, but NOT a Tasks:
# trailer -- this PR links work via `Closes #N`.
#
# Exit-code discipline: EAP 'Stop' governs PowerShell errors only -- native
# commands (git, dotnet) never trigger it, and $LASTEXITCODE is
# last-command-wins. Capture the exit code after EVERY native check, report
# failures via Write-Host, and end with an explicit exit.

$failures = @()

# Universal: catch whitespace errors in the diff.
git diff --check
if ($LASTEXITCODE -ne 0) { $failures += 'git diff --check' }

# Build the whole solution in Release (matches CI).
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build' }

# Cross-platform test leg (net10.0). The Tests project multi-targets
# net10.0;net10.0-windows; the net10.0 leg covers Core/Services/Infrastructure/
# API and skips the Windows-only GUI tests, mirroring the Linux CI runner.
dotnet test tests/Tests/Tests.csproj --framework net10.0 -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test (net10.0)' }

# Commit-message gate: Conventional Commits subject + non-empty body on HEAD.
$subject = git show -s --format=%s HEAD
$body = (git show -s --format=%b HEAD) -split "`n" | Where-Object { $_.Trim() } | Out-String
$convCommitRe = '^(feat|fix|docs|test|refactor|perf|build|ci|chore|style|revert)(\([^)]+\))?!?: .+'
if ($subject -notmatch $convCommitRe) { $failures += "commit subject not Conventional Commits: $subject" }
if (-not $body.Trim()) { $failures += 'commit body is empty' }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "gate.ps1: $($failures.Count) check(s) failed"
    exit 1
}
Write-Host 'gate.ps1: all checks green'
exit 0
