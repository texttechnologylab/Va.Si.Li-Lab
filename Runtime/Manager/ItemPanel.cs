using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// This script handles the UI element that represents an Eventsystem in the EventSystem Manager
/// </summary>
public class ItemPanel : MonoBehaviour
{
	private GameObject eventSystemFactoryObject;
	private string name;

	/// <summary>
	/// Returns the EventSystemFactory GameObject this UI element is referring to
	/// </summary>
	/// <returns></returns>
	public GameObject getEventSystemFactoryObject()
	{
		return eventSystemFactoryObject;
	}

	/// <summary>
	/// Sets the EventSystemFactory GameObject this UI element should be referring to
	/// </summary>
	/// <param name="eventSystemObject"></param>
	public void setEventSystemFactoryObject(GameObject eventSystemObject)
	{
		this.eventSystemFactoryObject = eventSystemObject;
	}

	/// <summary>
	/// Deletes itself and the EventSystemFactory this UI element is referring too
	/// </summary>
	/// <param name="onlyDeleteItemPanel"></param>
	public void deleteEventSystemFactory(bool onlyDeleteItemPanel)
	{
		if (!onlyDeleteItemPanel)
		{
			eventSystemFactoryObject.GetComponent<EventSystemFactory>().cancelEventSystemCreation();
		}
		Destroy(gameObject);
	}

	/// <summary>
	/// Enables the EventSystemFactory in order to modify it after creation
	/// </summary>
	public void modifyEventSystem()
	{
		eventSystemFactoryObject.GetComponent<EventSystemFactory>().modifyEventSystemFactory();
	}

	/// <summary>
	/// Sets the name of this EventSystem
	/// </summary>
	/// <param name="name"> The name of this EventSystem </param>
	public void setName(string name)
	{
		this.name = name;
		transform.Find("NamePanel/Text").GetComponent<Text>().text = name;
	}

}
