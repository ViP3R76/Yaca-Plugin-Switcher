# Contributing

## Development

- Use .NET 10.
- Target Windows x64.
- Keep the application free of third-party NuGet dependencies unless there is a documented technical reason.
- Nullable reference types must remain enabled.
- Warnings are treated as errors.
- Do not load YACA DLLs into the switcher process.
- Keep YACA binaries out of source-control unless redistribution rights are explicitly established.

## Before submitting a pull request

1. Build the solution in `Release | x64`.
2. Run `dotnet build -c Release --no-incremental`.
3. Check that no new compiler warnings are introduced.
4. Update documentation when behavior changes.
5. Do not include personal paths or private logs in commits/issues.

Stable UI changes should preserve the existing behavior. Experimental UI redesign work belongs on a dedicated feature branch.
