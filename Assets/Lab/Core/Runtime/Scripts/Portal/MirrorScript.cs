using UnityEngine;

/**
 * MirrorScript
 *
 * @author Berk Basaran
 *
 * This class is used to create a mirror effect for the portal.
 * It is used to render the view from the linked portal on the portal surface.
 */
public class MirrorScript : MonoBehaviour
{
    /**
     * Reference to the parent portal teleportation script.
     */
    private PortalSystem parentPortal;
    
    /**
     * Player's camera transform.
     */
    private Transform playerCamera;

    /**
     * Get the parent portal teleportation script.
     */
    void Start()
    {
        if (parentPortal == null) parentPortal = GetComponentInParent<PortalSystem>();

        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig != null)
            playerCamera = cameraRig.centerEyeAnchor;
        else
            playerCamera = Camera.main.transform;
    }

    /**
     * Update the position and rotation of the mirror based on the linked portal.
     */
    void LateUpdate()
    {
        if (parentPortal == null) return;
        if (playerCamera == null) return;
        if (parentPortal.linkedPortals == null || parentPortal.linkedPortals.Count == 0) return;

        PortalSystem targetPortal = parentPortal.linkedPortals[parentPortal.currentLinkedIndex];

        Transform parent = parentPortal.transform;
        Transform target = targetPortal.transform;

        Vector3 relativePosition = parent.InverseTransformPoint(playerCamera.position);
        transform.position = target.TransformPoint(relativePosition);

        Quaternion relativeRotation = Quaternion.Inverse(parent.rotation) * playerCamera.rotation;

        transform.rotation = target.rotation * relativeRotation;
    }
}
