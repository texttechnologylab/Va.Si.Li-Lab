using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Defines methods needed for the EventSystem to do
/// </summary>
public interface IEventSystem
{
    /// <summary>
    /// Triggers all events of an group that the EventSystem contains
    /// </summary>
    /// <param name="groupID"> The ID of groups that should be triggered </param>
    public void triggerEvents(int groupID);
    /// <summary>
    /// Checks if all triggers have triggered
    /// </summary>
    /// <param name="origin"> The origin GameObject that has triggered first </param>
    public void checkTriggers(GameObject origin);
    /// <summary>
    /// Adds a trigger to the EventSystem
    /// </summary>
    /// <param name="item"> The trigger GameObject to add </param>
    /// <param name="groupID"> The groupID of this trigger </param>
    public void addTrigger(GameObject item, int groupID);
    /// <summary>
    /// Returns list of all triggers
    /// </summary>
    /// <returns></returns>
    public List<List<GameObject>> getTriggers();
    /// <summary>
    /// Deletes a trigger
    /// </summary>
    /// <param name="id"> The ID of a trigger </param>
    public void deleteTrigger(short id);
	/// <summary>
	/// Adds an event to the EventSystem
	/// </summary>
	/// <param name="item"> The event GameObject to add </param>
	/// <param name="groupID"> The groupID of this event </param>
	public void addEvent(GameObject item, int groupID);
    /// <summary>
    /// Returns list of all events
    /// </summary>
    /// <returns></returns>
    public List<List<GameObject>> getEvents();
    /// <summary>
    /// Deletes an event
    /// </summary>
    /// <param name="id"> The ID of an event </param>
    public void deleteEvent(short id);
    /// <summary>
    /// Deletes this EventSystem
    /// </summary>
    public void deleteEventSystem();
    /// <summary>
    /// Sets if this EventSystem is repeatable
    /// </summary>
    /// <param name="repeatable"> Boolean value if this EventSystem is repeatable </param>
    public void setRepeatable(bool repeatable);
    /// <summary>
    /// Returns value if this EventSystem is repeatable
    /// </summary>
    /// <returns></returns>
    public bool getRepeatable();
}