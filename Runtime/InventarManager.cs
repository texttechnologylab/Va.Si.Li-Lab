using UnityEngine;
using Oculus.Interaction.Grab;
using Oculus.Interaction;


/*
InventarManager

@date 01.03.2026

@author Nick Korjakin
@author Emir Hadzikaric
@version 1.0

Within this class, objects can be grabbed and used.
It is possible, to grab items with the right and left hand, when a button is used.
While holding an object, objects with functions will trigger their function.
*/
public class InventarManager : MonoBehaviour
{
    [Header("Controller Grab Interactors")]
    [SerializeField] private GrabInteractor rightGrabInteractor;
    [SerializeField] private GrabInteractor leftGrabInteractor;

    [Header("Debug Runtime")]
    [SerializeField] private GrabInteractable rightHeldInteractableDebug;
    [SerializeField] private GrabInteractable leftHeldInteractableDebug;
    [SerializeField] private GameObject currentlyHeldObject;

    [Header("Release Delay")]
    [SerializeField] private float clearHeldObjectDelay = 0.2f;
    private float releaseTimer = -1f;

    /**
    * grab interactable component of the right hand
    */
    public GrabInteractable RightHeldInteractable { get; private set; }
    /**
    * grab interactable component of the left hand
    */
    public GrabInteractable LeftHeldInteractable { get; private set; }

    /**
    * object in right hand
    */
    public GameObject RightHeldObject =>
        RightHeldInteractable != null && RightHeldInteractable.transform.parent != null
            ? RightHeldInteractable.transform.parent.gameObject
            : RightHeldInteractable != null
                ? RightHeldInteractable.gameObject
                : null;
    /**
    * object in left hand
    */
    public GameObject LeftHeldObject =>
        LeftHeldInteractable != null && LeftHeldInteractable.transform.parent != null
            ? LeftHeldInteractable.transform.parent.gameObject
            : LeftHeldInteractable != null
                ? LeftHeldInteractable.gameObject
                : null;

    [SerializeField] private GameObject objectPanel;

    [Header("Player References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float spawnDistance = 1.0f;
    [SerializeField] private Modifier_Container _modifierContainer;
    private bool modifierCleared = true;

    public void OpenObjectView()
    {
        objectPanel.SetActive(true);
    }

    public void CloseObjectView()
    {
        objectPanel.SetActive(false);
    }

    public void ClearHeldObject()
    {
        currentlyHeldObject = null;
        releaseTimer = -1f;
    }

    /**
    * returns held object
    * @return held object
    */
    public GameObject GetHeldObject()
    {
        return currentlyHeldObject;
    }

    /**
    * checks if there is an object in hand
    */
    public bool IsHoldingObject()
    {
        return currentlyHeldObject != null;
    }

    /**
    * spawns object in front of player
    */
    public void SpawnObject(GameObject obj)
    {
        if (obj == null || playerCamera == null) return;

        Vector3 spawnPosition = playerCamera.position + playerCamera.forward * spawnDistance;
        obj.transform.position = spawnPosition;
        obj.transform.rotation = Quaternion.identity;
    }

    /**
    * updates the left and right hand
    * prioritizes the hand where the object is held
    */
    private void Update()
    {
        UpdateRightHand();
        UpdateLeftHand();

        rightHeldInteractableDebug = RightHeldInteractable;
        leftHeldInteractableDebug = LeftHeldInteractable;

        GameObject prioritizedHeldObject = GetPrioritizedHeldObject();

        if (prioritizedHeldObject != null)
        {
            if (currentlyHeldObject != prioritizedHeldObject)
            {
                if (!modifierCleared && currentlyHeldObject != null)
                {
                    _modifierContainer.RemoveValue(currentlyHeldObject);
                }

                currentlyHeldObject = prioritizedHeldObject;
                _modifierContainer.SetModifierState(currentlyHeldObject, true);
                modifierCleared = false;
            }

            releaseTimer = -1f;
        }
        else
        {
            if (currentlyHeldObject != null)
            {
                if (!modifierCleared)
                {
                    _modifierContainer.RemoveValue(currentlyHeldObject);
                    modifierCleared = true;
                }

                if (releaseTimer < 0f)
                {
                    releaseTimer = clearHeldObjectDelay;
                }
                else
                {
                    releaseTimer -= Time.deltaTime;

                    if (releaseTimer <= 0f)
                    {
                        currentlyHeldObject = null;
                        releaseTimer = -1f;
                    }
                }
            }
        }
    }

    /**
    * priotizes the hand with held object
    */
    private GameObject GetPrioritizedHeldObject()
    {
        if (RightHeldObject != null)
            return RightHeldObject;

        if (LeftHeldObject != null)
            return LeftHeldObject;

        return null;
    }

    /**
    * updates right hand
    */
    private void UpdateRightHand()
    {
        if (rightGrabInteractor == null)
        {
            RightHeldInteractable = null;
            return;
        }

        RightHeldInteractable = rightGrabInteractor.HasSelectedInteractable
            ? rightGrabInteractor.SelectedInteractable
            : null;
    }

    /**
    * updates left hand
    */
    private void UpdateLeftHand()
    {
        if (leftGrabInteractor == null)
        {
            LeftHeldInteractable = null;
            return;
        }

        LeftHeldInteractable = leftGrabInteractor.HasSelectedInteractable
            ? leftGrabInteractor.SelectedInteractable
            : null;
    }
}