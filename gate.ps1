#Requires -Version 7
$ErrorActionPreference = 'Stop'

# Mechanical gate for the #17/#18 test-suite reorganization PR.
# Exit-code discipline: EAP 'Stop' governs PowerShell errors only -- native
# commands (git, dotnet) never trigger it, and $LASTEXITCODE is
# last-command-wins. Capture the exit code after EVERY native check, aggregate
# failures, and end with an explicit exit so callers see the gate verdict.

$failures = @()

# Universal: catch whitespace errors in the diff.
git diff --check
if ($LASTEXITCODE -ne 0) { $failures += 'git diff --check' }

# Release build must be clean (0 warnings / 0 errors via TreatWarningsAsErrors).
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build -c Release' }

# Both test legs -- this PR moves files across BOTH target frameworks.
dotnet test tests/Tests/Tests.csproj --framework net10.0 --no-build -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test net10.0' }

dotnet test tests/Tests/Tests.csproj --framework net10.0-windows --no-build -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test net10.0-windows' }

# Commit-message Conventional-Commits gate on HEAD (no Tasks: trailer -- this
# is a lightweight, no-speckit PR with no tasks.md).
$subject = git show -s --format=%s HEAD
$body = (git show -s --format=%b HEAD) -split "`n" | Where-Object { $_.Trim() } | Out-String
$convCommitRe = '^(feat|fix|docs|test|refactor|perf|build|ci|chore|style|revert)(\([^)]+\))?!?: .+'
if ($subject -match '^(WIP|wip|draft|Draft|tmp|Tmp|temp|Temp|fixup!|squash!)') {
    $failures += "HEAD subject is WIP/draft/fixup: $subject"
}
elseif ($subject -notmatch $convCommitRe) {
    $failures += "HEAD subject is not a Conventional Commit: $subject"
}
if (-not $body) { $failures += 'HEAD commit body is empty' }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "gate.ps1: $($failures.Count) check(s) failed"
    exit 1
}
Write-Host 'gate.ps1: all checks green'
exit 0
