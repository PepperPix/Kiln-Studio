## Context
This change moves the existing deployment variant and publishing workflow from Settings and shell commands to a dedicated navigation destination. Configuration remains local to the project in `.kiln/deploy.json`.

## Decisions
- Add an optional latest result to `DeploymentConfig`, including a timestamp, success state, summary, and details. A missing JSON field remains `null` so existing files continue to load.
- Add a `DeploymentViewModel` that loads and saves deployment configuration, selects the primary action based on the variant, and collects runtime log lines.
- CI variants use the existing setup service. The file system variant uses the existing publish service. Show the result in the interface.
- Keep site configuration in Settings and remove its deployment fields. Connect the new view to the active project through the shell.
- Remove the six hard-coded `IsXTargetSelected` properties and compare the selected `NavTarget` generically in the view.
- Log forwarding from the service remains an implementation detail. Options include `IProgress<string>`, `IProgress<DeploymentLogEntry>`, or `IObservable<string>`; none is selected here.

## Boundaries
This change extends the existing Studio UI and service layer. It does not introduce a new deployment target or a history database.
