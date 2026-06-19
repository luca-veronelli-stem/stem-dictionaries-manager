#Requires -Version 7
$ErrorActionPreference = 'Stop'

# Exit-code discipline: EAP 'Stop' governs PowerShell errors only -- native
# commands (git, dotnet, ...) never trigger it, and $LASTEXITCODE is
# last-command-wins. Capture the exit code after EVERY native check, report
# failures via Write-Host (Write-Error under EAP Stop throws and aborts the
# caller's compound statement), and end with an explicit exit so callers see
# the gate's verdict, not the last command's.

$failures = @()

# Universal: catch whitespace errors in the diff
git diff --check
if ($LASTEXITCODE -ne 0) { $failures += 'git diff --check' }

# Commit-message gate: every commit since github/main must be a Conventional
# Commit with a non-empty body. This lightweight flow (no speckit / tasks.md)
# uses `Closes #N` as the durable issue link, so the `Tasks:` trailer that the
# full resolve-ticket gate enforces is intentionally NOT required here.
$base = git merge-base github/main HEAD
if ($LASTEXITCODE -eq 0 -and $base) {
    $convRe = '^(feat|fix|docs|test|refactor|perf|build|ci|chore|style|revert)(\([^)]+\))?!?: .+'
    foreach ($sha in (git rev-list --reverse "$base..HEAD")) {
        $subject = git show -s --format=%s $sha
        $body = (git show -s --format=%b $sha) -split "`n" | Where-Object { $_.Trim() } | Out-String
        if ($subject -match '^(WIP|wip|draft|Draft|tmp|Tmp|temp|Temp|fixup!|squash!)') {
            $failures += "commit gate (bad subject prefix): $($sha.Substring(0,7)) $subject"
        }
        elseif ($subject -notmatch $convRe) {
            $failures += "commit gate (not a Conventional Commit): $($sha.Substring(0,7)) $subject"
        }
        elseif (-not $body) {
            $failures += "commit gate (empty body): $($sha.Substring(0,7)) $subject"
        }
    }
}

# Build the whole solution. No --framework here: GUI.Windows is net10.0-windows
# only, so a solution-scope --framework net10.0 would NETSDK1005 it. The
# solution build produces the Tests project's net10.0 leg (the portability
# contract leg that CI runs).
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build' }

# CI's hard formatting gate is whitespace-only (cross-language analyzer gap).
dotnet format whitespace --verify-no-changes --no-restore
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet format whitespace' }

# Run the net10.0 (cross-platform) Tests leg -- the portability contract and
# exactly what CI exercises for this project. The Tests project also declares
# net10.0-windows, but the solution build does not produce that leg and it
# carries a pre-existing, unrelated build break (API integration tests), so it
# is intentionally not invoked here.
dotnet test --configuration Release --no-build --framework net10.0
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test (net10.0)' }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "gate.ps1: $($failures.Count) check(s) failed"
    exit 1
}
Write-Host 'gate.ps1: all checks green'
exit 0
