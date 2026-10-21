# kiln-studio/content-editing Specification

## Purpose
This specification describes the current content workflow and the implemented content and editor interface.

## Requirements
### Requirement: Content is displayed by collection
Studio MUST display content entries grouped by configured collections and allow a collection to be selected for the current list view.

#### Scenario: A collection is selected
- **WHEN** the user selects a collection in the Content view
- **THEN** Studio shows the content entries in that collection.

### Requirement: Content search filters titles and file names
Studio MUST filter the selected content list using a case-insensitive substring search across titles and file names.

#### Scenario: A search term is entered
- **WHEN** the user enters a search term
- **THEN** Studio shows only matching titles or file names after a short search delay.

### Requirement: The content list provides draft filtering and local sorting
Studio MUST allow the list to be filtered by draft status and sorted locally by default order, date, or title without changing the published order.

#### Scenario: Draft filtering and sorting are applied
- **WHEN** the user selects a draft filter or local sort order
- **THEN** only the displayed list changes; the project configuration does not change.

### Requirement: The editor opens a selected content entry
Studio MUST open a selected content entry in the editor with structured fields for title, date, description, and configured taxonomies, as well as a Markdown body editor.

#### Scenario: A list entry is opened
- **WHEN** the user selects a content entry
- **THEN** Studio shows its editable metadata and Markdown body in the editor.

### Requirement: The editor preserves unstructured front matter fields
Studio MUST preserve front matter fields that are not managed by structured input fields when saving.

#### Scenario: A document with additional front matter keys is saved
- **WHEN** structured metadata or the body is edited and saved
- **THEN** front matter keys that are not managed by the form fields remain in the document.

### Requirement: The editor saves front matter and body in the content file
Studio MUST save explicit changes to editable metadata and the Markdown body in the same content file.

#### Scenario: Changed content is saved
- **WHEN** the user selects "Save"
- **THEN** Studio writes the changes to the open Markdown file and clears the dirty state.

### Requirement: The editor protects unsaved changes
Studio MUST prompt the user to save, discard, or cancel before leaving an edited document.

#### Scenario: The user leaves a changed document
- **WHEN** a document has unsaved changes and the user leaves the editor
- **THEN** the user can save the changes, discard them, or cancel navigation.

### Requirement: Draft status is toggled from the content list
Studio MUST allow an entry's draft status to be toggled between published and draft from its context menu.

#### Scenario: Draft status is changed
- **WHEN** the user selects the draft action for a content entry
- **THEN** Studio updates its YAML front matter and reloads the current list filter.

### Requirement: New content pages are created as drafts
Studio MUST use the "New" action in a collection to create and open a Markdown page with draft status.

#### Scenario: A new page is created
- **WHEN** the user confirms a title for a new page
- **THEN** Studio creates the content entry in the selected collection and opens it in the editor.

### Requirement: The editor provides document statistics and switchable secondary views
Studio MUST provide Details, Statistics, Preview, and document-specific Assets views in the right editor pane.

#### Scenario: An editor secondary view is selected
- **WHEN** the user selects a tab in the right editor pane
- **THEN** Studio shows the corresponding document information, preview, or asset view.
