using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the functionality of an EventSystem
/// </summary>
public class EventSystem : MonoBehaviour, IEventSystem
{
    [SerializeField] List<List<GameObject>> triggers = new List<List<GameObject>>();
    [SerializeField] List<List<GameObject>> events = new List<List<GameObject>>();
    private bool repeatable = false;
    private bool decouple = false;
    private bool finished = false;
    private int id = 0;
    private List<GameObject> participants = new List<GameObject>();

	// Start is called before the first frame update
	private void Start()
    {
        findOutID();
	}

    /// <summary>
    /// Calculates its own ID
    /// </summary>
    public void findOutID()
    {
		GameObject[] gameObjects = FindObjectsOfType<GameObject>();
		id = 0;
		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.GetComponent<EventSystem>() != null)
			{
				id++;
			}
		}
	}
	// Update is called once per frame independently of the current fps
	private void FixedUpdate()
    {
        int amountTriggers = 0;
        foreach (List<GameObject> triggerList in triggers)
        {
            foreach (GameObject trigger in triggerList)
            {
                if (trigger != null)
                {
                    amountTriggers++;
                }

            }
        }
        if (finished && amountTriggers == 0)
        {
            triggerEvents(0);
        }
    }

	/// <summary>
	/// Triggers all events of an group that the EventSystem contains
	/// </summary>
	/// <param name="groupID"> The ID of groups that should be triggered </param>
	public void triggerEvents(int groupID)
    {
        int amountTriggers = 0;
        if (groupID == 0)
        {

            int groupCount;
            int currentCount;
            // Switching Group ID for dodging if-statement down below.
            for (int i = 1; i < triggers.Count; i++)
            {
                groupCount = triggers[i].Count;
                currentCount = 0;
                foreach (GameObject trigger in triggers[i])
                {
                    if (trigger.GetComponent<ITrigger>().getState() == true)
                    {
                        currentCount++;
                    }

                    if (currentCount == groupCount)
                    {
                        groupID = i;
                        break;
                    }
                }
                if (groupID != 0)
                {
                    break;
                }

            }
            foreach (List<GameObject> group in triggers)
            {
                foreach (GameObject item in group)
                {
                    amountTriggers += 1;
                }
            }
            if (amountTriggers != triggers[0].Count && groupID == 0)
            {
                return;
            }
        }
        foreach (GameObject i in events[0])
        {
            i.GetComponent<IEvent>().trigger();
        }
        if (groupID != 0)
        {
            foreach (GameObject i in events[groupID])
            {
                i.GetComponent<IEvent>().trigger();
            }
        }

        participants = new List<GameObject>();

        if (repeatable)
        {
            foreach (GameObject i in triggers[0])
            {
                i.GetComponent<ITrigger>().resetTrigger();
            }
            if (groupID != 0)
            {
                foreach (GameObject i in triggers[groupID])
                {
                    i.GetComponent<ITrigger>().resetTrigger();
                }
            }
            if (!decouple)
            {
                gameObject.SetActive(false);
            }
            return;
        }
        else
        {
            deleteEventSystem();
        }
    }
	/// <summary>
	/// Checks if all triggers have triggered
	/// </summary>
	/// <param name="origin"> The origin GameObject that has triggered first </param>
	public void checkTriggers(GameObject origin)
    {
        int groupID = 0;
        for (int i = 0; i < triggers.Count; i++)
        {
            foreach (GameObject trigger in triggers[i])
            {
                if (trigger == origin)
                {
                    groupID = i;
                    break;
                }
            }
        }
        int activatedTriggers = 0;
        int neededTriggers = triggers[groupID].Count;
        if (groupID != 0)
        {
            neededTriggers += triggers[0].Count;
        }

        foreach (GameObject i in triggers[0])
        {
            if (i.GetComponent<ITrigger>().getState())
            {
                activatedTriggers++;
            }
        }
        if (groupID != 0)
        {
            foreach (GameObject i in triggers[groupID])
            {
                if (i.GetComponent<ITrigger>().getState())
                {
                    activatedTriggers++;
                }
            }
        }

        if (activatedTriggers >= neededTriggers && finished)
        {
            triggerEvents(groupID);
        }

    }
	/// <summary>
	/// Adds a trigger to the EventSystem
	/// </summary>
	/// <param name="item"> The trigger GameObject to add </param>
	/// <param name="groupID"> The groupID of this trigger </param>
	public void addTrigger(GameObject item, int groupID)
    {
        triggers[groupID].Add(item);
    }

	/// <summary>
	/// Deletes a trigger
	/// </summary>
	/// <param name="id"> The ID of a trigger </param>
	public void deleteTrigger(short id)
    {
        triggers.RemoveAt(id);
    }

	/// <summary>
	/// Adds an event to the EventSystem
	/// </summary>
	/// <param name="item"> The event GameObject to add </param>
	/// <param name="groupID"> The groupID of this event </param>
	public void addEvent(GameObject item, int groupID)
    {
        events[groupID].Add(item);
    }

	/// <summary>
	/// Deletes an event
	/// </summary>
	/// <param name="id"> The ID of an event </param>
	public void deleteEvent(short id)
    {
        events.RemoveAt(id);
    }

    /// <summary>
    /// Adds participant to this EventSystem
    /// </summary>
    /// <param name="participant"> The player GameObject that participated at this EventSystem </param>
    public void addParticipant(GameObject participant)
    {
        participants.Add(participant);
    }

    /// <summary>
    /// Returns all participants
    /// </summary>
    /// <returns></returns>
    public List<GameObject> getParticipants()
    {
        return participants;
    }

    /// <summary>
    /// Resets the list of participants
    /// </summary>
    public void resetParticipants()
    {
		participants = new List<GameObject>();
	}

	/// <summary>
	/// Deletes this EventSystem
	/// </summary>
	public void deleteEventSystem()
    {
        foreach (List<GameObject> triggerList in triggers)
        {
            foreach (GameObject trigger in triggerList)
            {
                if (trigger.GetComponent<InteractionTrigger>() != null)
                {
                    if (!repeatable)
                    {
                        Destroy(trigger.GetComponent<InteractionTrigger>());
                    }
                    trigger.transform.SetParent(null);

                }

            }
        }
        Destroy(transform.gameObject);
    }
	/// <summary>
	/// Returns list of all triggers
	/// </summary>
	/// <returns></returns>
	public List<List<GameObject>> getTriggers()
    {
        return triggers;
    }

	/// <summary>
	/// Returns list of all events
	/// </summary>
	/// <returns></returns>
	public List<List<GameObject>> getEvents()
    {
        return events;
    }

	/// <summary>
	/// Sets if this EventSystem is repeatable
	/// </summary>
	/// <param name="repeatable"> Boolean value if this EventSystem is repeatable </param>
	public void setRepeatable(bool repeatable)
    {
        this.repeatable = repeatable;
    }

	/// <summary>
	/// Returns value if this EventSystem is repeatable
	/// </summary>
	/// <returns></returns>
	public bool getRepeatable()
    {
        return repeatable;
    }

    /// <summary>
    /// Sets this EventSystem as either finished or unfinished
    /// </summary>
    /// <param name="finished"> Boolean value if this EventSystems completion is finished or not </param>
    public void setFinished(bool finished)
    {
        this.finished = finished;
    }

    // Called when enabled
    private void OnEnable()
    {
        foreach (List<GameObject> triggerList in triggers)
        {
            foreach (GameObject trigger in triggerList)
            {
                if (trigger.GetComponent<InteractionTrigger>() == null)
                {
					trigger.transform.SetParent(transform, false);
				}
                
            }
        }
    }

    /// <summary>
    /// Sets this EventSystems ID
    /// </summary>
    /// <param name="id"></param>
    public void setID(int id)
    {
        this.id = id;
    }

    /// <summary>
    /// Returns the ID of this EventSystem
    /// </summary>
    /// <returns></returns>
    public int getID()
    {
        return id;
    }

	/// <summary>
	/// Sets if this EventSystem should be decoupled from it previous EventSystem
	/// Only necessary when this EventSystem is a following EventSystem
	/// </summary>
	/// <param name="decouple"></param>
	public void setDecouple(bool decouple)
    {
        this.decouple = decouple;
    }
}
