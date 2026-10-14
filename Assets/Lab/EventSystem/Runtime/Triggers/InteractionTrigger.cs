using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using Ubiq.XR;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This trigger actives when an user interacts with a given GameObject
/// </summary>
public class InteractionTrigger : MonoBehaviour, ITrigger
{
	private bool state = false;
	private bool triggerOnce = false;
	private EventSystem eventSystem = null;
	public bool getState()
	{
		return state;
	}

	public void notifySystem()
	{
		eventSystem.checkTriggers(transform.gameObject);
	}

	public void resetTrigger()
	{
		state = false;
	}

	public void setEventSystem(EventSystem es)
	{
		this.eventSystem = es;
	}

	public void UnUse(Hand controller)
	{
		if (!triggerOnce)
		{
			state = false;
		}
	}

	public void Use(Hand controller)
	{
		if (eventSystem == null)
		{
			Destroy(this);
		}
		else if (eventSystem != null && eventSystem.gameObject.activeSelf)
		{
			state = true;
			eventSystem.addParticipant(controller.transform.root.gameObject);
			notifySystem();
		}
	}

	public void setTriggerOnce(bool triggerOnce)
	{
		this.triggerOnce = triggerOnce;
	}

	public void delete()
	{
		Destroy(gameObject.GetComponent<InteractionTrigger>());
	}

	public void previewOn()
	{
		//not implemented
	}

	public void previewOff()
	{
		//not implemented
	}

	public string createJson(int groupID)
	{
		JsonInteractionTrigger jsonObject = new JsonInteractionTrigger { group = groupID, type = "InteractionTrigger", triggerOnce = this.triggerOnce, gameObjectTarget = this.gameObject.name };
		return JsonUtility.ToJson(jsonObject);
	}
	// This method is used when the target is a GameObject that doesn't exist when the scene starts
	public void startSearchForUnspawnedGameObject(EventSystemFactory originFactory ,string name)
	{
		gameObject.SetActive(true);
		StartCoroutine(continueSearchForGameObject(originFactory, name));
	}

	// This method is used when the target is a GameObject that doesn't exist when the scene starts. It functions as a Coroutine
	public IEnumerator continueSearchForGameObject(EventSystemFactory originFactory, string name)
	{
		bool foundGameObject = false;

		do
		{
			yield return new WaitForSeconds(0.5f);
			GameObject[] gameObjects = FindObjectsOfType<GameObject>(true);
			foreach (GameObject obj in gameObjects)
			{
				if (obj.name.Equals(name))
				{
					originFactory.GetComponent<EventSystemFactory>().createInteraction(obj, triggerOnce, obj.name);
					foundGameObject = true;
					originFactory.GetComponent<EventSystemFactory>().updateListSizes();
					originFactory.transform.GetComponent<FactoryMenuHandler>().updateTriggerList();
					Destroy(gameObject);
					break;
				}
					
			}
		}
		while (!foundGameObject);
	}

}
/// <summary>
/// Class that defines the JSON structure of this trigger
/// </summary>
public class JsonInteractionTrigger
{
	public int group = 0;
	public string type;
	public bool triggerOnce;
	public string gameObjectTarget;
}
