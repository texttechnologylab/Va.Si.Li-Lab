using Meta.WitAi;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
/// <summary>
/// This script handles the EventSystem Managing for a scene
/// </summary>
public class EventSystemManager : MonoBehaviour
{
	[SerializeField] GameObject eventSystemFactory;
	[SerializeField] GameObject addPanel;
	[SerializeField] GameObject itemPanel;
	[SerializeField] GameObject selecter;
	[SerializeField] List<GameObject> currentEventSystemFactories;
	[SerializeField] GameObject templates;
	// Start is called before the first frame update
	void Start()
	{
		loadDefaultEventSystems();
	}

	/// <summary>
	/// Switches to the Template Selection Panel
	/// </summary>
	public void switchToTemplateSelection()
	{
		transform.Find("Menu/Canvas/TemplatePanel").gameObject.SetActive(true);
		transform.Find("Menu/Canvas/ScrollView").gameObject.SetActive(false);
		StartCoroutine(getRegularTemplates());
	}

	/// <summary>
	/// Starts a GET Request to the Backend API for getting all saved templates
	/// </summary>
	/// <returns></returns>
	IEnumerator getRegularTemplates()
	{
		string uri = VaSiLi.Networking.VasiliEndpoints.Current.eventTemplates + "/defaultTemplates";
		// TESTING VALUES!!
		string jsonFromRequest = null;

		using (UnityWebRequest request = UnityWebRequest.Get(uri))
		{
			yield return request.SendWebRequest();
			switch (request.result)
			{
				case UnityWebRequest.Result.ConnectionError:
					Debug.LogError("ConnectionError has occured!");
					break;
				case UnityWebRequest.Result.DataProcessingError:
					Debug.LogError("DataProcessingError has occured!");
					break;
				case UnityWebRequest.Result.ProtocolError:
					Debug.LogError("ProtocolError has occured!");
					break;
				case UnityWebRequest.Result.Success:
					Debug.Log("Sucessfully got all regular Event Systems!");
					jsonFromRequest = request.downloadHandler.text;
					break;
			}

		}
		if (jsonFromRequest != null)
		{
			GameObject content = transform.Find("Menu/Canvas/TemplatePanel/Viewport/Content").gameObject;
			for (int i = content.transform.childCount - 1; i >= 0; i--)
			{
				Destroy(content.transform.GetChild(i).gameObject);
			}

			string[] jsonWrapped = wrapForJsonArray(jsonFromRequest);
			foreach (string json in jsonWrapped)
			{
				if (JsonUtility.FromJson<JsonEventSystem>(json).scene == SceneManager.GetActiveScene().name)
				{
					GameObject templateItem = Instantiate(templates);
					templateItem.transform.position = new Vector3(0, 0, 0);
					templateItem.transform.localScale = new Vector3(1, 1, 1);
					templateItem.GetComponent<TemplateItem>().setJson(json);
					templateItem.transform.SetParent(content.transform, false);
				}
			}
		}

	}

	/// <summary>
	/// Switches to the Eventsystem Selection Panel
	/// </summary>
	public void switchToEventSystemSelection()
	{
		transform.Find("Menu/Canvas/TemplatePanel").gameObject.SetActive(false);
		transform.Find("Menu/Canvas/ScrollView").gameObject.SetActive(true);
	}

