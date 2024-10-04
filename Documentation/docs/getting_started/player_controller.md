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



### In this Project
To avoid having Multiple Cameras Active at once, this setup also required us to keep the OVRCameraRigInteraction as "DONTDESTROYONLOAD", so that it can transfer between scenes and keep all the interactions aswell as the MetaAvatarSkeleton. Also this required us to use a Rigidbody and Capsule Collider on the OVRCameraRig, as to avoid being ablo to phase through objects that also have physics, such as doors.

### References
[MetaInteractionSDK] (https://developer.oculus.com/documentation/unity/unity-isdk-interaction-sdk-overview/)
[HandTracking] (https://developer.oculus.com/documentation/unity/unity-handtracking-overview/)
