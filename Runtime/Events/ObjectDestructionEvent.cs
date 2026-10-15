using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// An event that deletes a given GameObject
/// </summary>
public class ObjectDestructionEvent : MonoBehaviour, IEvent
{
	private GameObject target = null;
	private bool eraze = false;
	public void trigger()
	{
		if (eraze)
		{
			Destroy(target);
		}
		else
		{
			target.SetActive(false);
		}

	}

	public void setTarget(GameObject target)
	{
		this.target = target;
	}
	public void setTotalDestruct(bool eraze)
	{
		this.eraze = eraze;
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
		Destroy(gameObject);
	}

	public string createJson(int groupID)
	{
		JsonObjectDestruction jsonObject = new JsonObjectDestruction { group = groupID, type = "ObjectDestruction", target = target.name, eraze = this.eraze };
		return JsonUtility.ToJson(jsonObject);
	}

	// This method is used when the target is a GameObject that doesn't exist when the scene starts
	public void startSearchForUnspawnedGameObject(string name)
	{
		gameObject.SetActive(true);
		StartCoroutine(continueSearchForGameObject(name));
	}

	// This method is used when the target is a GameObject that doesn't exist when the scene starts. It functions as a Coroutine.
	public IEnumerator continueSearchForGameObject(string name)
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
					setTarget(obj);
					foundGameObject = true;
					break;
				}
			}
		}
		while (!foundGameObject);
	}
}
/// <summary>
/// Class that defines the JSON structure of this event
/// </summary>
public class JsonObjectDestruction
{
	public int group = 0;
	public string type;
	public string target;
	public bool eraze;
}
