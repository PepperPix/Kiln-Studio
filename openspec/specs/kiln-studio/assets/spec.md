# kiln-studio/assets Specification

## Purpose
This specification describes the implemented asset workflows, including the site-wide asset library and content-bound page bundle files.

## Requirements
### Requirement: Assets can be browsed as a library or by content
Studio MUST switch the Assets destination between the site-wide library under `static/` and grouped content assets from page bundles.

#### Scenario: The asset view is changed
- **WHEN** the user selects "Library" or "By Content"
- **THEN** Studio shows assets from the corresponding project areas.

### Requirement: The asset library manages project files
Studio MUST list assets in `static/` live from the file system and provide file uploads and folder management.

#### Scenario: A file is added to the library
- **WHEN** the user selects a destination folder and a local file
- **THEN** Studio stores the file under the selected `static/` path.

### Requirement: The editor can select or upload assets
Studio MUST provide an asset picker in the editor to select from the library and upload to the library or the current page bundle.

#### Scenario: An asset is inserted in the editor
- **WHEN** the user selects or uploads an asset
- **THEN** Studio inserts the appropriate Markdown reference at the editor insertion point.

### Requirement: Asset references depend on file type and location
Studio MUST use Markdown image syntax for image files and link syntax for other files, with a path appropriate to the storage location.

#### Scenario: An image or another file is inserted
- **WHEN** an image from a page bundle or any file from `static/` is inserted
- **THEN** Studio uses a relative bundle path or the `/assets/` path for library files, respectively.

### Requirement: The asset manager provides previews and reference actions
Studio MUST provide asset previews and rename, replace, and delete actions with reference protection.

#### Scenario: A referenced file is selected for deletion
- **WHEN** the reference index contains active content references to the file
- **THEN** Studio warns or blocks deletion and can navigate to the referencing content.

### Requirement: Unreferenced library files can be filtered
Studio MUST identify library assets without content references as orphaned and allow them to be filtered.

#### Scenario: The orphan filter is enabled
- **WHEN** the user enables "Orphaned only"
- **THEN** Studio shows only library files without active references.
