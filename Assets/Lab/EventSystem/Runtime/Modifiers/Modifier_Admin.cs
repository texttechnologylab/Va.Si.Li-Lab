using UnityEngine;

/*
Modifier_Admin

@date 16.03.2026

@author Nick Korjakin
@version 1.0

Give the player permission to control modifiers
*/
public class Modifier_Admin : MonoBehaviour, IModifier
{
    public bool active;

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

    public string getName()
    {
        return "Modifier_Admin";
    }
    public bool IsActive()
    {
        return active;
    }

    public string getDiscription()
    {
        return "Gibt einem Rechte modifier zu steuern";
    }
}
