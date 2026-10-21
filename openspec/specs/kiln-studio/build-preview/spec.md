# kiln-studio/build-preview Specification

## Purpose
This specification describes the implemented build and preview workflow, including status and editor behavior.

## Requirements
### Requirement: A site build can be started from the shell
Studio MUST allow a build of the open project to be started from the shell in selectable Debug or Release mode.

#### Scenario: A Debug or Release build is started
- **WHEN** the user starts a build with or without the Release option
- **THEN** Studio passes the selected mode to the build service.

### Requirement: The build result is shown in the status area
Studio MUST show build progress and the success or failure result in the status area.

#### Scenario: A build is complete
- **WHEN** the build service returns a result or an exception occurs
- **THEN** Studio shows the result or error message and clears the busy state.

### Requirement: Full preview opens the system browser
Studio MUST be able to start the Kiln development server for the active project and open its URL in the system browser.

#### Scenario: Preview is started
- **WHEN** the user starts full preview
- **THEN** Studio saves open editor content when needed, starts the server, and opens its URL.

### Requirement: Preview saves subsequent editor changes
Studio MUST save changed documents after a short delay while full preview is running.

#### Scenario: A document is changed during preview
- **WHEN** an open document is changed while the development server is running
- **THEN** Studio saves it after a delay so that the development server can reload the change.

### Requirement: The development server ends with the project session
Studio MUST stop the running preview when the active project session is stopped or changed.

#### Scenario: Another site is opened
- **WHEN** the user changes or closes the active project
- **THEN** Studio stops the previous development server.
