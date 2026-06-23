# Kiln Studio

A cross-platform desktop CMS for static websites — powered by [Kiln](https://github.com/PepperPix/Kiln).

## Status

**Early development** — Solution skeleton (v0.1.0). No feature logic yet.

## What is Kiln Studio?

Kiln Studio is a desktop application for creating and managing static websites. It provides a visual
interface for content editing, theme configuration, and one-click deployment — powered by the Kiln
static site generator engine.

### Planned Features

- Visual content editor (Markdown with live preview)
- Project and site management
- Theme selection and customization
- Media library
- Build and deployment from the UI
- Cross-platform: macOS and Windows

## Solution Layout

```
src/
  Kiln.Studio/             — Avalonia App (UI, DI host, Views)
  Kiln.Studio.ViewModels/  — ViewModels (UI-free, CommunityToolkit.Mvvm)
  Kiln.Studio.Services/    — Studio services (EngineHost, ProjectSession)
tests/
  Kiln.Studio.Tests/       — TUnit tests
```

## Building

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (10.0.301 or later)
- [Node.js](https://nodejs.org/) (for commit tooling)

### Building before Kiln is on nuget.org

`Kiln.Core` and `Kiln.Abstractions` are not yet published to nuget.org. You need to build them
locally first and provide a local feed:

```bash
# 1. Pack the Kiln engine packages
dotnet pack path/to/Kiln/src/Kiln.Abstractions -c Release -o /tmp/kiln-localfeed
dotnet pack path/to/Kiln/src/Kiln.Core -c Release -o /tmp/kiln-localfeed

# 2. Build and test Studio (nuget.config already points to /tmp/kiln-localfeed)
dotnet build
dotnet test
```

Once `Kiln.Core` and `Kiln.Abstractions` are published to nuget.org, remove the `kiln-local` source
from `nuget.config` — no changes to project files required.

### Regular build

```bash
dotnet build
dotnet test
dotnet run --project src/Kiln.Studio
```

### Commit tooling

```bash
npm install
```

Husky installs a `commit-msg` hook enforcing [Conventional Commits](https://www.conventionalcommits.org/).

## Related Projects

- **[Kiln](https://github.com/PepperPix/Kiln)** — The static site generator engine and CLI tool

## License

[AGPL-3.0](LICENSE)

Copyright 2026 Marcel Kummerow
