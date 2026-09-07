# General

There are currently 4 different services running in the background,
each responsible for different functions.
Not all of them are yet available as Docker images.

The pages in this section document each service individually.

## Deploying the full stack

A standardised Docker Compose deployment now exists as part of
[InterView](https://texttechnologylab.github.io/InterView/), which brings up the
services together — the Ubiq room server, the logging and scene API, a Janus
WebRTC gateway, an optional TURN relay, and the web client — with versioned
configuration instead of individually started containers.

!!! tip "Start here for a full deployment"
    The [InterView Setup Guide](https://texttechnologylab.github.io/InterView/setup/)
    walks through the whole stack step by step, including the prerequisites that
    are easy to miss: an external MongoDB, DNS records, firewall port ranges, and
    a TLS-terminating reverse proxy that correctly forwards WebSocket upgrades.

    The compose file and service configurations live in the
    [InterView repository](https://github.com/texttechnologylab/InterView).

The stack was built for the FACES project, but nothing in it is FACES-specific
apart from the web client — the same deployment serves any Va.Si.Li-Lab scenario
that needs a room server and logging backend.
