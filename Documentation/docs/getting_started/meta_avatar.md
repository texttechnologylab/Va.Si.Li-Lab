# Meta Avatar

Instead of the Ubiq avatars, we are using the [Meta Avatar](https://developers.meta.com/horizon/documentation/unity/meta-avatars-overview/) system.
The avatars have the following advantages:

* Diverse cast of pre-built avatars + customization options
* Visualisation of Hand- and Finger-Tracking
* Visualisation of Eye-Tracking
* Visualisation of Facial Expressions

## Usage
The Meta Avatars SDK (`com.meta.xr.sdk.avatars`, 40.0.1) is installed from the Unity registry as a dependency of
Va.Si.Li-Lab. Its *Sample Scenes* sample must be imported, because Va.Si.Li-Lab uses `SampleAvatarEntity` and the
`AvatarSdkManagerStyle2Meta` prefab from it (in MetaStart, renamed to `AvatarSdkManagerMeta`, the name the avatar scripts look up), see [Setting Up](setting_up.md#import-the-required-samples).

The avatar scripts of Va.Si.Li-Lab are in `Assets/Lab/Core/Runtime/Scripts/MetaAvatars`. The local avatar is driven by
`MetaAvatarInput` on the [player](player_controller.md).

## References
* [Meta Avatar Documentation](https://developers.meta.com/horizon/documentation/unity/meta-avatars-overview/)
* [Meta Avatars SDK on the Unity registry](https://docs.unity3d.com/Manual/upm-ui.html) (*Package Manager > Unity Registry > Meta Avatars SDK*)
