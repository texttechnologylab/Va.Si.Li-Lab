using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Input;
/**
 * PortalSystem
 *
 * @author Berk Basaran
 *
 * Enables Placement of Portals and handles collision, swipe, orientation of portals as well as permissions
 */
public class PortalSystem : MonoBehaviour, IActiveState
{ 
    /**
     * General controller for VR Input handling hands, location
     */
    public OVRCameraRig cameraRig;
    /**
     * Handles all portals connected to the current portal
     */
    public List<PortalSystem> linkedPortals = new List<PortalSystem>();
    /**
     * gives current index in Linked Portals, i.e what connection is present
     */
    public int currentLinkedIndex = 0;
    /**
     * decides whether main portal is set or not
     */
    public bool allowTeleport = false; // Assume that the portal is initally not set

    [SerializeField, Interface(typeof(IActiveState))]
    /**
     * Selects Permission based on ASG scripts (for initialization)
     */
    private List<UnityEngine.Object> _activeStates;
    /**
     * Selects Permission based on ASG scripts (for in script use)
     */
    private List<IActiveState> _states;
    /**
     * Decides whether Teleportation is possible or not
     */
    private bool canTeleport = true;
    /**
     * Mirror Surface
     */
    private GameObject surfaceObject;

    /**
     * Surface of other portal
     */
    public MeshRenderer surfaceRenderer;
    /**
     * Camera enabling surface and texture setting
     */
    public Camera portalCamera;
    /**
     * Also needed for turning camera image into texture that is blotted onto portal
     */
    public RenderTexture portalTexture;
    /**
     * All of these recognize both hands and swipe gestures for portal selection
     */
    [SerializeField] private HandRef leftHandRef;
    [SerializeField] private HandRef rightHandRef;
    [SerializeField] private GameObject leftSwipeGesturePrefab;
    [SerializeField] private GameObject rightSwipeGesturePrefab;

    /**
     * Initializes all lists and makes sure none are null but just empty
     */
    protected void Awake()
    {
        if (_activeStates == null) _activeStates = new List<UnityEngine.Object>();
            _states = _activeStates.ConvertAll(o => o as IActiveState);

        surfaceObject = transform.Find("Surface")?.gameObject;
        if (surfaceObject != null)
            surfaceRenderer = surfaceObject.GetComponent<MeshRenderer>();

        if (surfaceRenderer != null)
            surfaceRenderer.material = new Material(surfaceRenderer.material);

        if (portalCamera == null)
        {
            portalCamera = transform.Find("Reflection").GetComponent<Camera>();
        }

        if (cameraRig == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                player = GameObject.FindGameObjectWithTag("MetaPlayer");

            if (player != null)
                cameraRig = FindFirstObjectByType<OVRCameraRig>();
        }

        InjectHandRefs();
        InjectSwipeGestures();
    }

    /**
     * Creates the Texture of the portal
     */
    void Start()
    {
        portalTexture = new RenderTexture(1080, 1920, 24);
        portalTexture.Create();
        portalCamera.targetTexture = portalTexture;
    }

    /**
     * Inserts Portal into PortalRegistry for connection
     */
    void OnEnable()
    {
        PortalRegistry.RegisterPortal(this);
        gameObject.name = "Portal " + PortalRegistry.portals.Count;
    }

    /**
     * For external input of active states,i.e. permissions for teleportation
     *
     * @param MyActiveStates the list of ASGs that are used for teleportation permission
     */
    public void SetActiveStates(List<UnityEngine.Object> MyActiveStates)
    {
        _activeStates = MyActiveStates;
    }

    /**
     * Remove portal from PortalRegistry
     */
    void OnDisable()
    {
        PortalRegistry.RemovePortal(this);
    }
    
    /**
     * checks if PortalPartner exists and disables the texture if it isn't 
     */
    private void Update()
    {
        PortalSystem targetPortal = GetCurrentPortal();
        if (targetPortal == null || !targetPortal.gameObject.activeInHierarchy)
        {
            surfaceObject.SetActive(false);
        }
        else
        {
            surfaceObject.SetActive(true);
        }
    }

