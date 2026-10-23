## 1. Persist results and forward logs
- [ ] 1.1 Add an optional latest result to `DeploymentConfig` and preserve existing JSON compatibility when loading and saving.
- [ ] 1.2 Add progress and log forwarding to deployment and publish services, and report setup and publish steps.

## 2. Add a dedicated Deployment destination
- [ ] 2.1 Implement `DeploymentViewModel` with loading, saving, a variant-based action, busy and status state, results, and logs.
- [ ] 2.2 Remove the Deployment section and dependency from Settings; update Settings tests.
- [ ] 2.3 Create the Deployment view and connect it to NavRail, ShellViewModel, and dependency injection; disable `IsPlaceholder` for Deployment.
- [ ] 2.4 Remove deployment action properties and commands from `ShellViewModel` without changing the Build button.

## 3. Generalize navigation selection
- [ ] 3.1 Create a `NavTarget` comparison converter and replace the six `IsXTargetSelected` bindings and properties, including their change notifications.
- [ ] 3.2 Verify that Deployment navigation does not unexpectedly prompt about unsaved editor changes.

## 4. Add tests
- [ ] 4.1 Add tests for deployment loading, saving, variant actions, logs, and saved results.
- [ ] 4.2 Limit SettingsViewModel tests to site settings.
- [ ] 4.3 Optionally add a UI test for the variant, saving, and `.kiln/deploy.json`.

## 5. Validate in the target repository
- [ ] 5.1 Run `rtk dotnet build` with 0 warnings and 0 errors.
- [ ] 5.2 Run the full `dotnet test` suite without `rtk`; all tests must pass.
- [ ] 5.3 Run `jb inspectcode KilnStudio.slnx -e=WARNING -f=Text` with 0 findings.
- [ ] 5.4 Search for the six old `IsXTargetSelected` names in `src/`; no matches may remain.
- [ ] 5.5 Manually test file system publishing with logs and results after restart, and test CI configuration.
- [ ] 5.6 Manually verify that all six navigation destinations show the correct views.
- [ ] 5.7 Check `rtk git status --short` and `rtk git show --stat HEAD` for unexpected changes.
