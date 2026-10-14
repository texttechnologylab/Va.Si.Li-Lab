# Description
Core components for Va.Si.Li-Lab including Scene/Role/Level-management as well as player controllers, avatars and many more.

## Installation (as a package)
Unity can't install git dependencies of a package, so add these to your project's `Packages/manifest.json` yourself:

```json
"com.ucl.ubiq": "https://github.com/UCL-VR/ubiq.git#upm-unity-v1.0.0-pre.16",
"com.tlabaltoh.webview": "https://github.com/TLabAltoh/TLabWebView.git#upm",
"com.tlabaltoh.vkeyborad": "https://github.com/TLabAltoh/TLabVKeyborad.git#upm",
"org.texttechnologylab.vasili": "https://github.com/texttechnologylab/Va.Si.Li-Lab.git#upm"
```

Optional EventSystem package: `"org.texttechnologylab.vasili-es": "https://github.com/texttechnologylab/Va.Si.Li-Lab.git#upm-eventsystem"`.
All other dependencies (Meta XR SDKs, XRI, WebRTC, …) are installed automatically.

## Required samples
Import these in *Window > Package Manager* (package > *Samples* > *Import*), otherwise the package doesn't compile
(e.g. `SampleAvatarEntity could not be found`):

| Package | Sample |
|---|---|
| Meta Avatars SDK | Sample Scenes |
| Ubiq | Demo (XRI) |

Also import *TextMesh Pro Essential Resources* when Unity asks.

Then create your server config via *Tools > Va.Si.Li > Create Endpoints Config*.
