using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/**
 * PortalSelectionMenu
 *
 * @author Berk Basaran
 *
 * This class is used to manage the portal selection menu.
 * It allows the user to select a portal to link to.
 */
public class PortalSelectionMenu : MonoBehaviour
{
    /**
     * Reference to the center eye anchor of the VR rig.
     */
    public Transform centerEyeAnchor;
    /**
     * Distance at which the menu should be spawned in front of the user.
     */
    public float spawnDistance = 2;
    /**
     * Reference to the content transform of the menu, where the buttons will be instantiated.
     */
    public Transform content;
    /**
     * Reference to the button prefab used to create buttons for each portal.
     */
    public GameObject buttonPrefab;
    /**
     * Reference to the menu object.
     */
    public GameObject menu;
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
     * Reference to the currently selected portal for linking.
     */
    private PortalSystem currentPortal;

    /**
     * Shows the menu and sets the position and rotation of the menu to face the user.
     */
    void Update()
    {
        menu.SetActive(showMenu);

        if (showMenu != previousShowMenu) {
            menu.transform.position = centerEyeAnchor.transform.position + new Vector3(centerEyeAnchor.forward.x, 0, centerEyeAnchor.forward.z).normalized * (spawnDistance - 0.5f);
        }

        menu.transform.LookAt(new Vector3 (centerEyeAnchor.transform.position.x, menu.transform.position.y, centerEyeAnchor.transform.position.z));
        menu.transform.forward *= -1;
        previousShowMenu = showMenu;
    }

    /**
     * Opens the portal selection menu for a specific portal.
     *
     * @param portal the portal for which the menu should be opened
     */
    public void OpenMenu(PortalSystem portal)
    {
        Debug.Log("OPENMENU");
        showMenu = true;
        currentPortal = portal;

        PopulateList();
    }

    /**
     * Opens the portal selection menu.
     * This method can be used to open the menu without a specific portal.
     * This should only be used for debugging purposes!
     */
    public void OpenMenu()
    {
        showMenu = true;
        PopulateList();
    }

    /**
     * Populates the menu with buttons for each portal in the scene.
     */
    void PopulateList()
    {
        // Remove old buttons
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        float yOffset = 60f;
        float spacing = 91.3f + 4.0f; // Height of button + additionial spacing

        // Button that allows to add a new portal
        // Idea: This menu can be opened anywhere in the scene.
        // If the user clicks on "Portal hier erstellen", a new portal will be created at the position of the menu.
        // This should not be used when the menu is opened via a portal, as it creates another portal next to the exisiting one.
        GameObject createButton = Instantiate(buttonPrefab, content);
        RectTransform createRect = createButton.GetComponent<RectTransform>();
        createRect.anchoredPosition = new Vector2(0, -yOffset);
        yOffset += spacing;

        TMP_Text createText = createButton.GetComponentInChildren<TMP_Text>();
        createText.text = "Portal hier erstellen";

        Button createBtn = createButton.GetComponent<Button>();
        createBtn.onClick.AddListener(CreatePortalHere);

        // Get all active portals
        Debug.Log("Portal Count:" + PortalRegistry.portals.Count);
        foreach (PortalSystem portal in PortalRegistry.portals)
        {
            // Do not show itself
            if (portal == currentPortal) continue;
            //if (portal.linkedPortal != null) continue;

            GameObject buttonObj = Instantiate(buttonPrefab, content);
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(0, -yOffset);
            yOffset += spacing;

            TMP_Text text = buttonObj.GetComponentInChildren<TMP_Text>();
            bool isSelected = currentPortal.linkedPortals.Contains(portal);
            text.text = portal.gameObject.name;
            text.color = isSelected ? Color.green : Color.black;

            Button button = buttonObj.GetComponent<Button>();
            PortalSystem portalCopy = portal;
            button.onClick.AddListener(() => SelectPortal(portalCopy));
        }
    }

    /**
     * Selects a portal to link to the current portal.
     *
     * @param selectedPortal the portal that was selected
     */
    void SelectPortal(PortalSystem selectedPortal)
    {
        bool alreadyLinked = currentPortal.linkedPortals.Contains(selectedPortal);

        if (alreadyLinked)
        {
            currentPortal.linkedPortals.Remove(selectedPortal);

            if (selectedPortal.linkedPortals.Contains(currentPortal))
                selectedPortal.linkedPortals.Remove(currentPortal);
        }
        else
        {
            currentPortal.linkedPortals.Add(selectedPortal);

            if (!selectedPortal.linkedPortals.Contains(currentPortal))
                selectedPortal.linkedPortals.Add(currentPortal);
        }

        currentPortal.UpdatePortalCamera();
        selectedPortal.UpdatePortalCamera();

        //showMenu = false;
        PopulateList();
    }

    /**
     * Closes the portal selection menu.
     */
    public void CloseMenu()
    {
        showMenu = false;
    }

    /**
     * Creates a new portal at the position of the menu.
     */
    void CreatePortalHere()
    {
        Vector3 spawnPos = centerEyeAnchor.position +
            new Vector3(centerEyeAnchor.forward.x + 1, -0.7f, centerEyeAnchor.forward.z).normalized * spawnDistance;

        Quaternion rotation = Quaternion.LookRotation(
            new Vector3(centerEyeAnchor.forward.x, 0, centerEyeAnchor.forward.z)
        );

        GameObject newPortalObj = Instantiate(portalPrefab, spawnPos, rotation);
        PortalSystem newPortal = newPortalObj.GetComponent<PortalSystem>();

        SelectPortal(newPortal);
    }
}
