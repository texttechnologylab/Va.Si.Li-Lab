using UnityEngine;

/**
 * DebugCall
 *
 * @author Vujevic
 *
 * This class provides methods to log different types of messages to the console.
 * This helps debugging inside the Unity editor.
 */
public class DebugCall : MonoBehaviour
{
    /**
     * Print a message to the console when the script starts.
     */
    void Start()
    {
        Debug.Log("It started");
    }

    /**
     * Default debug method to print a message to the console.
     */
    public void debug(){
        Debug.Log("recognized");
    }

    /**
     * Debug method to print an integer value to the console.
     *
     * @param i the integer value to be printed
     */
    public void debugInt(int i)
    {
        Debug.Log(i);
    }

    /**
     * Debug method to print a string value to the console.
     *
     * @param s the string value to be printed
     */
    public void DebugString(string s)
    {
        Debug.Log(s);
    }
}
