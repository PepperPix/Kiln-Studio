# kiln-studio/project-shell Specification

## Purpose
This specification describes the current Studio project lifecycle and navigation shell.

## Requirements
### Requirement: The welcome view provides project start actions
Studio MUST show the Welcome view without an open project, with actions to create or open a project and a list of recent projects.

#### Scenario: Studio starts without a project
- **WHEN** no project is open
- **THEN** the user can create or open a project, or select one from the Recent list.

### Requirement: The project bar appears only when a project is open
Studio MUST show the project-specific header and toolbar only when a project is open.

#### Scenario: A site is opened
- **WHEN** a project is open
- **THEN** the shell shows the project name and actions to switch or close the project, open its folder, refresh, and build.

### Requirement: A project switch updates the project shell
Studio MUST update the active project name and displayed project state after a project is opened or switched successfully.

#### Scenario: Another site is opened
- **WHEN** a valid project path is opened
- **THEN** the shell loads its content and shows its name as the active project.

### Requirement: Closing a project returns to the welcome view
Studio MUST clear project-specific views and return to the Welcome view when a project is closed.

#### Scenario: An open site is closed
- **WHEN** the user selects "Close" and resolves any unsaved changes
- **THEN** Studio shows the Welcome view without an active project.

### Requirement: The navigation bar provides fixed project destinations
With a project open, Studio MUST provide Content, Assets, Menus, Theme, Deployment, and Settings destinations in a collapsible navigation bar.

#### Scenario: A navigation destination is selected
- **WHEN** the user selects a destination in the navigation bar
- **THEN** Studio selects that destination and shows its view.

### Requirement: Deployment remains a placeholder destination
Studio MUST show the Deployment navigation destination as a placeholder and MUST NOT provide a separate Deployment view there.

#### Scenario: Deployment is selected in navigation
- **WHEN** the user selects "Deployment"
- **THEN** Studio shows the placeholder view.
