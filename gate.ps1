#Requires -Version 7
$ErrorActionPreference = 'Stop'

# Universal: catch whitespace errors in the diff
git diff --check

# Build the whole solution; analyzers gate via TreatWarningsAsErrors,
# so a BOM/CRLF slip in a regenerated migration fails here.
dotnet build -c Release

# Mirror CI's whitespace-only format gate.
dotnet format whitespace --verify-no-changes --no-restore

# Full test suite is the bisect-safe guarantee. It includes the #86
# model-metadata regression test (AppVersion/ClaimedAppVersion max length
# == 128), which reads the EF model (HasMaxLength), not the DB, so it is
# deterministic and needs no SQL Server.
dotnet test tests/Tests/Tests.csproj -c Release
