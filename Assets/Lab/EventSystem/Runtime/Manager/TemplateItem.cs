using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
/// <summary>
/// This script handles the functions of an EventSystem template UI element
/// </summary>
public class TemplateItem : MonoBehaviour
{
	string json;

	// Start is called before the first frame update
	private void Start()
	{
		JsonEventSystem jsonEventSystem = JsonUtility.FromJson<JsonEventSystem>(json);
		try		
		{
			transform.Find("TemplateName/Text").GetComponent<Text>().text = jsonEventSystem.name;
		}
		catch { }
		
	}

	/// <summary>
	/// Creates an EventSystem by loading the json representation that is saved in this UI element
	/// </summary>
	public void createEventSystemFromJSON()
	{
		transform.root.GetComponent<EventSystemManager>().createEventSystemFromJSON(json);
		transform.root.GetComponent<EventSystemManager>().switchToEventSystemSelection();
	}

	/// <summary>
	/// Sets the json string of this UI element
	/// </summary>
	/// <param name="json"> The Json string that has all information about the associated EventSystem </param>
	public void setJson(string json)
	{
		this.json = json;
	}

	/// <summary>
	/// Deletes the template
	/// </summary>
	public void deletetemplate()
	{
		StartCoroutine(deleteTemplate());
	}

	/// <summary>
	/// Creates a DELETE request for the backend API for deleting this template
	/// </summary>
	/// <returns></returns>
	IEnumerator deleteTemplate(){
		string json = "{\"name\": \"" + transform.Find("TemplateName/Text").GetComponent<Text>().text + "\"}";
		string uri = "http://localhost:4567/deleteTemplate";
		using UnityWebRequest request = new UnityWebRequest(uri, "DELETE");
		request.SetRequestHeader("Content-Type", "application/json");
		request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
		request.downloadHandler = new DownloadHandlerBuffer();

		yield return request.SendWebRequest();
		Debug.Log(request.responseCode);
		switch (request.result)
		{
			case UnityWebRequest.Result.InProgress:
				Debug.Log("In Progress...");
				break;
			case UnityWebRequest.Result.Success:
				Debug.Log("The template has been deleted from the database!");
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

		Destroy(gameObject);
	}
}
