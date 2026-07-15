using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This event teleports players to a specified position
/// </summary>
public class PlayerTeleportationEvent : MonoBehaviour, IEvent
{
	private string playerCriteria = null;
	private Vector3 location = Vector3.zero;
	private Quaternion rotation = Quaternion.identity;

	public void trigger()
	{
		List<GameObject> selectedPlayers = new List<GameObject>();
		GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
		if (playerCriteria.Equals("all"))
		{

			foreach (GameObject player in allPlayers)
			{
				selectedPlayers.Add(player);
			}
		}
		else if (playerCriteria.Equals("triggering users"))
		{
			foreach (GameObject player in this.gameObject.transform.parent.GetComponent<EventSystem>().getParticipants())
			{
				selectedPlayers.Add(player);
			}
		}

		foreach (GameObject player in selectedPlayers)
		{
			player.transform.position = location;
			player.transform.rotation = rotation;
		}
	}

	public void SetPlayerCriteria(string playerCriteria)
	{
		this.playerCriteria = playerCriteria;
	}

	public void setLocation(Vector3 location)
	{
		this.location = location;
	}

	public void setRotation(Quaternion rotation)
	{
		this.rotation = new Quaternion(0, rotation.y, 0, rotation.w);
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
		JsonPlayerTeleportation jsonObject = new JsonPlayerTeleportation { group = groupID, type = "PlayerTeleportation", playerCriteria = this.playerCriteria, location = this.location, rotation = this.rotation };
		return JsonUtility.ToJson(jsonObject);
	}
}
/// <summary>
/// Class that defines the JSON structure of this event
/// </summary>
public class JsonPlayerTeleportation
{
	public int group = 0;
	public string type;
	public string playerCriteria;
	public Vector3 location;
	public Quaternion rotation;
}
