#Requires -Version 7
$ErrorActionPreference = 'Stop'

# Universal: catch whitespace errors in the diff
git diff --check

# Build (Release, matches CI line ci.yml:72)
dotnet build --configuration Release

# Formatting (whitespace-only, matches CI line ci.yml:65)
dotnet format whitespace --verify-no-changes --no-restore

# Full cross-platform test leg (matches CI line ci.yml:80)
dotnet test --framework net10.0 --configuration Release --no-build

# Ticket #85 proof: /register status-code mapping for the revoked-Installation
# scenario lives in the integration suite below.
dotnet test --framework net10.0 --configuration Release --no-build --filter "FullyQualifiedName~RegisterEndpointTests"
