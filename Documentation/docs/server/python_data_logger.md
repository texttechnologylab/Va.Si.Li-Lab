# Database API

Serves the scene, level and role definitions the VR client loads, and provides
the logging endpoints that all tracking and experiment data is written through
into [MongoDB](mongodb.md).

Previously referred to here as the *Python Data Logger*. It is available now, in
the [Va.Si.Li-Lab-backend](https://github.com/texttechnologylab/Va.Si.Li-Lab-backend)
monorepo under `database-api/`.

!!! info "Full documentation"
    Endpoints, environment variables, authentication and the batched
    `/logging/player` payload are documented at
    **[Database API](https://texttechnologylab.github.io/Va.Si.Li-Lab-backend/database_api/)**
    in the backend documentation.

## Port

Defaults to `5000`, configurable with the `PORT` environment variable.

## Authentication

Every endpoint requires a shared secret header:

```
X-API-KEY: <value of X_API_KEY>
```

!!! warning "A wrong key loses the session silently"
    Logging failures do not interrupt an experiment. The session runs normally
    and records nothing. Verify data is arriving before a participant sits down.

## Unity

`MetaDataLogger > http://ip:5000`

Set the same API key in the Va.Si.Li API asset.
