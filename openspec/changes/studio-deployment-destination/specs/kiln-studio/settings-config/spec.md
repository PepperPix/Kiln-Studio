## MODIFIED Requirements
### Requirement: Settings also manage deployment configuration
Studio MUST edit only site configuration in the Settings destination and MUST NOT offer deployment options there.

#### Scenario: Deployment configuration is saved in Settings
- **WHEN** the user saves the Settings destination
- **THEN** Studio saves only site configuration; deployment configuration is edited separately in the Deployment destination.
