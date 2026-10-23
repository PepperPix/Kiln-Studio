## Context
This change adds only a local packaging layer. Studio remains a .NET tool; the script additionally creates a native `.app` directory structure from a self-contained publish.

## Decisions
- The script accepts `osx-arm64` or `osx-x64` and uses `osx-arm64` as the default RID.
- It creates `dist/Kiln Studio.app/Contents/MacOS`, `Contents/Resources`, and `Contents/Info.plist` from the publish output.
- The bundle name and display name are "Kiln Studio". The planned bundle identifier is `com.pepperpix.kilnstudio`. The version comes from `Directory.Build.props` and is not hard-coded in the template. Verify the actual executable name from the publish output.
- Generate `.icns` from existing PNG assets using standard macOS tools. If `sips` or `iconutil` is unavailable, continue with a warning and no icon.
- The bundle remains unsigned. CI, DMG, and release automation remain out of scope.
