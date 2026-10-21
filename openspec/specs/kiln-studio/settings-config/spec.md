# kiln-studio/settings-config Specification

## Purpose
This specification describes the current site settings behavior, including deployment settings and the switch between the basic form and the advanced `site.yaml` editor.

## Requirements
### Requirement: Site settings can be edited with structured fields
Studio MUST allow the site title, description, base URL, and language to be edited in the basic form.

#### Scenario: Basic values are saved
- **WHEN** the user changes basic values and selects "Save"
- **THEN** Studio writes the edited site settings to the project configuration.

### Requirement: Advanced site configuration remains editable as raw YAML
Studio MUST provide an Advanced mode that displays and can save the site configuration as raw YAML.

#### Scenario: Advanced configuration is edited
- **WHEN** the user enables Advanced mode and saves
- **THEN** Studio writes the YAML content to the site configuration file.

### Requirement: The basic form preserves unmanaged site configuration
Studio MUST preserve site configuration values outside the managed basic fields when the basic form is saved.

#### Scenario: Basic fields are saved
- **WHEN** the user changes only the title, description, base URL, or language
- **THEN** other site configuration entries such as collections, menus, and taxonomies remain intact.

### Requirement: Settings also manage deployment configuration
Studio MUST allow the deployment variant and its file system options to be edited in Site Settings and saved to `.kiln/deploy.json`.

#### Scenario: Deployment configuration is saved in Settings
- **WHEN** the user changes a deployment variant or file system path in Settings and saves
- **THEN** Studio reads or writes these values in `.kiln/deploy.json`.

### Requirement: Settings report invalid language input
Studio MUST show a notice when the entered language code is invalid and report the save status.

#### Scenario: An invalid language code is entered
- **WHEN** the language input is not a valid language code
- **THEN** Studio shows a validation notice.
