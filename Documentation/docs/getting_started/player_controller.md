# Player Controller

The player is the prefab `MetaPlayer2.0` in `Assets/Lab/Core/Runtime/Prefabs`. It is built from Meta XR
[Building Blocks](https://developers.meta.com/horizon/documentation/unity/bb-overview/) and works with controllers and
hand tracking.

## How it is used
- Only the `MetaStart` scene contains the player. `DontDestroyOnLoad` keeps it alive when a scenario scene is loaded,
  so scenario scenes must **not** contain their own player or camera.
- When the role or scene changes, `PlayerSpawnManager` moves the player to the role's spawn point: a GameObject whose
  name matches the role's `spawnPosition` in the [scene definition](adding_scenes.md). Afterwards
  `MenuRequestManager` opens the social menu.

## Structure

| Object | Contents |
|---|---|
| `MetaPlayer2.0` (root) | `OVRPlayerController` + `CharacterController` (thumbstick movement, 45° snap turn, gravity), `PlayerSpawnManager`, `MenuRequestManager`, `DontDestroyOnLoad`, `DistanceObjectDesktopUser` |
| `[BuildingBlock] Camera Rig` | `OVRCameraRig` and `OVRManager`: head, eye and hand anchors |
| `[BuildingBlock] Interaction` | Interaction SDK rig: hand and controller interactions (grab, distance grab, poke, ray), synthetic hands, the `Locomotor` (teleport arc, turning, step locomotion with hands) and the eye-gaze ray (`EyeGazePointerPose`) |
| `Meta Avatar Input (OVR)` | `MetaAvatarInput`: feeds head and hand tracking into the local Meta avatar |
| `BlurEffect` | Blurred vision for roles with the `blur` restriction |

## Movement scripts
All in `Assets/Lab/Core/Runtime/Scripts/Movement`:

| Script | Purpose |
|---|---|
| `PlayerSpawnManager` | Moves the local player to the role's spawn point after a role or scene change |
| `MovementLock` | Locks movement while the player's status is *waiting* and lifts the `blur` restriction meanwhile |
| `TeleportOnTrigger` | Teleports a player to a target when they enter a trigger collider |
| `ContinousMovementEventBroadcaster` | Turns a thumbstick axis into continuous locomotion events |

## Input
The project uses the Input System only (*Player Settings > Active Input Handling*). For buttons that should work with
controllers, hand tracking and a desktop keyboard alike, use `VaSiLi.Helper.LabInput` (e.g. `LabInput.PrimaryDown`).
For controller- or hand-specific input use `OVRInput` and `OVRHand`, see
[Meta input actions](https://developers.meta.com/horizon/documentation/unity/unity-inputactions).

## Adding interactions to objects
The player already has all interactors, so an object only needs an interactable:

1. Select the GameObject, right-click and choose *Interaction SDK* and the interaction you want (hand grab, distance
   grab, poke, ray grab, ...).
2. If the object has no Collider or Rigidbody yet, click *Create* in the dialog. This adds an `ISDK_...` child object.

Don't put every interaction type on one object. If only part of a large object should be interactable, add the
interactable to that part only. More: [Interaction SDK overview](https://developers.meta.com/horizon/documentation/unity/unity-isdk-interaction-sdk-overview/).

## Teleporting in a scene
The `Locomotor` handles teleporting and turning. To make a floor teleportable, add a teleport interactable to it, for
example with *Add Teleport Quick Action* from the Interaction SDK, or copy `HotspotVoidFloor` and
`NavMeshHotspotWalkable` from Meta's locomotion sample scene (and `CollidersTeleportBlocker` to stop teleports through
walls). You may need to adjust the floor's height so it doesn't block the arc.

## References
- [Meta Interaction SDK](https://developers.meta.com/horizon/documentation/unity/unity-isdk-interaction-sdk-overview/)
- [Hand tracking](https://developers.meta.com/horizon/documentation/unity/unity-handtracking-overview/)
