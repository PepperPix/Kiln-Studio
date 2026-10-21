## ADDED Requirements
### Requirement: Deployment has its own navigation destination
Studio MUST provide a dedicated configuration and action view in the Deployment destination instead of a placeholder.

#### Scenario: The Deployment destination is selected
- **WHEN** the user selects "Deployment" in project navigation
- **THEN** Studio shows the current deployment configuration and its actions.

### Requirement: The deployment variant determines the primary action
For CI variants, Studio MUST generate the workflow configuration. For the file system variant, Studio MUST publish the site build.

#### Scenario: The primary deployment action is started
- **WHEN** the user runs the primary action for a saved variant
- **THEN** Studio starts the setup or publish service associated with that variant.

### Requirement: Deployment results and live logs are displayed
Studio MUST display log lines during a deployment action and persist the latest result with its timestamp, success state, summary, and details.

#### Scenario: A deployment action is complete
- **WHEN** the action completes successfully or with an error
- **THEN** Studio displays the result and log lines and saves the latest result in `.kiln/deploy.json`.

### Requirement: Existing deployment configuration remains readable
Studio MUST load existing `.kiln/deploy.json` files that do not contain a result field.

#### Scenario: A file from an older version is loaded
- **WHEN** `.kiln/deploy.json` does not contain a saved latest result
- **THEN** Studio loads the deployment options with an empty result.
