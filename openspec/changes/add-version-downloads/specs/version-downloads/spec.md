## ADDED Requirements
### Requirement: Stable release list
The system SHALL display a list of available stable releases for download.

#### Scenario: Show stable releases
- **WHEN** the release feed is retrieved
- **THEN** only stable releases are listed
- **AND** each entry shows version number, publish date, supported platform, and a release summary
- **AND** the list is sorted by publish date in descending order

### Requirement: Release download action
The system SHALL provide a download action for each listed release.

#### Scenario: Open a release download
- **WHEN** the user activates the download action for a release
- **THEN** the system opens the download URL for that release