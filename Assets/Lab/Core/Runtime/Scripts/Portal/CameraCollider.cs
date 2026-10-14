using UnityEngine;

/**
 * CameraCollider
 *
 * @author Berk Basaran
 *
 * This class is used to add a collider to the player's camera in order to detect collisions with the portal.
 * This is necessary for the teleportation logic to work properly.
 */
public class CameraCollider : MonoBehaviour
{
    /**
     * Reference to the OVRCameraRig component of the player.
     */
    public OVRCameraRig cameraRig;

    /**
     * The radius of the collider to be added to the camera.
     */
    public float colliderRadius = 0.15f;

    /**
     * Get the OVRCameraRig component and add a collider to the center eye anchor if it doesn't already have one.
     */
    void Awake()
    {
        if (cameraRig == null)
            cameraRig = FindFirstObjectByType<OVRCameraRig>();

        if (cameraRig == null) return;

        Transform eye = cameraRig.centerEyeAnchor;

        if (eye.GetComponent<Collider>() != null) return; // Collider already exists

        SphereCollider eyeCollider = eye.gameObject.AddComponent<SphereCollider>();
        eyeCollider.isTrigger = true;
        eyeCollider.radius = colliderRadius;

        Rigidbody rb = eye.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;
    }
}
