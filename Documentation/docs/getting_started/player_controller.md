# Player Controller
Instead of the XRInteraction that unity provides, this Camera uses the [META INTERACTION SDK](https://developer.oculus.com/documentation/unity/unity-before-you-begin/) and all the other assets that come with this asset.

## OVRCameraRigInteraction
This is the camera/PlayerController we use to implement all the things required for a VR-based game. This includes handtracking and controller tracking both with and without a avatar attached to camera. We use this camera instead of the normal OVRCameraRig as it also already has the OVRCameraRig and allows for more special interactions. This includes more complex interactions such as hand swipes to alter states of objects, as well as finger gestures that can be mapped onto different actions as required.

As this camera directly visualizes the Handinteraction getting objects to be interactable is not the task of the camera anymore, it already has all of the components to be fully interactable on its own. The only part necessary to make objects interactable with the Player Controller is the "Interaction SDK" that appears under the objects you want to have made Interactable.

## Usage
The (hand) interactions made possible with this setup include:

* Grabs (Special Grabs include i.e. Pinch and Pointer)
* Distance Grabs
* Ray Grabs
* Locomotion
* Teleportation

Usually with just the OVRCameraRigInteraction camera, the hands would appear as see-through if no Avatar is set but these can be made both visible and mapped to a networked character such as in this project.
A player is also free to interchangably switch between using controllers or their hands to interact with objects.

### In this Project
To avoid having Multiple Cameras Active at once, this setup also required us to keep the OVRCameraRigInteraction as "DONTDESTROYONLOAD", so that it can transfer between scenes and keep all the interactions aswell as the MetaAvatarSkeleton. Also this required us to use a Rigidbody and Capsule Collider on the OVRCameraRig, as to avoid being ablo to phase through objects that also have physics, such as doors.

### References
[MetaInteractionSDK] (https://developer.oculus.com/documentation/unity/unity-isdk-interaction-sdk-overview/)
[HandTracking] (https://developer.oculus.com/documentation/unity/unity-handtracking-overview/)
