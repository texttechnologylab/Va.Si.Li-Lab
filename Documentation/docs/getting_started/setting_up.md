# Setting Up Va.Si.Li-Lab

## Development

### Clone the Repository
`git clone https://github.com/texttechnologylab/Va.Si.Li-Lab.git`

### Open the Project with UnityHub
Unity Hub downloads the correct Unity Version for you. 
Add Android Support, if you want to build for Oculus VR Devices.


## Import as Unity Package (Work in Progress)

### Unity
Install Ubiq as a git package in the Unity Package Manager
```
https://github.com/UCL-VR/ubiq.git#upm-unity-v1.0.0-pre.14
```
Install Va.Si.Li-Lab
```
https://github.com/texttechnologylab/Va.Si.Li-Lab.git#upm
```

Import Meta Avatar Samples from the Meta Avatars SDK(UPM).

Fix all remaining issues listed in the Meta Project Setup Tools.

(WIP) Ensure Active Input Handling(Player Settings) is set to Both.

(WIP) Navigate to Packages/VaSiLi-Lab/Runtime/Scenes/MetaStart or LocalLoopbackTest and open the scene. 
For now only the MetaXRSimulator or a VR headset is supported.
<!--
### Quick-start
1. Import the samples from the Unity package and open the `Start` scene.
2. Open the `Social Network Scene` prefab and configure the `Connection Defintion` in the RoomClient to point to your own Ubiq-Server installation, or for quick testing you can switch it out to the `Nexus Connection Definition` which is the server provided by the UCL (some features will be missing)
3. Configure the api url to point to your server in the `Scene Manager` script located in the `SceneManager` GameObject.
4. Click play!

### Advanced
1. Create a new scene to act as your starting scene.
2. Import the `Player`, `Scene Selecter` and `Social Network Scene` prefab into your scene.
3. Create another scene and add it to your build settings as well as the database.
4. Click play!
-->