    /**
     * Ensures that image posted on the portal is up to date
     */
    void LateUpdate()
    {
        PortalSystem targetPortal = GetCurrentPortal();
        if (targetPortal != null) UpdatePortalCamera();
    }

    /**
     * Handles collision of objects and player with portal and teleports them if they should
     *
     * @param other the colliding object
     */
    private void OnTriggerEnter(Collider other)
    {
        if (!allowTeleport) return;
        if (!canTeleport) return;

        PortalSystem targetPortal = GetCurrentPortal();
        if (targetPortal == null || !targetPortal.gameObject.activeInHierarchy) return;

        // Check if player is colliding
        if (cameraRig == null) return;
        if (other.transform != cameraRig.centerEyeAnchor) return;

        /*
         * Idea about multi camera setup:
         * Use the collider's cameraRig object to get the position of the player and teleport them to the linked portal's position.
         * So, no need to teleport the cameraRig itself, just the colliding object.
         * This should also work for other colliding objects, so it is more flexible and less error prone.
         */

        if (other.GetComponent<TeleportationPermission>() != null) TeleportObject(other);

        //conditional checks
        if (_states == null || _states.Count == 0)
        {
            StartCoroutine(Teleport());
        }
        else
        {
            for (int i = 0; i < _states.Count; i++)
                if (!_states[i].Active)
                    return;

            StartCoroutine(Teleport());
        }
    }

    /**
     * Finds the handRefs and injects them into the components
     */
    private void InjectHandRefs()
    {
        if (cameraRig == null) return;
        if (leftHandRef == null || rightHandRef == null) return;

        var hands = FindObjectsByType<Oculus.Interaction.Input.Hand>(
            FindObjectsSortMode.None
        );

        foreach (var hand in hands)
        {
            if (hand.gameObject.name.Contains("Left"))
            {
                leftHandRef.InjectAllHandRef(hand);
            }
            else if (hand.gameObject.name.Contains("Right"))
            {
                rightHandRef.InjectAllHandRef(hand);
            }
        }
    }

    /**
     * Retrieves swipe gestures and append them to ASGs to check if active
     */
    private void InjectSwipeGestures()
    {
        // Get objects where the gestures need to be injected to
        Transform swipeGestureTransform = transform.Find("SwipeGesture");
        if (swipeGestureTransform == null) return;

        Transform leftHandTransform  = swipeGestureTransform.Find("LeftHand");
        Transform rightHandTransform = swipeGestureTransform.Find("RightHand");

        if (leftHandTransform == null || rightHandTransform == null) return;

        // Find the gestures. Assume it is called "HandSwipeGestureLeft" and "HandSwipeGestureRight".
        GameObject leftGestureInstance  = GameObject.Find("HandSwipeGestureLeft");
        GameObject rightGestureInstance = GameObject.Find("HandSwipeGestureRight");

        if (leftGestureInstance == null || rightGestureInstance == null) return;

        IActiveState leftActiveState  = leftGestureInstance.GetComponent<IActiveState>();
        IActiveState rightActiveState = rightGestureInstance.GetComponent<IActiveState>();

        ActiveStateGroup leftGroup  = leftHandTransform.GetComponent<ActiveStateGroup>();
        ActiveStateGroup rightGroup = rightHandTransform.GetComponent<ActiveStateGroup>();

        if (leftGroup == null || rightGroup == null || leftActiveState == null || rightActiveState == null) return;

        AppendToActiveStateGroup(leftGroup, leftActiveState);
        AppendToActiveStateGroup(rightGroup, rightActiveState);
    }

