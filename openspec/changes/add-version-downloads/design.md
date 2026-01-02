## Context
The app needs a stable release list for downloads and a simplified About page. The update channel is GitHub Releases for the project repository.

## Goals / Non-Goals
- Goals: show stable releases with required fields; provide a download action; keep About page concise.
- Non-Goals: implement auto-updater or background update checks.

## Decisions
- Decision: use GitHub Releases API as the release source.
- Decision: filter out prerelease entries and sort by published date descending.
- Decision: derive release summary from the first non-empty line of the release notes.

## Risks / Trade-offs
- External API availability; mitigate by showing a user-facing load error and allowing retry.

## Migration Plan
- Add new model and service; wire into GUI; update About view text.

## Open Questions
- Confirm release repository URL used for the API and update channel link.
- Confirm the list of "common resolutions" for UI layout checks.
