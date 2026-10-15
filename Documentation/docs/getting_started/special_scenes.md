# Special Scenes
The sample scenes are in `Assets/Lab/Core/Samples/Scenes`. When Va.Si.Li-Lab is installed as a package, import them
via *Package Manager > Va.Si.Li-Lab > Samples*.

## MetaStart
Starting scene for every experiment. It contains the [player](player_controller.md), the scene selector and the
network connection. Here the user selects an avatar and joins a scenario.

## MetaReturn
Helper scene the user returns to when disconnecting from a scenario (instead of MetaStart).
Some scripts that would otherwise cause problems are removed here.

## Tutorial
Tutorial scene to get familiar with the controls. Can be started from MetaStart.

## Example
Minimal scenario scene (networking and a box of pens) to copy when [adding a scene](adding_scenes.md).

## InterViewStart, InterView, InterViewReturn
The scenes of [InterView](../systems.md#interview), used instead of MetaStart/MetaReturn for VR interviews:

- `InterViewStart`: the participant enters the interview token (`InterviewTokenManager`).
- `InterView`: the interview room with the interviewee's seat, a mirror (self-view), the in-world browser for the
  questionnaire and the WebRTC (Janus) connection to the interviewer's browser.
- `InterViewReturn`: shown after the interview.

See [InterView VR Client](interview_client.md) for their configuration.