	/// <summary>
	/// Updates the EventSystem UI in case Eventsystems have been deleted or added
	/// </summary>
	public void updateEventSystems()
	{
		GameObject[] gameObjects = FindObjectsOfType<GameObject>(true);
		GameObject currentItemPanel;
		Transform contentTransform = transform.Find("Menu/Canvas/ScrollView/Viewport/Content");
		for (int i = contentTransform.childCount - 1; i >= 0; i--)
		{
			if (contentTransform.GetChild(i).name == "ItemPanel")
			{
				contentTransform.GetChild(i).GetComponent<ItemPanel>().deleteEventSystemFactory(true);
			}
		}

		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.GetComponent<EventSystemFactory>() != null && gameObject.GetComponent<EventSystemFactory>().getPreviousEventSystemFactory() == null)
			{
				currentEventSystemFactories.Add(gameObject);
				currentItemPanel = Instantiate(itemPanel, transform.Find("Menu/Canvas/ScrollView/Viewport/Content"));
				currentItemPanel.name = "ItemPanel";
				currentItemPanel.GetComponent<ItemPanel>().setEventSystemFactoryObject(gameObject);
				currentItemPanel.GetComponent<ItemPanel>().setName(gameObject.GetComponent<EventSystemFactory>().getName());
			}
		}
		transform.Find("Menu/Canvas/ScrollView/Viewport/Content/AddPanel").SetSiblingIndex(-1);
	}

	/// <summary>
	/// Creates a new Eventsystem Factory
	/// </summary>
	public void createNewEventSystemFactory()
	{
		Debug.Log("test");
		GameObject[] gameObjects = FindObjectsOfType<GameObject>();
		int amountOfFactories = 0;
		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.GetComponent<EventSystemFactory>() != null)
			{
				amountOfFactories++;
				break;
			}
		}
		if (amountOfFactories == 0)
		{
			GameObject newEventSystemFactory = Instantiate(eventSystemFactory);
			newEventSystemFactory.GetComponent<EventSystemFactory>().setEventSystemManager(gameObject);
			newEventSystemFactory.transform.position = selecter.transform.position + new Vector3(0.7f, 0f, 0f);
			//newEventSystemFactory.transform.position = new Vector3(1.031f, 0.189f, 1.373f);
			newEventSystemFactory.name = "EventSystemFactory";
		}
	}

	/// <summary>
	/// Starts loading all Eventsystems at the beginning of the scene that have been set as "default" for this scene
	/// </summary>
	public void loadDefaultEventSystems()
	{
		StartCoroutine(getDefaultEventSystems());
	}

	/// <summary>
	/// Starts a GET Request to the Backend API for getting all saved default templates
	/// </summary>
	/// <returns></returns>
	public IEnumerator getDefaultEventSystems()
	{
		List<string> eventSystemJSONs = new List<string>();
		string uri = VaSiLi.Networking.VasiliEndpoints.Current.eventTemplates + "/defaultTemplates";
		string jsonFromRequest = null;

		using (UnityWebRequest request = UnityWebRequest.Get(uri))
		{
			yield return request.SendWebRequest();
			switch (request.result)
			{
				case UnityWebRequest.Result.ConnectionError:
					Debug.LogError("ConnectionError has occured!");
					break;
				case UnityWebRequest.Result.DataProcessingError:
					Debug.LogError("DataProcessingError has occured!");
					break;
				case UnityWebRequest.Result.ProtocolError:
					Debug.LogError("ProtocolError has occured!");
					break;
				case UnityWebRequest.Result.Success:
					Debug.Log("Sucessfully got all default Event Systems!");
					jsonFromRequest = request.downloadHandler.text;
					break;
			}

		}

		string[] jsonWrapped = wrapForJsonArray(jsonFromRequest);

		if (jsonFromRequest != null)
		{
			foreach (string json in jsonWrapped)
			{
				if(JsonUtility.FromJson<JsonEventSystem>(json).scene == SceneManager.GetActiveScene().name)
				{
					createEventSystemFromJSON(json);
				}
				
			}
		}


	}

	/// <summary>
	/// Starts a POST Request to the Backend API for saving all default templates for Eventsystems
	/// </summary>
	/// <returns></returns>
	public IEnumerator postDefaultEventSystem()
	{
		List<string> jsons = new List<string>();
		GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>(true);
		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.GetComponent<EventSystemFactory>() != null)
			{
				gameObject.SetActive(true);
			}
		}

		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.GetComponent<EventSystemFactory>() != null && gameObject.GetComponent<EventSystemFactory>().getPreviousEventSystemFactory() == null)
			{
				jsons.Add(gameObject.GetComponent<EventSystemFactory>().createJson(gameObject.GetComponent<EventSystemFactory>().getName(), 0));
			}

		}

		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.GetComponent<EventSystemFactory>() != null)
			{
				gameObject.SetActive(false);
			}
		}

		string uri = VaSiLi.Networking.VasiliEndpoints.Current.eventTemplates + "/defaultTemplates";
		using UnityWebRequest request = new UnityWebRequest(uri, "POST");
		request.SetRequestHeader("Content-Type", "application/json");
		JsonArrayWrapper jsonArray = new JsonArrayWrapper{data = jsons.ToArray()};
		request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(jsonArray)));
		request.downloadHandler = new DownloadHandlerBuffer();

		yield return request.SendWebRequest();
		switch (request.result)
		{
			case UnityWebRequest.Result.InProgress:
				Debug.Log("In Progress...");
				break;
			case UnityWebRequest.Result.Success:
				Debug.Log("The template has been uploaded to the database!");
				break;
			case UnityWebRequest.Result.ConnectionError:
				Debug.Log("ConnectionError");
				break;
			case UnityWebRequest.Result.ProtocolError:
				Debug.Log("ProtocolError");
				break;
			case UnityWebRequest.Result.DataProcessingError:
				Debug.Log("DataProcessingError");
				break;
		}
	}

	/// <summary>
	/// Saves all default Eventsystems that should be loaded when the scene starts
	/// </summary>
	public void createDefaultEventSystems()
	{
		StartCoroutine(postDefaultEventSystem());
	}

	/// <summary>
	/// Creates an Eventsystem by reading in a json string
	/// </summary>
	/// <param name="json"></param>
	public void createEventSystemFromJSON(string json)
	{
		GameObject newEventSystemFactory = Instantiate(eventSystemFactory);
		newEventSystemFactory.GetComponent<EventSystemFactory>().setEventSystemManager(gameObject);
		newEventSystemFactory.transform.position = transform.position + new Vector3(0.7f, 0f, 0f);
		newEventSystemFactory.name = "EventSystemFactory";
		StartCoroutine(newEventSystemFactory.GetComponent<EventSystemFactory>().loadFromJson(json));

	}

	/// <summary>
	/// Starts all Eventsystem that are currently handled by the EventSystemManager
	/// </summary>
	public void startEventSystems()
	{
		if (transform.Find("Menu/Canvas/ScrollView/Viewport/Content").childCount > 0)
		{
			GameObject[] gameObjects = FindObjectsOfType<GameObject>(true);
			if (gameObjects != null)
			{
				foreach (GameObject gameObject in gameObjects)
				{
					if (gameObject.GetComponent<EventSystemFactory>() != null)
					{
						gameObject.GetComponent<EventSystemFactory>().finishEventSystem();
					}
				}
			}

		}

		// Deleting all Items 
		Transform contentTransform = transform.Find("Menu/Canvas/ScrollView/Viewport/Content");
		for (int i = contentTransform.childCount - 1; i >= 0; i--)
		{
			if (contentTransform.GetChild(i).name == "ItemPanel")
			{
				contentTransform.GetChild(i).GetComponent<ItemPanel>().deleteEventSystemFactory(true);
			}
		}

	}

	/// <summary>
	/// Helper function for handling JsonArrays by wrapping the json string
	/// </summary>
	/// <param name="json"> The json that should be wrapped </param>
	/// <returns></returns>
	public string[] wrapForJsonArray(string json)
	{
		json = "{ \"data\":" + json + "}";
		JsonArrayWrapper jsonArrayWrapper = JsonUtility.FromJson<JsonArrayWrapper>(json);
		return jsonArrayWrapper.data;
	}
}

/// <summary>
/// Defines the Json representation for the JsonArrayWrapper
/// </summary>
[System.Serializable]
public class JsonArrayWrapper
{
	public string[] data;

}