    /**
     * append retireved swipe gesture ASGs to initialise the gesture
     * 
     * @param group the ASG to which the gesture should be added
     * @param newState the gesture to be added
     */
    private void AppendToActiveStateGroup(ActiveStateGroup group, IActiveState newState)
    {
        var activeStatesField = typeof(ActiveStateGroup)
            .GetField("_activeStates",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        // Get current Active State Group
        var existing = activeStatesField?.GetValue(group) as List<UnityEngine.Object>;
        existing = existing != null ? new List<UnityEngine.Object>(existing) : new List<UnityEngine.Object>();

        // Append the Gesture to the ASG
        existing.Add(newState as UnityEngine.Object);

        // Inject the new list
        group.InjectActiveStates(existing.ConvertAll(o => o as IActiveState));
    }

    /**
     * Teleport a colliding object
     *
     * @param o the object to teleport
     */
    private void TeleportObject(Collider o)
    {
        PortalSystem targetPortal = GetCurrentPortal();
        if (targetPortal == null) return;

        Debug.Log("Teleporting Object");
        Vector3 forwardOffset = targetPortal.transform.forward * -1f;
        Vector3 newPosition = targetPortal.transform.position + forwardOffset;
        o.transform.position = newPosition;
    }

    /**
     * Get the currently connected portal and ensure no errors with the index exists
     *
     * @return the current portal
     */
    PortalSystem GetCurrentPortal()
    {
        if (linkedPortals == null || linkedPortals.Count == 0) return null;

        if (currentLinkedIndex < 0)
            currentLinkedIndex = 0;

        if (currentLinkedIndex >= linkedPortals.Count)
            currentLinkedIndex = linkedPortals.Count - 1;

        return linkedPortals[currentLinkedIndex];
    }

    /**
     * swipe to get to the next Portal (swiping does it externally)
     */
    public void NextPortal()
    {
        if (linkedPortals.Count == 0) return;

        currentLinkedIndex = (currentLinkedIndex + 1) % linkedPortals.Count;
        UpdatePortalCamera();
    }

    /**
     * get to the previous portal (decrease the counter)
     */
    public void PreviousPortal()
    {
        if (linkedPortals.Count == 0) return;

        currentLinkedIndex--;
        if (currentLinkedIndex < 0)
            currentLinkedIndex = linkedPortals.Count - 1;

        UpdatePortalCamera();
    }

    /**
     * teleport the player
     * @return cooldown for teleportation to avoid multiple teleports in a row
     */
    private IEnumerator Teleport()
    {
        PortalSystem targetPortal = GetCurrentPortal();
        if (targetPortal == null) yield break;

        canTeleport = false;
        targetPortal.canTeleport = false;

        Debug.Log("Teleporting Player");

        Debug.Log("Old Position: " + cameraRig.transform.position);

        Vector3 headOffset = cameraRig.centerEyeAnchor.position - cameraRig.trackingSpace.position;
        Vector3 forwardOffset = -targetPortal.transform.forward;

        // Ignore vertical offset
        forwardOffset.y = 0;
        headOffset.y = 0;

        // Set position
        Vector3 newPosition = targetPortal.transform.position + forwardOffset - headOffset;
        newPosition.y = cameraRig.trackingSpace.position.y;
        cameraRig.trackingSpace.position = newPosition;
        //cameraRig.trackingSpace.rotation = targetPortal.transform.rotation;

        yield return new WaitForSeconds(0.2f);

        canTeleport = true;
        targetPortal.canTeleport = true;

        Debug.Log("New position: " + cameraRig.trackingSpace.position);
    }

    /**
     * sets texture of updated camera for goal Portal
     */
    public void UpdatePortalCamera()
    {
        PortalSystem targetPortal = GetCurrentPortal();
        if (targetPortal == null || portalCamera == null) return;

        portalCamera.transform.position = transform.position;

        Quaternion deltaRotation = targetPortal.transform.rotation * Quaternion.Inverse(transform.rotation);
        portalCamera.transform.rotation = deltaRotation * Quaternion.Euler(0, 180f, 0);

        if (surfaceRenderer != null && targetPortal.portalTexture != null)
            surfaceRenderer.material.mainTexture = targetPortal.portalTexture;
    }

    /**
     * Has to be added because this implements IActiveState, could be used as a condition for ActiveStateGroups
     */
    public bool Active => canTeleport;
}
