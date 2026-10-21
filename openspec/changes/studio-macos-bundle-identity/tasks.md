## 1. Package a macOS application bundle
- [ ] 1.1 Implement `scripts/package-macos-app.sh` with RID selection (`osx-arm64` or `osx-x64`, default `osx-arm64`) and self-contained `dotnet publish`.
- [ ] 1.2 Create the `.app` directory structure and `Info.plist` from a template file; read the version from `Directory.Build.props` and verify the executable name from the publish output.
- [ ] 1.3 Best-effort convert existing PNG icons to `AppIcon.icns` with `sips` and `iconutil`; continue with a warning when either tool is unavailable.
- [ ] 1.4 Document the local unsigned `.app` workflow and expected Gatekeeper notice in `README.md`.

## 2. Validate in the target repository
- [ ] 2.1 Run `bash scripts/package-macos-app.sh` and create `dist/Kiln Studio.app`.
- [ ] 2.2 Successfully run `plutil -lint "dist/Kiln Studio.app/Contents/Info.plist"`.
- [ ] 2.3 Run `rtk dotnet build` with 0 warnings and 0 errors.
- [ ] 2.4 Run the full `dotnet test` suite without `rtk`; all tests must pass.
- [ ] 2.5 Run `jb inspectcode KilnStudio.slnx -e=WARNING -f=Text` with 0 findings.
- [ ] 2.6 On macOS, launch the bundle with `open "dist/Kiln Studio.app"` and verify the menu bar name is "Kiln Studio".
- [ ] 2.7 Check `rtk git status --short` and `rtk git show --stat HEAD`; do not change `.github/workflows/`.
