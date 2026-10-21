## Why
The .NET tool distribution does not create a native macOS application bundle identity, so macOS may show "Avalonia Application" instead of "Kiln Studio". A local packaging script creates a launchable `.app` with a suitable `Info.plist` without introducing release automation that has not been planned.

## What Changes
- Add a local script that creates `dist/Kiln Studio.app` for `osx-arm64` and `osx-x64`.
- Create an `Info.plist` with the application name, identity, executable, and version metadata.
- Create an `.icns` from the existing PNG files when possible; missing macOS tools do not prevent bundle creation.
- Document local unsigned launch and the expected Gatekeeper notice.

## Capabilities
### New Capabilities
- `kiln-studio/macos-app-bundle`

### Modified Capabilities
None.

## Impact
- Target repository: `PepperPix/Kiln-Studio`, on a feature branch from `beta`.
- Affected areas: `scripts/package-macos-app.sh`, `scripts/Info.plist.template`, the local `dist/` artifact, and `README.md`.
- Out of scope: CI or release workflows, DMG creation, code signing, notarization, Windows packaging, and release asset uploads.
