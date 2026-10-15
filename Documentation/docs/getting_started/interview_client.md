# InterView VR Client

[InterView](https://texttechnologylab.github.io/InterView/) is a platform for
conducting survey interviews in virtual reality, built on Va.Si.Li-Lab. The
participant wears a headset and sits across from the interviewer's avatar; the
interviewer works from an ordinary web browser.

This page covers **setting up the VR client** - the Unity side. The platform
itself, its services and their deployment are documented separately:

!!! info "Platform documentation"
    Architecture, server setup, Janus, coturn, Ubiq, the logging API and the web
    client are documented at
    **[texttechnologylab.github.io/InterView](https://texttechnologylab.github.io/InterView/)**.

## Prerequisites

- Unity project set up as described in [Setting Up Va.Si.Li-Lab](setting_up.md),
  with Android support for Quest builds
- A running InterView backend - see the
  [InterView Setup Guide](https://texttechnologylab.github.io/InterView/setup/)
- A Meta Quest headset in developer mode

## What the client connects to

Four independent services, all configured in the [endpoints file](../modules/endpoints.md)
(`Assets/Resources/VasiliEndpoints.asset`). Each can fail without the others noticing.

| Service | Purpose | Endpoints field |
|---|---|---|
| Janus | Audio/video and the control data channel | `janus`, `iceServers` |
| Ubiq | VR room and avatar state | `ubiqServer` |
| Logging API | Tracking data and events | `api` |
| Questionnaire | The survey instrument | `questionnaire` |

## Configuration

The InterView sample scenes are in `Assets/Lab/Core/Samples/Scenes`: `InterViewStart` (token entry, holds the
`InterviewTokenManager`), `InterView` (the interview room, holds the `WebCamClient`) and `InterViewReturn`. Add all
three to the build profile, `InterViewStart` first, and check the components below.

### `WebCamClient`

The Janus client. Publishes local audio and video, subscribes to the
interviewer's feed, and hosts the textroom data channel that carries chat,
answer options, gaze updates and the recording signal. Its server comes from
`janus` in the endpoints: use `wss://janus.<your-domain>`, the TLS endpoint of your
reverse proxy, not Janus's own port. `iceServers` must match the TURN server the
backend uses.

| Field | Value | Notes |
|---|---|---|
| `SubProtocol` | `janus-protocol` | Leave unchanged |
| `RecordingPath` | `/mnt/recordings/` | Path **inside the Janus container**; the room ID is appended automatically |
| `captureWidth` / `captureHeight` | `854` × `480` | |
| `maxBitrate` / `minBitrate` | `1000000` / `250000` | |
| `maxFramerate` | `24` | |

### `InterviewTokenManager`

Holds the participant token across scene loads and derives every identifier from
it, so the headset and the interviewer's browser meet without a coordination
step.

The questionnaire URL is `questionnaire` in the endpoints. The token is appended as `?token=…&room=…`. The follow-up
questionnaire that the interviewer can send afterwards is `followupQuestionnaire`.

```
token ──┬─→ RoomId         (Janus room ID)
        ├─→ UbiqRoomName   = "interview-{token}"
        └─→ BrowserUrl     = {questionnaire}?token={token}&room={token}
```

The room ID is the token parsed as an integer when it is all digits, and
otherwise a DJB2 hash (seed 5381, multiplier 33), absolute value, minimum 1.

!!! danger "This derivation exists twice and must stay identical"
    The same function is implemented in the web client as `roomIdFromString()`
    in `js/state.js`. If the two ever disagree, the interviewer and the
    participant join **different rooms** - each waits alone, and no error is
    shown anywhere. Change both or neither.

### Ubiq connection

Set `ubiqServer` in the endpoints to a Ubiq connection definition with your Ubiq host
and its published TCP port (**16485** in the default deployment).

### Logging API

Set `api` in the endpoints to your Database API URL.

!!! warning "A wrong API address loses the whole session, silently"
    Logging failures do not interrupt an interview. The session looks completely
    normal and records nothing. Verify that data is arriving **before** a
    participant sits down, not afterwards.

## Building for Quest

1. Switch the build target to Android.
2. Resolve any outstanding items in the Meta Project Setup Tool.
3. Build and deploy to the headset.

## Pre-session checklist

- [ ] `janus` reachable from the headset's network
- [ ] TURN credentials current and matching the server
- [ ] Ubiq host and port correct
- [ ] `api` points at the running Database API
- [ ] `questionnaire` points at the correct questionnaire
- [ ] A full test interview completed, with data confirmed in the database

## Troubleshooting

Symptoms and their usual causes - connection failures, missing media, data
channel problems - are collected in the
[InterView operations guide](https://texttechnologylab.github.io/InterView/operations/#troubleshooting).
