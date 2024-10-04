## OVRCameraRigInteraction

If you have the CoreSDK/InteractionSDK Assets installed, delete the Default Camera in your scene and just add this prefab into your scene. This Camera works both with Controllers and Handtracking by default.



## Usage
The (hand) interactions made possible with this setup include:

* Grabs (Special Grabs include i.e. Pinch and Pointer)
* Distance Grabs
* Ray Grabs
* Locomotion
* Teleportation

## How to Add Interactions
To add interactions to your OVRCameraRig, all that is required is to modify the state of the object you want to make interactable.

For each type of grab interaction, follow these steps:

Highlight your GameObject, right-click, search for INTERACTION SDK, and choose the type of grab interaction you want to apply to your GameObject.
If your object doesn’t have a Collider or Rigidbody yet, a window will pop up prompting you to click "Create." This will add a child GameObject named "ISDK_...", allowing the object to be interacted with. (This only works because the OVRCameraRigInteraction already has all of the necessary interactors.)
It is recommended NOT to attach every type of interactor to a single GameObject. If you have a large object but only want a small part of it to be interactable, the same rule applies.

There are multiple types of interactions that you can add to a GameObject:
* Hand Grab
* Distance Grab
* Poke
* Ray Grab
* etc.

[For more check out this link](https://developers.meta.com/horizon/documentation/unity/unity-isdk-interaction-sdk-overview/)

## Locomotion

The OVRCameraRigInteractable prefab already comes with the necessary setup to implement teleporting and turning via hand tracking.
To enable teleportation, you will need to copy HotspotVoidFloor and NavMeshHotspotWalkable from the Meta Locomotion sample scene. This will create a large plane that allows for teleportation. If you want to prevent teleportation through objects, you will also need to copy CollidersTeleportBlocker.

It may be necessary to adjust the Y-axis of HotspotVoidFloor to ensure the floor does not block teleportation.

Alternatively, for more complex environments, you can use the "Add Teleport Quick Action" from the Oculus SDK. You can also bake a NavMesh and add a teleport interactable to it. However, this is not recommended.



### References
[MetaInteractionSDK](https://developer.oculus.com/documentation/unity/unity-isdk-interaction-sdk-overview/)
[HandTracking](https://developer.oculus.com/documentation/unity/unity-handtracking-overview/)
