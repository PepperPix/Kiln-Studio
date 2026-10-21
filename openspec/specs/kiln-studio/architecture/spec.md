# kiln-studio/architecture Specification

## Purpose
This specification describes the stable architecture contracts of Kiln Studio, based on the implementation.

## Requirements
### Requirement: Studio separates presentation, view models, and services
The solution MUST keep the UI, UI-independent view models, and Studio services in separate projects.

#### Scenario: View-model logic is testable without Avalonia views
- **WHEN** a view model is tested without the Avalonia user interface
- **THEN** it can be used from the view-model or service project without requiring view-control types.

### Requirement: Studio uses Kiln Core in-process
Studio MUST execute Kiln functionality through typed Core services rather than by starting the CLI process.

#### Scenario: A Studio feature uses the engine
- **WHEN** Studio performs an engine operation
- **THEN** it accesses the reused Kiln Core services within the Studio process.

### Requirement: The file system is the source of truth for projects
Studio MUST read site configuration, content, themes, and assets from project files and save them there.

#### Scenario: A project is reopened
- **WHEN** a change has been saved and the project is reloaded
- **THEN** the loaded state comes from the Kiln project files.

### Requirement: Content is edited without a persistent Studio database
Studio MUST read content directly from the project file tree and MUST NOT rely on a persistent content cache as the source of truth.

#### Scenario: The content list is populated
- **WHEN** Studio loads a project
- **THEN** it discovers content from project files, not from a separate content database.

### Requirement: Engine errors are handled at the UI boundary
Studio MUST execute engine operations so that errors are returned to the interface as a status or result instead of propagating through the UI.

#### Scenario: An engine operation fails
- **WHEN** a build or preview operation returns an error
- **THEN** Studio displays an error message in the status or preview area.
