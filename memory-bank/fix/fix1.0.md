# Fix 1.0 - Security Advisory Remediation

## Scope
- Address NU1904 for `System.Drawing.Common` 4.7.0.
- Address NU1903 for `System.IO.Packaging` 8.0.0.

## Changes
- Centralized transitive package pinning via `Directory.Packages.props`:
  - `System.Drawing.Common` -> 8.0.1
  - `System.IO.Packaging` -> 8.0.1
- Enabled Central Package Management and moved all package versions out of `.csproj` files.

## Rationale
- Central package management with transitive pinning forces NuGet to resolve the patched versions.
- Centralized pinning applies consistently to all projects.

## Verification
- Pending. Suggested command:
  - `dotnet test src/AI.DiffAssistant.Tests`
