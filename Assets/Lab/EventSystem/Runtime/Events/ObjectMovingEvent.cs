using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
/// <summary>
/// This event moves an object from one place to another
/// </summary>
public class ObjectMovingEvent : MonoBehaviour, IEvent
{
	private GameObject target;
	private Vector3 position;
	private Quaternion rotation;
	public void trigger()
	{
		target.transform.position = position;
		target.transform.rotation = rotation;
	}

	public void setTarget(GameObject target)
	{
		this.target = target;
	}

	public void setPosition(Vector3 position)
	{
		this.position = position;
	}

	public void setRotation(Quaternion rotation)
	{
		this.rotation = rotation;
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
		JsonObjectMoving jsonObject = new JsonObjectMoving { group = groupID, type = "ObjectMoving", target = this.target.name, position = this.position, rotation = this.rotation };
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
public class JsonObjectMoving
{
	public int group = 0;
	public string type;
	public string target;
	public Vector3 position;
	public Quaternion rotation;
}
