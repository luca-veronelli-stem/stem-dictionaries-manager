#Requires -Version 7
$ErrorActionPreference = 'Stop'

# Exit-code discipline: EAP 'Stop' governs PowerShell errors only -- native
# commands (git, dotnet) never trigger it, and $LASTEXITCODE is last-command-wins.
# Capture the exit code after every native check, aggregate into $failures, and
# end with an explicit exit so callers (incl. GitHub Actions' `shell: pwsh`) see
# the gate's verdict, not the last command's.

$failures = @()

# Universal: catch whitespace errors in the diff.
git diff --check
if ($LASTEXITCODE -ne 0) { $failures += 'git diff --check' }

# Build the whole solution; analyzers gate via TreatWarningsAsErrors.
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build' }

# Mirror CI's whitespace-only format gate.
dotnet format whitespace --verify-no-changes --no-restore
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet format whitespace' }

# Cross-platform test leg (net10.0), matching CI's Linux job. The Tests project
# multi-targets net10.0;net10.0-windows; --framework net10.0 runs the portable
# leg and skips the WPF/Windows-only one.
dotnet test tests/Tests/Tests.csproj --framework net10.0 -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test (net10.0)' }

# Commit-message gate on HEAD: Conventional Commits subject + non-empty body.
# This lightweight PR carries no speckit tasks.md, so no Tasks: trailer is required.
$subject = git show -s --format=%s HEAD
$body = (git show -s --format=%b HEAD) -split "`n" | Where-Object { $_.Trim() } | Out-String
$convCommitRe = '^(feat|fix|docs|test|refactor|perf|build|ci|chore|style|revert)(\([^)]+\))?!?: .+'
if ($subject -match '^(WIP|wip|draft|Draft|tmp|Tmp|temp|Temp|fixup!|squash!)') {
    $failures += "commit subject looks like WIP/fixup: $subject"
}
elseif ($subject -notmatch $convCommitRe) {
    $failures += "commit subject is not a Conventional Commit: $subject"
}
if (-not $body) { $failures += 'commit body is empty' }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "gate.ps1: $($failures.Count) check(s) failed"
    exit 1
}
Write-Host 'gate.ps1: all checks green'
exit 0
