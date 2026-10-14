using Oculus.Interaction;
using Oculus.Platform.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This trigger actives a target enters a specific area
/// </summary>
public class AreaTrigger : MonoBehaviour, ITrigger
{
	private bool state = false;
	EventSystem eventSystem;
	[SerializeField] GameObject target = null;
	private bool activatedOnlyOnce = false;
	private GameObject previewClone = null;
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
		transform.gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
	}

	public void setEventSystem(EventSystem es)
	{
		this.eventSystem = es;
	}

	public void setTarget(GameObject target)
	{
		this.target = target;
	}

	public void setActivatedOnlyOnce(bool activatedOnlyOnce)
	{
		this.activatedOnlyOnce = activatedOnlyOnce;
	}
	private void OnTriggerEnter(Collider other)
	{
		if (other.transform.gameObject == target)
		{
			state = true;
			transform.gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
			notifySystem();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.transform.gameObject == target && !activatedOnlyOnce)
		{
			transform.gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
			state = false;
		}
	}

	public void delete()
	{
		Destroy(gameObject);
	}

	public void previewOn()
	{
		previewClone = Instantiate(gameObject, null);
		previewClone.SetActive(true);
		previewClone.GetComponent<MeshRenderer>().enabled = true;
		Destroy(previewClone.GetComponent<AreaTrigger>());
	}

	public void previewOff()
	{
		if (previewClone != null)
		{
			Destroy(previewClone);
		}
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

	public string createJson(int groupID)
	{
		Vector3 origin = gameObject.GetComponent<MeshFilter>().mesh.vertices[0];
		Vector3 destination = gameObject.GetComponent<MeshFilter>().mesh.vertices[5];
		Vector3 newOrigin = gameObject.transform.TransformPoint(origin);
		Vector3 newDestination = gameObject.transform.TransformPoint(destination);

		JsonAreaTrigger jsonObject = new JsonAreaTrigger { group = groupID, type = "AreaTrigger", activatedOnce = this.activatedOnlyOnce, gameObjectTarget = target.name, origin = newOrigin, destination = newDestination };
		return JsonUtility.ToJson(jsonObject);
	}

}
/// <summary>
/// Class that defines the JSON structure of this trigger
/// </summary>
public class JsonAreaTrigger
{
	public int group = 0;
	public string type;
	public bool activatedOnce;
	public string gameObjectTarget;
	public Vector3 origin;
	public Vector3 destination;
}
