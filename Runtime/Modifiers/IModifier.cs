using UnityEngine;


/*
IModifier

@date 24.01.2026

@author Nick Korjakin
@version 1.0

The interface for the modifiers
*/
public interface IModifier
{
    /*
    Activates the modifier.
    */
    void Activate();

    /*
    Deactivates the modifier.
    */
    void Deactivate();

    /*
    Toggles the modifier on a given target with a specified value.

    @param target The GameObject the modifier affects.
    @param value A value that influences the modifier's effect.
    */
    void Toggle(GameObject target, float value);

    /*
    Returns the name of the modifier.

    @return The name as a string.
    */
    string getName();

    /*
    Returns the description of the modifier.

    @return A short description of the modifier.
    */
    string getDiscription();

    /*
    Indicates whether the modifier is currently active.

    @return True if the modifier is active, otherwise false.
    */
    bool IsActive();
}
