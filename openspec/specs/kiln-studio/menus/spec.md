# kiln-studio/menus Specification

## Purpose
This specification describes the implemented visual menu editor and its project navigation destination. It is based on loading and saving the `menus` configuration in `site.yaml` and the Studio interface.

## Requirements
### Requirement: Named menus can be managed
Studio MUST allow multiple named menus for a site to be listed, created, renamed, and deleted.

#### Scenario: A menu is added
- **WHEN** the user creates a menu and enters a name
- **THEN** it appears as a selectable menu in the editor.

### Requirement: Menu entries support references and URLs
Studio MUST allow menu entries with a title and an internal `ref` or `url` destination to be edited.

#### Scenario: A menu link is edited
- **WHEN** the user selects a reference or URL link for an entry
- **THEN** Studio shows the appropriate input fields and saves the link type.

### Requirement: Menus support nested entries
Studio MUST allow menu entries to be added, deleted, reordered, and moved between sibling and child levels.

#### Scenario: An entry is nested
- **WHEN** the user indents an entry or moves it under another entry
- **THEN** it appears as a child entry in the menu tree.

### Requirement: Menu references are validated
Studio MUST show invalid or unresolved `ref` values on the affected entries without generally preventing valid content from being saved.

#### Scenario: A reference cannot be resolved
- **WHEN** a menu entry refers to an unknown collection or slug
- **THEN** Studio shows a validation notice on that entry.

### Requirement: Menus are saved in the site configuration
Studio MUST save the menu block to `site.yaml` or `site.yml` and preserve the remaining configuration values.

#### Scenario: Menu changes are saved
- **WHEN** the user selects "Save"
- **THEN** Studio writes the menu definitions to the site configuration and loads them again the next time it opens.
