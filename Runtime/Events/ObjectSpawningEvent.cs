using System.Collections;
using System.Collections.Generic;
using Ubiq.Spawning;
using UnityEngine;
/// <summary>
/// This event spawns a selected object
/// </summary>
public class ObjectSpawningEvent : MonoBehaviour, IEvent
{
	private GameObject selectedGameObject;
	private GameObject spawningGameObject;
	private Vector3 spawnPosition;
	private Quaternion spawnRotation;
	private Vector3 spawnScale;
	private string objectName;
	private bool usedShapeNet;

	public ObjectSpawningEvent(GameObject gameObject, Vector3 spawnposition)
	{
		this.selectedGameObject = gameObject;
		this.spawnPosition = spawnposition;
	}
	public void trigger()
	{
		// The NetworkSpawnManager will be used once networking should be made availabe with the EventSystems
		//NetworkSpawnManager nSpawnManager = spawningGameObject.GetComponent<NetworkSpawnManager>();
		previewOn();
		//nSpawnManager.SpawnWithRoomScope(spawningGameObject);
		if (transform.parent.GetComponent<EventSystem>().getRepeatable())
		{
			spawningGameObject = Instantiate(this.spawningGameObject, spawnPosition, spawnRotation);
			previewOff();
		}

	}

	public void setGameObject(GameObject gameObject)
	{
		selectedGameObject = gameObject;
		spawningGameObject = Instantiate(this.selectedGameObject, spawnPosition, spawnRotation);
		spawningGameObject.name = objectName;
		spawningGameObject.transform.localScale = spawnScale;
		spawningGameObject.SetActive(false);
	}

	public void setUsedShapeNet(bool usedShapeNet)
	{
		this.usedShapeNet = usedShapeNet;
	}


	public void setSpawnPosition(Vector3 spawnPosition)
	{
		this.spawnPosition = spawnPosition;
	}
	public void setSpawnRotation(Quaternion spawnRotation)
	{
		this.spawnRotation = spawnRotation;
	}
	public void setSpawnScale(Vector3 spawnScale)
	{
		this.spawnScale = spawnScale;
	}

	public void setObjectName(string name)
	{
		this.objectName = name;
	}

	public void previewOn()
	{
		if(spawningGameObject != null)
		{
			spawningGameObject.transform.position = spawnPosition;
			spawningGameObject.SetActive(true);
		}

	}

	public void previewOff()
	{
		if(spawningGameObject != null)
		{
			spawningGameObject.SetActive(false);
		}
		
	}

	public void delete()
	{
		Destroy(gameObject);
	}

	public string createJson(int groupID)
	{
		JsonObjectSpawning jsonObject = new JsonObjectSpawning { group = groupID, type = "ObjectSpawning", usedShapeNet = this.usedShapeNet, objectName = this.objectName, spawnPosition = this.spawnPosition, spawnScale = this.spawnScale, spawnRotation = this.spawnRotation };
		return JsonUtility.ToJson(jsonObject);
	}
	// This method is used when the target is a GameObject that doesn't exist when the scene starts
	public void startSearchForUnspawnedGameObject(string name, string newName)
	{
		gameObject.SetActive(true);
		StartCoroutine(continueSearchForGameObject(name, newName));
	}

	// This method is used when the target is a GameObject that doesn't exist when the scene starts. It functions as a Coroutine.
	public IEnumerator continueSearchForGameObject(string name, string newName)
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
					if (newName != null)
					{
						objectName = newName;
					}
					setGameObject(obj);
					foundGameObject = true; 
					break;
				}
			}
		}	
		while(!foundGameObject);
	}
}
/// <summary>
/// Class that defines the JSON structure of this event
/// </summary>
public class JsonObjectSpawning
{
	public int group = 0;
	public string type;
	public bool usedShapeNet;
	public string objectName;
	public Quaternion spawnRotation;
	public Vector3 spawnScale;
	public Vector3 spawnPosition;
}
