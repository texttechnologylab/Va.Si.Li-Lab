using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// An Event that starts to show a kind of Survey once triggered
/// </summary>
public class VotingEvent : MonoBehaviour, IEvent
{   
    private string type;
    private string question;
    private float duration;
    private List<string> options;
    private VotingManager votingManager;

    public void trigger()
    {
        votingManager.TriggerVoting(type, question, duration, options);
    }
    public void setType(string type){
        this.type = type;
    }
    public void setQuestion(string question){
        this.question = question;
    }
    public void setDuration(float duration){
        this.duration = duration;
    }
    public void setVotingManager(VotingManager votingManager){
        this.votingManager=votingManager;
    }
    public void setOptions(List<string> options){
        this.options = options;
    }
    public string createJson(int groupID)
	{
		//JsonFeedbackEvent jsonObject = new JsonFeedbackEvent {group = groupID, type = "FeedbackEvent", text = this.text, duration = this.duration, feedbackType = this.type, position = this.position };
		return JsonUtility.ToJson("h");
	}

    public void delete()
    {
        //throw new System.NotImplementedException();
    }

    public void previewOff()
    {
        //throw new System.NotImplementedException();
    }

    public void previewOn()
    {
        //throw new System.NotImplementedException();
    }

    
}