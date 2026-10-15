using UnityEngine;
using System.Collections.Generic;

/**
 * PortalRegistry
 *
 * @author Berk Basaran
 *
 * This class is used to manage the portals in the scene.
 * It keeps track of all the portals and provides methods to register and remove portals.
 */
public class PortalRegistry : MonoBehaviour
{
    /**
     * List of all the portals in the scene
     */
    public static List<PortalSystem> portals = new List<PortalSystem>();

    /**
     * Register a portal to the manager
     *
     * @param p the portal to be registered
     */
    public static void RegisterPortal(PortalSystem p)
    {
        if (!portals.Contains(p))
            portals.Add(p);
    }

    /**
     * Remove a portal from the manager
     *
     * @param p the portal to be removed
     */
    public static void RemovePortal(PortalSystem p)
    {
        portals.Remove(p);
    }
}
