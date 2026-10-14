using UnityEngine;
using System.Collections.Generic;
using Oculus.Interaction;
using TMPro;

/**
 * PortalManager
 *
 * @author Berk Basaran
 * @author Vujevic
 *
 * Place the actual portals, enable remote placement, check permissions for placement and set mode of portals
 */
public class PortalManager : MonoBehaviour
{
    /**
     * Reference to the center eye anchor of the VR rig.
     */
    private Transform centerEyeAnchor;
    /**
     * Optional reference to the OVRCameraRig, used to find the center eye anchor if not set.
     */
    public OVRCameraRig cameraRig;
    /**
     * The distance at which to spawn portals.
     */
    public float spawnDistance = 2;
    /**
     * Reference to the menu object.
     */
    public GameObject menu;
    /**
     * Reference to the portal selection menu.
     */
    public GameObject portalSelectionMenuPrefab;
    /**
     * Instance of the portal selection menu.
     */
    private PortalSelectionMenu selectionMenu;

    /**
     * Reference to the portal prefab used to create new portals.
     */
    public GameObject portalPrefab;

    /**
     * Flag indicating whether the menu is currently shown.
     */
    public bool showMenu = false;
    /**
     * Previous state of the showMenu flag to detect changes.
     */
    private bool previousShowMenu = false;

    /**
     * List of teleport targets that can be linked to portals.
     */
    public List<Transform> teleportTargets;
    /**
     * List of IActiveState that need to be active in order to set portals.
     */
    private List<UnityEngine.Object> _activeStatesPortalSetting;
    /**
     * Settings for the portal setting states.
     */
    private List<IActiveState> _statesPortalSetting;
    /**
     * List of IActiveState that need to be active in order to teleport through portals.
     */
    private List<UnityEngine.Object> _activeStatesCanTeleport;
    /**
     * List of states that need to be active in order to teleport through portals.
     */
    private List<IActiveState> _statesCanTeleport;

    /**
     * Counter to keep track of the current teleport target index.
     */
    private int counter = 0;
    /**
     * Flag to determine if the portals should be in 1:1 mode or n:m mode.
     */
    private bool oneToOne = false; // This defines if the portal should be 1:1 or n:m.
    /**
     * References to the two portals in 1:1 mode.
     */
    private GameObject portalA;
    private GameObject portalB;
    /**
     * Flag to determine which portal to place next in 1:1 mode.
     */
    private bool placePortalA = true;
    /**
     * Reference to the text object that displays the current mode.
     */
    public TextMeshProUGUI modeText;

    /**
     * Initializes the lists of active states and updates the mode text.
     */
    void Start()
    {
        if (centerEyeAnchor == null)
        {
            if (cameraRig == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                    player = GameObject.FindGameObjectWithTag("MetaPlayer");
                if (player != null)
                    cameraRig = player.GetComponentInChildren<OVRCameraRig>();
                if (cameraRig == null)
                    cameraRig = FindFirstObjectByType<OVRCameraRig>();
            }

            if (cameraRig != null)
                centerEyeAnchor = cameraRig.centerEyeAnchor;
        }

        if (_activeStatesPortalSetting == null)
            _activeStatesPortalSetting = new List<UnityEngine.Object>();

        _statesPortalSetting = _activeStatesPortalSetting.ConvertAll(o => o as IActiveState);

        if (_activeStatesCanTeleport == null)
            _activeStatesCanTeleport = new List<UnityEngine.Object>();

        _statesCanTeleport = _activeStatesCanTeleport.ConvertAll(o => o as IActiveState);

        UpdateModeText();
    }

    /**
     * Sets the position and rotation of the menu to face the user.
     */
    void Update()
    {
        menu.SetActive(showMenu);

        if (showMenu != previousShowMenu)
            menu.transform.position = centerEyeAnchor.transform.position + new Vector3(centerEyeAnchor.forward.x, 0, centerEyeAnchor.forward.z).normalized * spawnDistance;

        menu.transform.LookAt(new Vector3 (centerEyeAnchor.transform.position.x, menu.transform.position.y, centerEyeAnchor.transform.position.z));
        menu.transform.forward *= -1;
        previousShowMenu = showMenu;
    }

    /**
     * Checks if the player has permission to set a portal based on the active states.
     *
     * @return true if the player is allowed to set a portal, false otherwise
     */
    public bool AllowedToSetPortal()
    {
        for (int i = 0; i < _statesPortalSetting.Count; i++)
        {
            if (!_statesPortalSetting[i].Active)
            {
                return false;
            }
        }
        return true;
    }

