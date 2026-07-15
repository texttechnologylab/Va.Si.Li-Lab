using UnityEngine;
/// <summary>
/// An Event that starts to show a feedback once triggered
/// </summary>
public class FeedbackEvent : MonoBehaviour, IEvent
{
	private string text;
	private float duration;
	private NotificationManager notificationManager;
	private string type;
	private string position;
	public void trigger()
	{
		notificationManager.TriggerNotification(text, duration, type, position);
	}
	public void setText(string text)
	{
		this.text = text;
	}

	public void setDuration(float seconds)
	{
		this.duration = seconds;
	}

	public void setNotificationManager(NotificationManager notificationManager)
	{
		this.notificationManager = notificationManager;
	}
	public void setType(string type)
	{
		this.type = type;
	}

	public void setPosition(string position)
	{
		this.position = position;
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
		//not implemented
	}

	public string createJson(int groupID)
	{
		JsonFeedbackEvent jsonObject = new JsonFeedbackEvent {group = groupID, type = "FeedbackEvent", text = this.text, duration = this.duration, feedbackType = this.type, position = this.position };
		return JsonUtility.ToJson(jsonObject);
	}
}

/// <summary>
/// Class that defines the JSON structure of this event
/// </summary>
public class JsonFeedbackEvent
{
	public int group = 0;
	public string type;
	public string text;
	public float duration;
	public string feedbackType;
	public string position;
}
