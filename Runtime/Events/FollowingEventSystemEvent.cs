using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// An event that starts another EventSystem
/// </summary>
public class FollowingEventSystemEvent : MonoBehaviour, IEvent
{
	private GameObject followingEventSystemObject = null;

	public void trigger()
	{
		if (followingEventSystemObject != null)
		{
			followingEventSystemObject.SetActive(true);
		}
	}

	public void setFollowingEventSystem(GameObject followingEventSystemObject)
	{
		this.followingEventSystemObject = followingEventSystemObject;
	}

	public void previewOn()
	{
		//not implemented
	}

	public void previewOff()
	{
		//not implemented
	}

	public void delete()
	{
		GameObject[] gameObjects = FindObjectsOfType<GameObject>();
		foreach (GameObject potentialEventSystemFactory in gameObjects)
		{
			if (potentialEventSystemFactory.GetComponent<EventSystemFactory>() != null)
			{
				if (potentialEventSystemFactory.GetComponent<EventSystemFactory>().getEventSystemObject() == followingEventSystemObject)
				{
					potentialEventSystemFactory.GetComponent<EventSystemFactory>().cancelEventSystemCreation();
				}
			}
		}
		Destroy(followingEventSystemObject);
		Destroy(gameObject);
	}

	public string createJson(int groupID)
	{
		GameObject[] gameObjects = FindObjectsOfType<GameObject>();
		foreach (GameObject gameObject in gameObjects)
		{
			EventSystemFactory factory = gameObject.GetComponent<EventSystemFactory>();
			if (factory != null)
			{
				if (factory.getEventSystem() == followingEventSystemObject.GetComponent<EventSystem>())
				{
					return factory.createJson("FollowingEventSystem", 0);
				}
			}
		}
		return null;
	}
}
