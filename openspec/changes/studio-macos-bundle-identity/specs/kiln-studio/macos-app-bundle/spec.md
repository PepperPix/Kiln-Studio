## ADDED Requirements
### Requirement: Local packaging creates a macOS application bundle
The packaging script MUST create a self-contained `Kiln Studio.app` for `osx-arm64` and `osx-x64` with `Contents/MacOS`, `Contents/Resources`, and `Contents/Info.plist`.

#### Scenario: Default macOS packaging
- **WHEN** the packaging script runs on macOS without a RID
- **THEN** it creates a launchable `dist/Kiln Studio.app` for `osx-arm64` by default.

### Requirement: The bundle identity names the application
The bundle MUST set `CFBundleName` and `CFBundleDisplayName` to "Kiln Studio", set the intended `CFBundleIdentifier`, and use the version from project metadata.

#### Scenario: Bundle metadata is checked
- **WHEN** the generated `Info.plist` is read
- **THEN** it contains the visible name "Kiln Studio", an executable value matching the publish output, and the project version.

### Requirement: Icon generation is best effort
The packaging script MUST convert existing PNG icons to `.icns` when possible and MUST create the bundle even when `sips` or `iconutil` is unavailable.

#### Scenario: macOS icon tools are unavailable
- **WHEN** the script runs without one of the icon tools
- **THEN** it reports that icon generation was skipped and still creates the application bundle.

### Requirement: The local application bundle remains unsigned
The local packaging script MUST NOT perform signing, notarization, or release automation.

#### Scenario: The generated bundle is launched
- **WHEN** the user opens the locally generated bundle
- **THEN** macOS may show the usual Gatekeeper notice for an unsigned application.
