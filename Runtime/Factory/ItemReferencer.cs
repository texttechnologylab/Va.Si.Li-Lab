using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This scripts is for the UI elements that either hold an event or a trigger in a factory
/// </summary>
public class ItemReferencer : MonoBehaviour
{
	// An eventGameObject might be an event or a trigger.
	private GameObject eventGameObject;
	private FactoryMenuHandler menuHandler;

	/// <summary>
	/// Returns the GameObject that either holds the trigger or the event
	/// </summary>
	/// <returns></returns>
	public GameObject getEventGameObject()
	{
		return eventGameObject;
	}

	/// <summary>
	/// Sets the GameObject that either holds the trigger or the event
	/// </summary>
	/// <param name="eventGameObject"></param>
	public void setEventGameObject(GameObject eventGameObject)
	{
		this.eventGameObject = eventGameObject;
	}

	/// <summary>
	/// Sets the MenuHandler Component that this UI element is part of
	/// </summary>
	/// <param name="menuHandler"> The FactoryMenuHandler Component </param>
	public void setMenuHandler(FactoryMenuHandler menuHandler)
	{
		this.menuHandler = menuHandler;
	}

	/// <summary>
	/// Changes the group of this event or trigger
	/// </summary>
	public void changeGroupClick()
	{
		menuHandler.changeGroup(transform.gameObject);
	}

	/// <summary>
	/// Deletes the event or the trigger this UI element is referring to
	/// </summary>
	public void deleteEvent()
	{
		IEvent thisEvent = eventGameObject.GetComponent<IEvent>();
		ITrigger thisTrigger = eventGameObject.GetComponent<ITrigger>();
		menuHandler.gameObject.GetComponent<EventSystemFactory>().deleteFromList(eventGameObject);
		if (thisEvent != null)
		{
			thisEvent.previewOff();
			thisEvent.delete();
		}
		else
		{
			thisTrigger.previewOff();
			thisTrigger.delete();
		}
		Destroy(gameObject);
	}
}
