using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This trigger works like a timer that triggers after a given amount of seconds
/// </summary>
public class TimeTrigger : MonoBehaviour, ITrigger
{
	private bool state = false;
	private int seconds = 0;
	EventSystem eventSystem;

	void Start()
	{
		StartCoroutine(countdown());
	}

	private void FixedUpdate()
	{
		if (state)
		{
			notifySystem();
		}
	}

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
		StartCoroutine(countdown());
	}
	// Starts a courotine for counting down
	IEnumerator countdown()
	{

		yield return new WaitForSeconds(seconds);
		state = true;
	}

	public void setSeconds(int seconds)
	{
		this.seconds = seconds;
	}

	public void setEventSystem(EventSystem eventSystem)
	{
		this.eventSystem = eventSystem;
	}

	public void delete()
	{
		Destroy(gameObject);
	}

	public void previewOn()
	{
		//not implemented
	}

	public void previewOff()
	{
		//not implemented
	}

	private void OnEnable()
	{
		resetTrigger();
	}

	public string createJson(int groupID)
	{
		JsonTimeTrigger jsonObject = new JsonTimeTrigger { group = groupID, type = "TimeTrigger", seconds = this.seconds };
		return JsonUtility.ToJson(jsonObject);
	}

}
/// <summary>
/// Class that defines the JSON structure of this trigger
/// </summary>
public class JsonTimeTrigger
{
	public int group = 0;
	public string type;
	public int seconds;
}