    /**
     * Increments the counter to cycle through teleport targets.
     */
    public void IncrementCounter()
    {
       counter = (counter + 1) % teleportTargets.Count; 
    }

    /**
     * Places a portal at the position in front of the player if they have permission to set a portal.
     * In 1:1 mode, it alternates between placing portal A and portal B
     * In n:m mode, it creates a new portal and opens the portal selection menu to link it to existing portals.
     */
    public void PlacePortal()
    {
        if (!AllowedToSetPortal()) return;

        Vector3 spawnPosition = SpawnPosition();

        float portalHeight = portalPrefab.transform.localScale.y;
        spawnPosition.y = centerEyeAnchor.position.y - (portalHeight / 4f);

        Vector3 directionToPlayer = centerEyeAnchor.position - spawnPosition;
        directionToPlayer.y = 0;

        Quaternion spawnRotation = Quaternion.LookRotation(-directionToPlayer, Vector3.up);

        if (oneToOne)
        {
            if (placePortalA)
            {
                if (portalA != null) Destroy(portalA);
                portalA = Instantiate(portalPrefab, spawnPosition, spawnRotation);
            }
            else
            {
                if (portalB != null) Destroy(portalB);
                portalB = Instantiate(portalPrefab, spawnPosition, spawnRotation);
            }

            if (portalA != null && portalB != null)
            {
                PortalSystem portalAScript = portalA.GetComponent<PortalSystem>();
                PortalSystem portalBScript = portalB.GetComponent<PortalSystem>();


                portalAScript.SetActiveStates(_activeStatesCanTeleport);
                portalBScript.SetActiveStates(_activeStatesCanTeleport);

                portalAScript.linkedPortals = new List<PortalSystem> { portalBScript };
                portalBScript.linkedPortals = new List<PortalSystem> { portalAScript };
            }

            placePortalA = !placePortalA;
        } else
        {
            GameObject newPortalObj = Instantiate(portalPrefab, spawnPosition, spawnRotation);

            PortalSystem newPortal = newPortalObj.GetComponent<PortalSystem>();
            newPortal.SetActiveStates(_activeStatesCanTeleport);

            CreatePortalSelectionMenu();
            selectionMenu.OpenMenu(newPortal);
        }

        showMenu = false;
    }

    /**
     * Toggles between 1:1 mode and n:m mode for the portals.
     */
    public void ToggleOneToOne()
    {
        oneToOne = !oneToOne;

        UpdateModeText();

        // Reset portals when switching modes
        if (portalA != null) Destroy(portalA);
        if (portalB != null) Destroy(portalB);

        placePortalA = true;
        portalA = null;
        portalB = null;
    }

    /**
     * Updates the mode text to reflect the current portal mode.
     */
    void UpdateModeText()
    {
        modeText.text = oneToOne ? "1:1 Mode" : "n:m Mode";
    }

    /**
     * Toggles the visibility of the menu.
     */
    public void ToggleMenu()
    {
        showMenu = !showMenu;
    }

    /**
     * Opens the portal selection menu.
     */
    public void ShowMenu()
    {
        showMenu = true;
    }

    /**
     * Closes the portal selection menu.
     */
    public void HideMenu()
    {
        showMenu = false;
    }

    /**
     * Adds a teleport target in front of the player to the list of teleport targets if it is not already in the list.
     */
    public void AddTarget()    
    {
        Ray ray = new Ray(centerEyeAnchor.position, centerEyeAnchor.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10f))
        {
            Transform target = hit.transform;

            if (!teleportTargets.Contains(target))
            {
                teleportTargets.Add(target);
                Debug.Log("Added teleport target: " + target.name);
            }
            else
            {
                Debug.Log("Target already in list: " + target.name);
            }
        }
        else
        {
            Debug.Log("No object detected in front of the player.");
        }
    }

    /**
     * Opens the portal selection menu.
     */
    public void OpenPortalSelectionMenu()
    {
        selectionMenu.OpenMenu();
        HideMenu();
    }

    private Vector3 SpawnPosition()
    {
        return centerEyeAnchor.position + new Vector3(centerEyeAnchor.forward.x, 0, centerEyeAnchor.forward.z).normalized * spawnDistance;
    }

    private void CreatePortalSelectionMenu()
    {
        if (selectionMenu != null) return;

        GameObject menuObj = Instantiate(portalSelectionMenuPrefab, SpawnPosition(), Quaternion.identity);
        selectionMenu = menuObj.GetComponent<PortalSelectionMenu>();

        selectionMenu.centerEyeAnchor = centerEyeAnchor;
    }
}
