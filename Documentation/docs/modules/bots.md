# Bots

Va.Si.Li-Lab has LLM-based agents that users talk to by voice and that can act in the scene through *tools*. The code
is in `Assets/Lab/Core/Runtime/Scripts/LLM` (namespace `VaSiLi.Llm`). The agents need two backend services:
[vr-agent](https://texttechnologylab.github.io/Va.Si.Li-Lab-backend/services/vr_agent/) (`agent` in the [endpoints](endpoints.md)), which talks to the LLM, and
[tts-stt](https://texttechnologylab.github.io/Va.Si.Li-Lab-backend/services/tts_stt/) (`speechSynthesis`) for speech in and out.

## Prefabs
Both are samples (`Assets/Lab/Core/Samples/Objects/Prefabs`):

| Prefab | Contents |
|---|---|
| `AgentToolkit` | One per scene: `AgentManager` (creates the agents), `AgentToolProvider` (collects the tools), `PlayerMicListener` (push-to-talk) and `ToolExamples` |
| `AgentBot` | One agent: the bot model, `AgentInstance`, `ChatAPI` (speech in and out) and its UI (`AgentActivatorUI`, `ToolWhitelistUI`, `ToolLogUI`) |

`MetaStart` contains an `AgentToolkit`; `AgentManager` spawns `AgentBot` instances from it.

## How it works
1. `AgentManager` creates the agents listed in its *Agents* field: for each it instantiates the `AgentBot` prefab at the
   given position and registers the agent with the agent service (`/createAgent`). The system prompt is the agent's
   *Persona* followed by the tool-call instructions. Set the list on the `AgentToolkit` instance of each scene.
2. The user activates an agent (`AgentActivatorUI`) and talks to it. `PlayerMicListener` records while *A* (or the
   right index pinch, or *V* on a keyboard) is held and passes the recording to every active agent. It only records
   while at least one agent is active.
3. `ChatAPI` transcribes the audio (`speechSynthesis/whisperx`) and sends the text to the agent (`agent/chat`).
4. The agent answers with text, which `ChatAPI` speaks (`speechSynthesis/tts`), or with a tool call.
5. `AgentInstance` runs tool calls the user has allowed for that agent (`ToolWhitelistUI`) and logs them (`ToolLogUI`).

## Tools
A tool is any method marked with `[AgentTool]` on a `MonoBehaviour` in the scene. `AgentToolProvider` finds them by
reflection and sends their descriptions to the agent service (`/toolDescriptions`):

```csharp
using UnityEngine;
using VaSiLi.Llm;

public class LampTools : MonoBehaviour
{
    public Light lamp;

    [AgentTool("switch_lamp", "Call when the user wants the lamp switched on or off.")]
    public string SwitchLamp(bool on)
    {
        lamp.enabled = on;
        return on ? "The lamp is on" : "The lamp is off";
    }
}
```

Parameters can be `string`, `int`, `float`, `double`, `bool` and `Vector3`; for other types the agent passes the name of an
earlier tool call, and that call's return value is used. The returned string goes back to the agent as the result. `ToolExamples` has more examples (spawning prefabs,
changing colours and textures). Components that implement `IAgentContextInfo` add information about the scene to
the agent's context.

## Chatbot module
`Assets/Lab/Core/Runtime/Scripts/Modules/Chatbot` is an older, simpler voice chatbot: `ChatBotManager` sends recorded
audio to `speech/query` and plays the answer, `ChatBotSelectButton` picks the chatbot. It uses the `speech` service.
