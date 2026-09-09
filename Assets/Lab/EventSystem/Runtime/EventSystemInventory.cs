using Meta.WitAi;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using UnityEngine.UI;
using NUnit.Framework;
/// <summary>
/// This script handles the EventSystem Inventory
/// </summary>


/*
EventSystemInventory

@date 11.12.2025

@author Emir Hadzikaric
@author Nick Korjakin
@version 1.0

This class is for everything that includes the Inventory.
Within the Inventory, you can store Objects, which functions can be used then.
The functions can be turned off in a seperate canvas.
*/
public class EventSystemInventory : MonoBehaviour
{
    [SerializeField] GameObject objectPanel;
    [SerializeField] private InventarManager inventorySystem;
    [SerializeField] private Modifier_Container _modifierContainer;

    public GameObject currentSlotObject;
    public PrefabToImage currentPrefabSlot;

    [SerializeField] private Transform playerHead;
    [SerializeField] private float distanceFromPlayer = 1.0f;
    [SerializeField] private float verticalOffset = -0.2f;
    [SerializeField] private float horizontalOffset = 0.0f;

    public void openObjectView()
    {
        objectPanel.SetActive(true);
    }

    /**
    * if object is held in hand and the inventory slot is empty, it will be transferred into the inventory
    * if the inventory slot is not empty, the item will be sent out of the inventory
    * if the user has modifier admin, the extra canvas to deactivate the function will be opened
    */
    public void OnButtonClicked(PrefabToImage prefabToImage)
    {
        GameObject storedObject;

        if (prefabToImage == null)
        {
            Debug.LogWarning("PrefabToImage nicht auf dem Button gefunden.");
            return;
        }
        storedObject = prefabToImage.prefab;
        //Button ist leer
        if (storedObject == null)
        {
            GameObject heldObject = inventorySystem.GetHeldObject();

            if (heldObject == null)
            {
                Debug.Log("Kein Objekt in der Hand.");
                return;
            }

            prefabToImage.prefab = heldObject;

            StartCoroutine(StoreObjectRoutine(prefabToImage));
            Debug.Log("Objekt wurde im Button gespeichert.");
        }
        //Button ist belegt
        else
        {
            if (_modifierContainer.GetModifierStatus("Modifier_Admin"))
            {
                currentSlotObject = storedObject;
                currentPrefabSlot = prefabToImage;
                transform.Find("Inventory_Menu/ModifierConfig/ObjectCanvas").gameObject.SetActive(true);
                transform.Find("Inventory_Menu/ModifierConfig/ObjectCanvas/ObjectToggle/Toggle").gameObject.GetComponent<Toggle>()
                    .SetIsOnWithoutNotify(storedObject.GetComponent<IModifier>().IsActive());
                transform.Find("Inventory_Menu/ModifierConfig/ObjectCanvas/BackgroundPanel/TitlePanel/Text").gameObject.GetComponent<Text>().text 
                    = storedObject.GetComponent<IModifier>().getName();
                transform.Find("Inventory_Menu/ModifierConfig/ObjectCanvas/ObjectDescription/Text").gameObject.GetComponent<Text>().text 
                    = storedObject.GetComponent<IModifier>().getDiscription();
            }
            else
            {
                inventorySystem.SpawnObject(storedObject);

                SetObjectVisible(storedObject, true);

                //SetObjectPhysics(storedObject, true);

                Debug.Log("Objekt wurde vor dem Spieler gespawnt.");

                prefabToImage.prefab = null;
                prefabToImage.rawImage.texture = null;
                _modifierContainer.RemoveValue(storedObject);   
            }
        }
    }

    /**
    * button to drop the object out of the inventory
    */
    public void OnButtonDropClicked()
    {
        GameObject storedObject;

        if (currentPrefabSlot == null)
        {
            Debug.LogWarning("PrefabToImage nicht auf dem Button gefunden.");
            return;
        }
        storedObject = currentPrefabSlot.prefab;
        
        inventorySystem.SpawnObject(storedObject);

        SetObjectVisible(storedObject, true);

        //SetObjectPhysics(storedObject, true);

        Debug.Log("Objekt wurde vor dem Spieler gespawnt.");

        currentPrefabSlot.prefab = null;
        currentPrefabSlot.rawImage.texture = null;
        _modifierContainer.RemoveValue(storedObject);   

        transform.Find("Inventory_Menu/ModifierConfig/ObjectCanvas").gameObject.SetActive(false);
        currentPrefabSlot = null;
    }

    /**
    * button to close the admin canvas
    */
    public void OnButtonCancelClicked()
    {
        transform.Find("Inventory_Menu/ModifierConfig/ObjectCanvas").gameObject.SetActive(false);
        currentPrefabSlot = null;
    }


    public void toggleCheckmark()
    {
        if(currentSlotObject != null)
        {
            Debug.Log("Test toggle");
            _modifierContainer.SetModifierState(currentSlotObject.GetComponent<IModifier>().getName(), !currentSlotObject.GetComponent<IModifier>().IsActive());
        }
    }

    /**
    * lets the object get invinsible when placed into the inventory
    */
    private void SetObjectVisible(GameObject obj, bool visible)
    {
        if (obj == null) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }

        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
        foreach (Collider c in colliders)
        {
            c.enabled = visible;
        }
    }
    /**
    * lets the object be static when placed into the inventory
    */
    private void SetObjectPhysics(GameObject obj, bool enabledState)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = !enabledState;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = enabledState;
        }
    }

    public void HideInventory()
    {
        transform.Find("Inventory_Menu/ModifierConfig").gameObject.SetActive(false);
        transform.Find("Inventory_Menu/ModifierConfig/ObjectCanvas").gameObject.SetActive(false);
        transform.Find("Inventory_Menu/InvMenu").gameObject.SetActive(false);
    }

    public void ShowInventory()
    {
        transform.Find("Inventory_Menu/InvMenu").gameObject.SetActive(true);
        transform.Find("Inventory_Menu/ModifierConfig").gameObject.SetActive(true);

        Vector3 forward = playerHead.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = playerHead.right;
        right.y = 0f;
        right.Normalize();

        Vector3 spawnPosition =
            playerHead.position
            + forward * distanceFromPlayer
            + right * horizontalOffset
            + Vector3.up * verticalOffset;

        transform.position = spawnPosition;

        Vector3 lookTarget = playerHead.position;
        lookTarget.y = transform.position.y;

        transform.LookAt(lookTarget);
        transform.Rotate(0f, 180f, 0f);
    }

    /**
    * stores the object in the inventory and calls the method OnButtonClicked
    */
    private IEnumerator StoreObjectRoutine(PrefabToImage prefabToImage)
    {
        yield return StartCoroutine(prefabToImage.RenderQueued());

        SetObjectVisible(prefabToImage.prefab, false);
        //SetObjectPhysics(prefabToImage.prefab, false);
        yield return new WaitForSeconds(1.2f);
        _modifierContainer.SetModifierState(prefabToImage.prefab, true);
    }
}