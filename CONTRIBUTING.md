# Contributing

Bug reports and focused pull requests are welcome.

## Reporting Bugs

Search existing issues first. Include your Jellyfin version, server platform,
installation method, affected media type, source image orientation, relevant
logs, and steps to reproduce the problem.

## Development

The plugin requires the .NET 9 SDK.

```bash
dotnet restore Jellyfin.Plugin.CoverArt.slnx
dotnet test Jellyfin.Plugin.CoverArt.slnx -c Release
```

Keep changes focused, add or update tests for changed behavior, and do not
commit generated `bin`, `obj`, or `artifacts` directories.

By participating, you agree to keep project discussions respectful and
constructive.