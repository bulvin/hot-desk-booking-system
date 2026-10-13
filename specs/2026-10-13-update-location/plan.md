# Update location

## CONTEXT

`Location` already has a name and an owned address containing street, building number, city, and postal code. Creation uses FluentValidation and rejects duplicate locations with matching name and all address fields. The repository's current duplicate query includes the location itself, so update checks need to exclude the target ID.

## GOAL

Allow an administrator to replace an existing location's name and full address while retaining its ID, desks, and reservations.

- Add `PUT /api/locations/{id:guid}` with a body containing `name` and `address` (`street`, `buildingNumber`, `city`, `postalCode`). This is a full update, not a partial patch or an upsert.
- Return `200 OK` with the updated `LocationDto`. Return `404` for an unknown location, `400` for invalid input, `409` for a duplicate, `401` without authentication, and `403` for an authenticated user without administrator access.
- Add the request model and endpoint in Web.Api;
- Validate required name and address fields through FluentValidation. Preserve existing address limits: street 120, building number 16, city 100, and postal code 16 characters. Handle a missing address without dereferencing null. Do not introduce new name limits or normalization rules in this feature.
- Load the target, check duplicate name/address combinations while excluding its ID, apply the update, and save through the existing unit of work. Preserve creation's duplicate behavior. An unchanged submission must succeed.
- Preserve existing exact-match duplicate semantics; case folding, whitespace normalization, and new database uniqueness constraints are outside this plan.
- Update endpoint metadata and Swagger schemas to match runtime responses.
- Use the existing entity and owned-address schema.

## DEFINITION OF DONE

- An authenticated administrator can update both name and address, and the values persist. The location ID and associated desks and reservations remain intact.
- An unchanged update succeeds; a duplicate of another location returns `409` without persisting changes. Existing location creation duplicate checks continue to work.
- Unknown IDs return `404`. Missing/empty required fields, a missing address, and address values exceeding their limits return `400` without changes. Malformed bodies follow the established HTTP error contract.
- Unauthenticated callers receive `401`; employees receive `403`. Neither can modify location data.
- Focused validator and handler tests cover success, no-op updates, duplicate self-exclusion, duplicate conflicts, missing locations, and invalid input. Endpoint tests cover authorization, response bodies/statuses, and Problem Details. Persistence verification confirms the update and preservation of relationships.
- Restore, Release build with warnings enforced, tests, and formatting verification pass using the documented repository commands. No broad diagnostic suppressions or unnecessary dependencies are added.
- Swagger describes the endpoint's request, success response, and error contracts accurately. Relevant API documentation is updated, and the roadmap checkbox is marked complete only after implementation and verification.
