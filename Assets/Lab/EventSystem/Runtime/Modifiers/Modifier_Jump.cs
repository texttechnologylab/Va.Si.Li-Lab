using UnityEngine;

/*
Modifier_Container

@date 

@author Nick Korjakin
@version 1.0

*/
public class Modifier_Jump : MonoBehaviour, IModifier
{
    public void Activate()
    {
        return;
    }

    public void Deactivate()
    {
        return;
    }

    public void Toggle(GameObject target, float value)
    {
        throw new System.NotImplementedException();
    }

    public string getName()
    {
        return "Modifier_Jump";
    }
    public bool IsActive()
    {
        return false;
    }

    public string getDiscription()
    {
        return "Modifierziert den Jump vom Spieler";
    }
}
