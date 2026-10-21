# kiln-studio/themes Specification

## Purpose
This specification describes the implemented theme manager and its navigation destination. It covers theme selection, installation, duplication, and file-based preview.

## Requirements
### Requirement: Available themes can be selected
Studio MUST list the project themes and allow the active theme to be selected in `site.yaml`.

#### Scenario: Another theme is applied
- **WHEN** the user selects an available theme and selects "Apply"
- **THEN** Studio saves its name as the active site theme.

### Requirement: Theme files can be viewed without editing
Studio MUST list files in the selected theme and preview their text or image content without allowing the files to be edited in the manager.

#### Scenario: A theme file is selected
- **WHEN** the user selects a theme file in the browser
- **THEN** Studio shows its content in a read-only preview.

### Requirement: Themes can be installed from ZIP files
Studio MUST install a theme from a selected ZIP file into the project and validate that the ZIP contains a theme structure with layouts.

#### Scenario: An invalid theme ZIP file is imported
- **WHEN** the selected ZIP file does not contain a valid layout structure
- **THEN** Studio reports the error and does not retain an incomplete theme directory.

### Requirement: A theme can be duplicated
Studio MUST copy an existing theme under a new, unique name and make the copy selectable.

#### Scenario: The current theme is duplicated
- **WHEN** the user enters a name that is not already in use
- **THEN** Studio copies the theme files and allows the copy to be activated.

### Requirement: Theme configuration remains outside the theme manager
Studio MUST NOT provide direct editing of theme files or `themeConfig` values in the theme manager.

#### Scenario: A theme file is displayed
- **WHEN** the user uses the theme browser
- **THEN** the user can view files but cannot change them in Studio.
