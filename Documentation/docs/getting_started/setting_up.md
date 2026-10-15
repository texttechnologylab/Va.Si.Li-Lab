# Setting Up Va.Si.Li-Lab

## Development

### Clone the Repository
`git clone https://github.com/texttechnologylab/Va.Si.Li-Lab.git`

### Open the Project with UnityHub
Unity Hub downloads the correct Unity version (6000.3) for you.
Add Android Build Support if you want to build for Meta Quest headsets.

### Import the required samples
Va.Si.Li-Lab uses code and prefabs from two package samples. Import them in *Window > Package Manager* (select the
package, *Samples* tab, *Import*). Until then you get compile errors such as `SampleAvatarEntity could not be found`.

| Package | Sample |
|---|---|
| Meta Avatars SDK | Sample Scenes |
| Ubiq | Demo (XRI) |

The Meta Avatars sample doesn't compile against Platform SDK 207 (`'CAPI' does not exist in the namespace
'Oculus.Platform'`). In `Assets/Samples/Meta Avatars SDK/40.0.1/Sample Scenes/Scripts/Utility/AvatarEditorUtils.cs`
replace the line `var result = new Request<…>(Oculus.Platform.CAPI.ovr_Avatar_LaunchAvatarEditor((IntPtr)options));`
with:

```csharp
var result = Oculus.Platform.Avatar.LaunchAvatarEditor(options);
```

Also import *TextMesh Pro Essential Resources* when Unity asks.

### Quick-start
1. Run *Tools > Va.Si.Li > Create Endpoints Config*. This creates `Assets/Resources/VasiliEndpoints.asset`, which holds
   the addresses of all servers (Ubiq server, Database API, speech, chatbot, WebRTC). Point it to your own
   [server installation](../server/server_structure.md). For quick tests, set *Ubiq Server* to the
   `Nexus Connection Definition` provided by UCL (features that need our backend will be missing).
2. Open the `MetaStart` scene in `Assets/Lab/Core/Samples/Scenes` and press Play. Without a headset, use the
   Meta XR Simulator.


## Import as Unity Package

Unity can't install git dependencies of a package, so add these to your project's `Packages/manifest.json` yourself:

```json
"com.ucl.ubiq": "https://github.com/UCL-VR/ubiq.git#upm-unity-v1.0.0-pre.16",
"com.tlabaltoh.webview": "https://github.com/TLabAltoh/TLabWebView.git#upm",
"com.tlabaltoh.vkeyborad": "https://github.com/TLabAltoh/TLabVKeyborad.git#upm",
"org.texttechnologylab.vasili": "https://github.com/texttechnologylab/Va.Si.Li-Lab.git#upm"
```

The event system ([Va.Si.Li-ES](../systems.md#vasili-es)) is a separate, optional package:
`"org.texttechnologylab.vasili-es": "https://github.com/texttechnologylab/Va.Si.Li-Lab.git#upm-eventsystem"`.
All other dependencies (Meta XR SDKs, XR Interaction Toolkit, WebRTC, ...) are installed automatically.

Then:

1. Import the [required samples](#import-the-required-samples) and apply the `AvatarEditorUtils.cs` fix.
2. Import the Va.Si.Li-Lab samples (*Package Manager > Va.Si.Li-Lab > Samples*) for the [scenes](special_scenes.md).
3. Fix the remaining issues listed in *Meta XR Project Setup Tool*.
4. Set *Player Settings > Active Input Handling* to *Input System Package (New)*.
5. Continue with the [quick-start](#quick-start).
