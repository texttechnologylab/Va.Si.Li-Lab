# General

The backend consists of two required services and a number of optional ones,
depending on the scenario.

| Service | Purpose | |
|---|---|---|
| [Ubiq Server](ubiq_server.md) | Room and session state - avatars, transforms, ownership | required |
| [Database API](python_data_logger.md) | Scene definitions and the logging API, backed by [MongoDB](mongodb.md) | required |
| Chatbot, Speech, VR Agent | Conversational agents, speech-to-text and text-to-speech | optional |

All of them live in the
[Va.Si.Li-Lab-backend](https://github.com/texttechnologylab/Va.Si.Li-Lab-backend)
monorepo.

!!! info "Backend documentation"
    Each module - its endpoints, environment variables and deployment - is
    documented at
    **[texttechnologylab.github.io/Va.Si.Li-Lab-backend](https://texttechnologylab.github.io/Va.Si.Li-Lab-backend/)**.

    The pages in this section cover what the Unity client needs from each
    service; the backend docs cover the services themselves.

## Deploying the full stack

A standardised Docker Compose deployment exists as part of
[InterView](https://texttechnologylab.github.io/InterView/), which brings the
services up together - the Ubiq room server, the Database API, a Janus WebRTC
gateway, an optional TURN relay and a web client - with versioned configuration
instead of individually started containers.

!!! tip "Start here for a full deployment"
    The [InterView Setup Guide](https://texttechnologylab.github.io/InterView/setup/)
    walks through the whole stack step by step, including the prerequisites that
    are easy to miss: an external MongoDB, DNS records, firewall port ranges, and
    a TLS-terminating reverse proxy that correctly forwards WebSocket upgrades.

The stack was built for the FACES project, but nothing in it is FACES-specific
apart from the web client - the same deployment serves any Va.Si.Li-Lab scenario
that needs a room server and a logging backend.
