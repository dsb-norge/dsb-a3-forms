# DSB NuGet Package for Altinn 3 forms

> ### Publishing
> To publish a new version to NuGet, create a new release manually. This will trigger a workflow which builds and publishes the main branch to the NuGet Gallery. Releases are created automatically when Renovate dependency updates are merged to main.


## Clients

- **AuthClient** - OAuth 2.0 token acquisition 
- **BrregClient** - Norwegian business register lookups with caching
- **GeoNorgeClient** - Uses GeoNorge API to retrieve municipalities and perform search by street address or coordinates
- **InputValidatorClient** - Validation for phone, email, postal code, org number, SSN, age
- **MottakstjenesteClient** - Retrieves country option from internal application
- **SaftClient** - Integration with internal civil defence application