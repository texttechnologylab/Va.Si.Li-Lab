# Player Controller
Instead of the XRInteraction that unity provides, this Camera uses the [META INTERACTION SDK](https://developer.oculus.com/documentation/unity/unity-before-you-begin/).

## OVRCameraRigInteraction




## Usage
The (hand) interactions made possible with this setup include:

* Grabs (Special Grabs include i.e. Pinch and Pointer)
* Distance Grabs
* Ray Grabs
* Locomotion
* Teleportation

## How to add Interactions

If you want to add interactions to your OVRCameraRig, all that is required is to alter the state of the Object you want to be interactable.

For each different Grab type, all you need to do is:
Highlight your GameObject, right-click, search for INTERACTION SDK and then Choose which grab type you want to have on your GameObject. If your Object doesnt have a Coliider or Rigidbody yet, there will be a window that pops up, click on "Create". This will add a child-GameObject, called "ISDK_..." this means the Object can now be Interacted with. (This only works because of the OVRCameraRigInteraction having all of the interactors already.)

It is recommended to NOT put every single Interactor type onto a single GameObject.
If you have a Large Object but only want a small part of it to be interactable, the same rule applies here.

There are Multiple type of Interactions that you can add to a GameObject:
* Hand Grabs
* Distance Grabs
* Palm Grabs
* Pinch Grabs
* Poke
* etc.

[For more Check out this link](https://developers.meta.com/horizon/documentation/unity/unity-isdk-interaction-sdk-overview/)
## Locomotion

The OVRCameraRigInteractable prefab already comes with the necessary setup to implement teleporting and turning via hand tracking.
To enable teleportation, you will need to copy HotspotVoidFloor and NavMeshHotspotWalkable from the Meta Locomotion sample scene. This will create a large plane that allows for teleportation. If you want to prevent teleportation through objects, you will also need to copy CollidersTeleportBlocker.

It may be necessary to adjust the Y-axis of HotspotVoidFloor to ensure the floor does not block teleportation.

Alternatively, for more complex environments, you can use the "Add Teleport Quick Action" from the Oculus SDK. You can also bake a NavMesh and add a teleport interactable to it. However, this is not recommended.

### In this Project
To avoid having Multiple Cameras Active at once, this setup also required us to keep the OVRCameraRigInteraction as "DONTDESTROYONLOAD", so that it can transfer between scenes and keep all the interactions aswell as the MetaAvatarSkeleton. Also this required us to use a Rigidbody and Capsule Collider on the OVRCameraRig, as to avoid being ablo to phase through objects that also have physics, such as doors.

### References
[MetaInteractionSDK] (https://developer.oculus.com/documentation/unity/unity-isdk-interaction-sdk-overview/)
[HandTracking] (https://developer.oculus.com/documentation/unity/unity-handtracking-overview/)
