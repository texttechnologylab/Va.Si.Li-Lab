using System.Collections;
using UnityEngine;
using UnityEngine.XR;

/*
Modifier_Map

@date 24.01.2026

@author Nick Korjakin
@version 1.0

Gives the user a speed boost modifier
*/
public class Modifier_Speed : MonoBehaviour, IModifier
{
    [SerializeField] private OVRPlayerController controller;
    [SerializeField] private Transform playerOrigin;
    [SerializeField] private Transform head;

    [SerializeField] private float accelerationMultiplier = 3f;
    [SerializeField] private float dampingMultiplier = 1f;
    [SerializeField] private float backAndSideDampenMultiplier = 1f;

    [SerializeField] private bool boostSimulatorTrackingMovement = true;
    [SerializeField] private XRNode simulatorVelocityNode = XRNode.RightHand;
    [SerializeField] private float simulatorVelocityDeadzone = 0.02f;
    [SerializeField] private float simulatorSpeedMultiplier = 1.5f;
    [SerializeField] private bool ignoreY = true;
    [SerializeField] private bool invertSimX = false;
    [SerializeField] private bool invertSimY = false;

    public bool active;

    private float baseAcceleration;
    private float baseDamping;
    private float baseBackAndSideDampen;
    private bool cached;

    private Vector3 pendingDelta;

    void Awake()
    {
        if (controller == null) controller = GetComponentInChildren<OVRPlayerController>(true);
        if (playerOrigin == null && controller != null) playerOrigin = controller.transform;
        CacheBase();
        ApplyController();
        StartCoroutine(EndOfFrameMover());
    }

    /**
    * Activates the speed modifier
    */
    public void Activate() { active = true; ApplyController(); }
    /**
    * Deactivates the speed modifier
    */
    public void Deactivate() { active = false; ApplyController(); }

    /**
    * If the speed is deactivated, diable
    * If the speed is activated, enable
    */
    public void Toggle(GameObject target, float value)
    {
        if (target != null && controller == null)
            controller = target.GetComponentInChildren<OVRPlayerController>(true);

        if (target != null && playerOrigin == null)
            playerOrigin = target.transform;

        if (value > 0f)
            accelerationMultiplier = value;

        active = !active;
        CacheBase();
        ApplyController();
    }

    /**
    * activates the speed
    */
    public void Function(GameObject target, bool cond, float value)
    {
        if (target != null && controller == null)
            controller = target.GetComponentInChildren<OVRPlayerController>(true);

        if (target != null && playerOrigin == null)
            playerOrigin = target.transform;

        if (value > 0f)
            accelerationMultiplier = value;

        active = cond;
        CacheBase();
        ApplyController();
    }

    /**
    * values will be set
    */
    private void CacheBase()
    {
        if (controller == null) return;
        if (cached) return;

        baseAcceleration = controller.Acceleration;
        baseDamping = controller.Damping;
        baseBackAndSideDampen = controller.BackAndSideDampen;
        cached = true;
    }

    /**
    * Applies the movement to the user
    */
    private void ApplyController()
    {
        if (controller == null) return;
        if (!cached) CacheBase();

        if (active)
        {
            controller.Acceleration = baseAcceleration * Mathf.Max(0.01f, accelerationMultiplier);
            controller.Damping = baseDamping * Mathf.Max(0.01f, dampingMultiplier);
            controller.BackAndSideDampen = baseBackAndSideDampen * Mathf.Max(0.01f, backAndSideDampenMultiplier);
        }
        else
        {
            controller.Acceleration = baseAcceleration;
            controller.Damping = baseDamping;
            controller.BackAndSideDampen = baseBackAndSideDampen;
        }
    }

    /**
    * updates the whole avatar of the user
    */
    private void Update()
    {
        if (!active) return;
        if (!boostSimulatorTrackingMovement) return;
        if (playerOrigin == null || head == null) return;

        if (!TryGetDeviceVelocity(simulatorVelocityNode, out Vector3 vel))
            return;

        if (ignoreY) vel.y = 0f;

        if (vel.magnitude < simulatorVelocityDeadzone)
            return;

        Vector3 fwd = head.forward;
        Vector3 right = head.right;

        if (ignoreY)
        {
            fwd = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(right, Vector3.up).normalized;
        }

        float x = Vector3.Dot(vel, right);
        float y = Vector3.Dot(vel, fwd);

        if (invertSimX) x = -x;
        if (invertSimY) y = -y;

        Vector3 flatVel = right * x + fwd * y;
        float mult = Mathf.Max(1f, simulatorSpeedMultiplier);

        Vector3 extraVel = flatVel * (mult - 1f);
        pendingDelta += extraVel * Time.deltaTime;
    }

    /**
    * Only if one frame of the speed boost ends, the next one will be applied -> delay
    */
    private IEnumerator EndOfFrameMover()
    {
        while (enabled)
        {
            yield return new WaitForEndOfFrame();
            if (pendingDelta != Vector3.zero && playerOrigin != null)
            {
                playerOrigin.position += pendingDelta;
                pendingDelta = Vector3.zero;
            }
        }
    }

    /**
    * @return if the avatar is moving
    */
    private static bool TryGetDeviceVelocity(XRNode node, out Vector3 vel)
    {
        var dev = InputDevices.GetDeviceAtXRNode(node);
        vel = default;
        return dev.isValid && dev.TryGetFeatureValue(CommonUsages.deviceVelocity, out vel);
    }

    /**
    * Brings the values back to normal, if the user has no speed modifier anymore
    */
    void OnDisable()
    {
        if (!cached || controller == null) return;
        controller.Acceleration = baseAcceleration;
        controller.Damping = baseDamping;
        controller.BackAndSideDampen = baseBackAndSideDampen;
    }

    public void Toggle(float zoomFov = -1f) => Toggle(null, zoomFov);

    public string getName()
    {
        return "Modifier_Speed";
    }

    public bool IsActive()
    {
        return active;
    }
    public string getDiscription()
    {
        return "Modifiziert die Geschwindigkeit des Spielers";
    }
}
