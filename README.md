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

### Conventions

Every `.cs` file contains exactly one top-level type. Nested types are used only as a deliberate
encapsulation aid.

## Installation

### For developers with the .NET SDK

Install Kiln Studio as a global tool:

```bash
dotnet tool install --global PepperPix.Kiln.Studio
kiln-studio
```

Update:

```bash
dotnet tool update --global PepperPix.Kiln.Studio
```

> Note: `dotnet tool` does not provide OS integration (Dock, Start menu, `.app` bundle).
> For end-user installation, use Homebrew Cask or winget.

## Building

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (10.0.301 or later)
- [Node.js](https://nodejs.org/) (for commit tooling)

### Building

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
