# Endpoints Configuration

All server addresses of the client live in one asset, `VasiliEndpoints`
(`Assets/Lab/Core/Runtime/Scripts/Networking/VasiliEndpoints.cs`). It is loaded from any `Resources` folder by the
name `VasiliEndpoints`.

The repository doesn't ship one. Create yours with *Tools > Va.Si.Li > Create Endpoints Config*, which writes
`Assets/Resources/VasiliEndpoints.asset`. Until then, every service falls back to `localhost` and a warning is
logged at start-up.

!!! warning "Don't commit your endpoints file to a public repository"
    It contains your server addresses and TURN credentials. Keep it in a private folder or repository.

## Fields

| Field | Default | Used by | Without it |
|---|---|---|---|
| `ubiqServer` | `127.0.0.1:8009` (TCP) | `VasiliServerConnection` on the `RoomClient` of the `Social Network Scene` prefab | No multiplayer room |
| `api` | `http://localhost:8000` | [Database API](../server/python_data_logger.md): scene definitions (`SceneManager`), [logging](logging.md), tutorials, connection status | No scenes in MetaStart, nothing is logged |
| `speech` | `http://localhost:8001` | [speech2text](https://texttechnologylab.github.io/Va.Si.Li-Lab-backend/services/speech/): `MicrophoneListener` (`/whisper`); [chatbot host](https://texttechnologylab.github.io/Va.Si.Li-Lab-backend/services/chatbot/): `ChatBotManager` (`/query`). Both are expected at this address | No speech input |
| `agent` | `http://localhost:8002` | [vr-agent](https://texttechnologylab.github.io/Va.Si.Li-Lab-backend/services/vr_agent/): `AgentManager`, `AgentInstance`, `AgentToolProvider`, `ChatAPI` (`/chat`), see [Bots](bots.md) | No agents |
| `speechSynthesis` | `http://localhost:8003` | [tts-stt](https://texttechnologylab.github.io/Va.Si.Li-Lab-backend/services/tts_stt/): `ChatAPI` transcription (`/whisperx`) and text-to-speech (`/tts`) | Agents don't hear or speak |
| `shapeNet` | `http://localhost:8004` | ShapeNet object search and download (`ShapeNetInterface`) | No ShapeNet objects |
| `eventTemplates` | `http://localhost:4567` | [Event System](eventsystem.md) templates (`/defaultTemplates`) | No saved or default event systems |
| `questionnaire` | *(empty)* | [InterView](../getting_started/interview_client.md): questionnaire page opened in the in-world browser (`InterviewTokenManager`) | Empty browser in InterView |
| `followupQuestionnaire` | *(empty)* | InterView: follow-up questionnaire the interviewer sends to the interviewee (`WebCamClient` follow-up button; the interview token is appended) | The follow-up button logs a warning and sends nothing |
| `janus` | `ws://localhost:8188` | WebRTC (Janus) for InterView and the webcam screen (`WebCamClient`) | No audio/video with the interviewer |
| `iceServers` | *(empty)* | STUN/TURN servers for WebRTC. Leave `username`/`credential` empty for STUN | WebRTC only works on the same network |

Base URLs are written without a trailing slash. Use `wss://` for Janus behind a TLS reverse proxy.

## In code

```csharp
using VaSiLi.Networking;

string url = VasiliEndpoints.Current.api + "/logging/status";
```

`VasiliEndpoints.Current` is loaded once and cached. To add a new service, add a field with a `localhost` default and
a tooltip to `VasiliEndpoints`, and read it through `Current`. Don't hard-code hosts:
`Tools/Publish/check-public.sh` reports them.
