## Why
Deployment configuration is currently in Settings, while the deployment actions are split between the ShellViewModel and a placeholder navigation view. A dedicated destination brings configuration, CI workflow generation, and file system publishing together with their results and live logs.

## What Changes
- Move deployment configuration from Settings to a dedicated Deployment navigation destination.
- Show "Generate deployment config" or "Publish" depending on the deployment variant, with the result and log.
- Persist the latest deployment result in `.kiln/deploy.json` while remaining backward compatible.
- Replace the six `IsXTargetSelected` properties with a generic destination selection.
- Do not redesign the engine, add separate deployment history, or add SFTP.

## Capabilities
### New Capabilities
- `kiln-studio/deployment`

### Modified Capabilities
- `kiln-studio/settings-config`

## Impact
- Target repository: `PepperPix/Kiln-Studio`, on a feature branch from `beta`.
- Affected areas: `Kiln.Studio.Services` (deployment configuration and services), `Kiln.Studio.ViewModels` (deployment view and shell), Avalonia views, and service, view-model, and UI tests.
- Out of scope: engine changes, removing the toolbar build action, separate history storage, and SFTP.
