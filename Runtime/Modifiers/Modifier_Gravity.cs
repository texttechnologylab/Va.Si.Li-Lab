using UnityEngine;

/*
Modifier_Container

@date 16.03.2026

@author Nick Korjakin
@version 1.0

Extern modifier, which changes the gravity in scene
*/
public class Modifier_Gravity : MonoBehaviour, IModifier
{
    public bool active;
    [SerializeField] private string affectedTag = "affected";
    public float gravityMultiplier = 0.3f;

    public void Activate()
    {
        active = true;
    }

    public void Deactivate()
    {
        active = false;
    }

    public void Toggle(GameObject target, float value)
    {
        active = !active;
    }

    /**
    * All Objects with the tag, get the gravity modifier
    */
    void FixedUpdate()
    {
        if(!active) return;

        GameObject[] objects = GameObject.FindGameObjectsWithTag(affectedTag);

        foreach (GameObject obj in objects)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
            }
        }
    }

    public string getName()
    {
        return "Modifier_Gravity";
    }
    public bool IsActive()
    {
        return active;
    }

    public string getDiscription()
    {
        return "Modifierziert die Gravitation von Objekten mit dem Tag affected";
    }
}
