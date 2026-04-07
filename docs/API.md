# API Reference

## `GET /channels`
Returns all normalized channels.

## `GET /channels/{id}`
Returns a single normalized channel by id.

## `GET /now`
Returns currently airing programs based on UTC now.

## `GET /search?query={q}`
Searches channels by name and program slots by title.

## Response design notes

- DTOs are intentionally compact and provider-agnostic.
- Internal provider IDs are not exposed through API contracts.
