#Requires -Version 7
$ErrorActionPreference = 'Stop'

# Exit-code discipline: EAP 'Stop' governs PowerShell errors only -- native
# commands (git, dotnet) never trigger it, and $LASTEXITCODE is
# last-command-wins. Capture the exit code after every native check, report
# failures via Write-Host (Write-Error under EAP Stop throws and aborts the
# caller's compound statement), and end with an explicit exit so callers --
# including GitHub Actions' `shell: pwsh`, which appends `exit $LASTEXITCODE`
# to every run: step -- see the gate's verdict, not the last command's.

$failures = @()

# Universal: catch whitespace errors in the diff.
git diff --check
if ($LASTEXITCODE -ne 0) { $failures += 'git diff --check' }

# Build the whole solution in Release (mirrors CI).
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet build' }

# Cross-platform test leg. The single Tests project multi-targets
# net10.0;net10.0-windows; --framework net10.0 selects the cross-platform leg
# the Linux CI runner gates on. The standards auto-discovery loop keys off a
# *.Tests.csproj name, but this repo's project is Tests.csproj (no match), so
# target it explicitly to avoid a silent zero-test green.
dotnet test tests/Tests/Tests.csproj --framework net10.0 -c Release
if ($LASTEXITCODE -ne 0) { $failures += 'dotnet test' }

# Commit-message gate: every commit this branch adds on top of github/main
# must be a Conventional Commit with a non-empty body and no WIP/draft/fixup
# subject. This is the lightweight resolve-ticket flow (no specs/tasks.md), so
# the durable commit->issue link is the body's `Closes #N`, not a Tasks:
# trailer; the gate therefore does not require a Tasks: trailer.
$convCommitRe = '^(feat|fix|docs|test|refactor|perf|build|ci|chore|style|revert)(\([^)]+\))?!?: .+'
foreach ($sha in (git rev-list github/main..HEAD)) {
    $subject = git show -s --format=%s $sha
    $body = (git show -s --format=%b $sha) -split "`n" | Where-Object { $_.Trim() } | Out-String
    $short = $sha.Substring(0, 7)
    if ($subject -match '^(WIP|wip|draft|Draft|tmp|Tmp|temp|Temp|fixup!|squash!)') {
        $failures += "commit $short subject is WIP/draft/fixup: $subject"
    }
    elseif ($subject -notmatch $convCommitRe) {
        $failures += "commit $short subject is not a Conventional Commit: $subject"
    }
    elseif (-not $body) {
        $failures += "commit $short has an empty body"
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "gate.ps1: $($failures.Count) check(s) failed"
    exit 1
}
Write-Host 'gate.ps1: all checks green'
exit 0
