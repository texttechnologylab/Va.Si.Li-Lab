using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This script handles the creation of EventSystem GameObject with each triggers and events
/// </summary>
public class EventSystemFactory : MonoBehaviour
{
	private GameObject eventSystemObject;
	private GameObject eventSystemManager;
	public bool repeatable = false;
	public bool decouple = false;
	private EventSystem eS;
	private bool jsonReadComplete = false;
	private string name = "Default Name";

	[SerializeField] GameObject eventSystemFactoryTemplate;
	[SerializeField] List<List<GameObject>> triggers = new List<List<GameObject>>();
	[SerializeField] List<List<GameObject>> events = new List<List<GameObject>>();
	private GameObject previousEventSystemFactory = null;

	// Start is called before the first frame update
	void Start()
	{
		eventSystemObject = new GameObject("EventSystem");
		eS = eventSystemObject.AddComponent<EventSystem>();
		triggers.Add(new List<GameObject>());
		events.Add(new List<GameObject>());
		StartCoroutine(updateID(0.5f));
		if (previousEventSystemFactory == null)
		{
			decouple = true;
		}
		else
		{
			decouple = false;
		}
		eS.setDecouple(decouple);
	}

	/// <summary>
	/// Creates a timer trigger.
	/// </summary>
	/// <param name="seconds"> The amount of secodns to pass before this trigger triggers</param>
	public void createTimer(int seconds)
	{
		GameObject timer = new GameObject("Timer");
		timer.SetActive(false);
		timer.AddComponent<TimeTrigger>();
		timer.GetComponent<TimeTrigger>().setSeconds(seconds);
		timer.GetComponent<TimeTrigger>().setEventSystem(eS);
		timer.transform.SetParent(eventSystemObject.transform, false);
		triggers[0].Add(timer);
		updateListSizes();
	}

	/// <summary>
	/// Creates an area trigger. This descripes trigger that requires a selected object to enter a created area.
	/// </summary>
	/// <param name="collider"> The area that has to be entered by the target </param>
	/// <param name="target"> The target GameObject that has to enter the area </param>
	/// <param name="activatedOnce"> If true this trigger does not have to be activated at the the excact moment every other trigger is considered "activated" </param>
	/// <param name="nameOfTarget"> The name of the GameObject for the case that the target is not instantiated while creating this EventSystem </param>
	public void createAreaTrigger(GameObject collider, GameObject target, bool activatedOnce, string nameOfTarget)
	{
		collider.name = "Area Trigger";
		GameObject colliderClone = collider;
		colliderClone.SetActive(false);
		colliderClone.GetComponent<MeshRenderer>().enabled = false;
		colliderClone.GetComponent<AreaTrigger>().setEventSystem(eS);
		if (target != null)
		{
			colliderClone.GetComponent<AreaTrigger>().setTarget(target);
		}
		else
		{

			colliderClone.GetComponent<AreaTrigger>().startSearchForUnspawnedGameObject(nameOfTarget);
		}

		colliderClone.AddComponent<MeshCollider>();
		colliderClone.GetComponent<MeshCollider>().convex = true;
		colliderClone.GetComponent<MeshCollider>().isTrigger = true;
		colliderClone.transform.SetParent(eventSystemObject.transform, true);
		colliderClone.GetComponent<AreaTrigger>().setActivatedOnlyOnce(activatedOnce);
		triggers[0].Add(collider);
		updateListSizes();
	}

	/// <summary>
	/// Creates an Object Spawning Event. This descripes an event where a selected object gets instantiated at a specified position, with a specific rotation and scale.
	/// </summary>
	/// <param name="spawnPosition"> Position of the object to spawn at </param>
	/// <param name="spawnRotation"> Rotation of the spawning object </param>
	/// <param name="spawnScale"> Scale of the spawning objec t</param>
	/// <param name="gameObject"> Selected GameObject that should be spawned </param>
	/// <param name="objectName"> Name of the selected GameObject in case the GameObject was not instantiated while creating this event </param>
	/// <param name="usedShapeNet"> Toggle if ShapeNet has been used for the GameObject selection </param>
	/// <param name="newName"> Name of the GameObject that should be used after instantiating </param>
	public void createObjectSpawn(Vector3 spawnPosition, Quaternion spawnRotation, Vector3 spawnScale, GameObject gameObject, string objectName, bool usedShapeNet, string newName)
	{
		GameObject objectSpawner = new GameObject("ObjectSpawner");
		objectSpawner.SetActive(false);
		objectSpawner.AddComponent<ObjectSpawningEvent>();
		objectSpawner.GetComponent<ObjectSpawningEvent>().setSpawnPosition(spawnPosition);
		objectSpawner.GetComponent<ObjectSpawningEvent>().setSpawnRotation(spawnRotation);
		objectSpawner.GetComponent<ObjectSpawningEvent>().setSpawnScale(spawnScale);
		objectSpawner.GetComponent<ObjectSpawningEvent>().setUsedShapeNet(usedShapeNet);

		if (gameObject == null)
		{
			objectSpawner.GetComponent<ObjectSpawningEvent>().startSearchForUnspawnedGameObject(objectName, newName);
		}
		else
		{
			if (newName != null)
			{
				objectName = newName;
			}
			objectSpawner.GetComponent<ObjectSpawningEvent>().setObjectName(objectName);
			objectSpawner.GetComponent<ObjectSpawningEvent>().setGameObject(gameObject);
		}
		objectSpawner.transform.SetParent(eventSystemObject.transform, false);
		events[0].Add(objectSpawner);
		updateListSizes();
	}

	/// <summary>
	/// Creates an Interaction Trigger. This is a trigger that triggers when using a selected object.
	/// </summary>
	/// <param name="target"> The GameObject that holds the trigger </param>
	/// <param name="activatedOnce"> If true this trigger does not have to be activated at the the excact moment every other trigger is considered "activated" </param>
	/// <param name="nameTarget"> Name of the selected GameObject in case the GameObject was not instantiated while creating this event </param>
	public void createInteraction(GameObject target, bool activatedOnce, string nameTarget)
	{
		if (target == null)
		{
			GameObject blankTarget = new GameObject("InteractionTriggerPlaceHolder");
			blankTarget.AddComponent<InteractionTrigger>();
			blankTarget.GetComponent<InteractionTrigger>().setTriggerOnce(activatedOnce);
			blankTarget.GetComponent<InteractionTrigger>().setEventSystem(eS);
			blankTarget.GetComponent<InteractionTrigger>().startSearchForUnspawnedGameObject(this, nameTarget);
		}
		else
		{
			target.AddComponent<InteractionTrigger>();
			target.GetComponent<InteractionTrigger>().setTriggerOnce(activatedOnce);
			target.GetComponent<InteractionTrigger>().setEventSystem(eS);
			triggers[0].Add(target);
		}
		updateListSizes();
	}

	/// <summary>
	/// Handles the creation of a following EventSystem Event
	/// </summary>
	public void createFollowingEventSystem()
	{
		int amountOfAlreadyExistingFollowingEventSystems = 0;
		GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.GetComponent<EventSystemFactory>() != null && gameObject.GetComponent<EventSystemFactory>().getPreviousEventSystemFactory() == this.gameObject)
			{
				amountOfAlreadyExistingFollowingEventSystems++;
			}
		}

		GameObject nextEventSystemFactory = Instantiate(eventSystemFactoryTemplate, this.transform.position + new Vector3(0.72f, 0.7f, 0f) - new Vector3(0f, amountOfAlreadyExistingFollowingEventSystems * 0.7f, 0f), transform.rotation);
		nextEventSystemFactory.GetComponent<EventSystemFactory>().setPreviousEventSystemFactory(gameObject);
		nextEventSystemFactory.name = "EventSystemFactory";
		GameObject followingEventSystemEvent = new GameObject();
		followingEventSystemEvent.SetActive(false);
		followingEventSystemEvent.AddComponent<FollowingEventSystemEvent>();
		// Using Coroutine here for avoiding race condition
		StartCoroutine(addNextEventSystemObjectToEvent(0.3f, followingEventSystemEvent, nextEventSystemFactory));
		followingEventSystemEvent.transform.SetParent(eventSystemObject.transform, true);
		events[0].Add(followingEventSystemEvent);
		updateListSizes();
	}

	/// <summary>
	/// Handles the creation of a following EventSystem Event by rading in a json-String
	/// </summary>
	/// <param name="json">The json-String that contains all needed information</param>
	public void createFollowingEventSystemFromJson(string json)
	{
		int amountOfAlreadyExistingFollowingEventSystems = 0;
		GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.GetComponent<EventSystemFactory>() != null && gameObject.GetComponent<EventSystemFactory>().getPreviousEventSystemFactory() == this.gameObject)
			{
				amountOfAlreadyExistingFollowingEventSystems++;
			}
		}

		GameObject nextEventSystemFactory = Instantiate(eventSystemFactoryTemplate, this.transform.position + new Vector3(0.72f, 0.7f, 0f) - new Vector3(0f, amountOfAlreadyExistingFollowingEventSystems * 0.7f, 0f), transform.rotation);
		nextEventSystemFactory.GetComponent<EventSystemFactory>().setPreviousEventSystemFactory(gameObject);
		nextEventSystemFactory.name = "EventSystemFactory";
		GameObject followingEventSystemEvent = new GameObject();
		followingEventSystemEvent.SetActive(false);
		followingEventSystemEvent.AddComponent<FollowingEventSystemEvent>();
		// Using Coroutine here for avoiding race condition
		StartCoroutine(addNextEventSystemObjectToEvent(0.3f, followingEventSystemEvent, nextEventSystemFactory));
		followingEventSystemEvent.transform.SetParent(eventSystemObject.transform, true);
		StartCoroutine(nextEventSystemFactory.GetComponent<EventSystemFactory>().loadFromJson(json));
		events[0].Add(followingEventSystemEvent);
		updateListSizes();
	}

	/// <summary>
	/// Handles the creation of a Feedback Event
	/// </summary>
	/// <param name="text"> The content of the feedback </param>
	/// <param name="duration"> The duration in seconds after which the feedback should disappear</param>
	/// <param name="type"> The type of the feedback (warning, information, error) </param>
	/// <param name="position"> The position at which the feedback should appear </param>
	public void createFeedback(string text, float duration, string type, string position)
	{
		GameObject feedbackEvent = new GameObject("Feedback");
		FeedbackEvent feedbackComponent = feedbackEvent.AddComponent<FeedbackEvent>();
		feedbackComponent.setDuration(duration);
		feedbackComponent.setText(text);
		feedbackComponent.setType(type);
		feedbackComponent.setPosition(position);
		GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
		NotificationManager notificationManager = null;
		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.GetComponent<NotificationManager>() != null)
			{
				notificationManager = gameObject.GetComponent<NotificationManager>();
			}
		}
		feedbackComponent.setNotificationManager(notificationManager);
		feedbackEvent.transform.SetParent(eventSystemObject.transform, false);
		events[0].Add(feedbackEvent);
		updateListSizes();

	}

	/// <summary>
	/// Handles the creation of an event which changes the environment settings
	/// </summary>
	/// <param name="brightness"> The brightness of the sky </param>
	/// <param name="rainIntensity"> The intensity of the rain</param>
	/// <param name="snowIntensity"> The intensity of snow</param>
	/// <param name="cloudIntensity"> The density of clouds</param>
	/// <param name="fogDensity"> The density of fog</param>
	/// <param name="wert"> A bolean walue that defines if thunderstorm is enables</param>

	public void createVoting(string type, string  question, float duration, List<string> options)
	{
		GameObject votingEvent = new GameObject("Vote");
		VotingEvent votingComponent = votingEvent.AddComponent<VotingEvent>();

		votingComponent.setType(type);
		votingComponent.setQuestion(question);
		votingComponent.setDuration(duration);
		votingComponent.setOptions(options);

		GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
		VotingManager voteManager = null;
		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.GetComponent<VotingManager>() != null)
			{
				voteManager = gameObject.GetComponent<VotingManager>();
			}
		}
		votingComponent.setVotingManager(voteManager);
		votingEvent.transform.SetParent(eventSystemObject.transform, false);
		events[0].Add(votingEvent);
		updateListSizes();

	}

	public void createEnvironment(float brightness, float rainIntensity, float snowIntensity, float cloudIntensity, float fogDensity, Boolean wert, float delay)
	{
		GameObject environmentEvent = new GameObject("Environment");

		//#### Weather Event
		WeatherEvent weatherComponent = environmentEvent.AddComponent<WeatherEvent>();
		weatherComponent.setBrightness(brightness);
		weatherComponent.setRainIntensity(rainIntensity);
		weatherComponent.setSnowIntensity(snowIntensity);
		weatherComponent.setCloudIntensity(cloudIntensity);
		weatherComponent.setFogDensity(fogDensity);
		weatherComponent.SetThunderstromBooleanValue(wert, delay);

		GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
		WeatherController weatherController = null;
		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.GetComponent<WeatherController>() != null)
			{

				weatherController = gameObject.GetComponent<WeatherController>();
			}
		}
		weatherComponent.setWeatherController(weatherController);



		environmentEvent.transform.SetParent(eventSystemObject.transform, false);
		events[0].Add(environmentEvent);
		updateListSizes();


	}

	/// <summary>
	/// Creates an event that teleports players to a specified position
	/// </summary>
	/// <param name="playerCriteria"> A string that defines which players should be teleported </param>
	/// <param name="position"> The position the players should be teleported to </param>
	/// <param name="rotation"> The rotation that the players should be rotated towards after being teleported </param>
	public void createPlayerTeleportation(string playerCriteria, Vector3 position, Quaternion rotation)
	{
		GameObject playerTeleportation = new GameObject("Teleportation");
		PlayerTeleportationEvent playerTeleportationEvent = playerTeleportation.AddComponent<PlayerTeleportationEvent>();
		playerTeleportationEvent.setLocation(position);
		playerTeleportationEvent.setRotation(rotation);
		playerTeleportationEvent.SetPlayerCriteria(playerCriteria);
		playerTeleportation.transform.SetParent(eventSystemObject.transform, false);
		events[0].Add(playerTeleportation);
		updateListSizes();
	}

	/// <summary>
	/// Creates an event that destroys a selected GameObject
	/// </summary>
	/// <param name="target"> The GameObject that should be destroyed </param>
	/// <param name="eraze"> When true the object will be destroyed and not just disabled in the scene </param>
	/// <param name="targetName"> Name of the selected GameObject in case the GameObject was not instantiated while creating this event  </param>
	public void createObjectDestruction(GameObject target, bool eraze, string targetName)
	{
		GameObject objectDestruction = new GameObject("Destroy Object");
		ObjectDestructionEvent objectDestructionEvent = objectDestruction.AddComponent<ObjectDestructionEvent>();
		if (target != null)
		{
			objectDestructionEvent.setTarget(target);
		}
		else
		{
			objectDestructionEvent.startSearchForUnspawnedGameObject(targetName);
		}

		objectDestructionEvent.setTotalDestruct(eraze);
		objectDestruction.transform.SetParent(eventSystemObject.transform, false);
		events[0].Add(objectDestruction);
		updateListSizes();
	}

	/// <summary>
	/// Creates an event that moves a selected GameObject to another position
	/// </summary>
	/// <param name="target"> The GameObject that should be moved </param>
	/// <param name="position"> The position the GameObject should be moved to </param>
	/// <param name="rotation"> The rotation to which the GameObject should be rotated </param>
	/// <param name="targetName" > Name of the selected GameObject in case the GameObject was not instantiated while creating this event </param>
	public void createObjectMoving(GameObject target, Vector3 position, Quaternion rotation, string targetName)
	{
		GameObject objectMoving = new GameObject("Moving Object");
		ObjectMovingEvent objectMovingEvent = objectMoving.AddComponent<ObjectMovingEvent>();
		if (target != null)
		{
			objectMovingEvent.setTarget(target);
		}
		else
		{
			objectMovingEvent.startSearchForUnspawnedGameObject(targetName);
		}
		objectMovingEvent.setTarget(target);
		objectMovingEvent.setPosition(position);
		objectMovingEvent.setRotation(rotation);
		objectMovingEvent.transform.SetParent(eventSystemObject.transform, false);
		events[0].Add(objectMoving);
		updateListSizes();
	}

	/// <summary>
	/// Deletes an event or a trigger from the current EventSystem
	/// </summary>
	/// <param name="trigger_event"> The GameObject that has the trigger or event </param>
	public void deleteFromList(GameObject trigger_event)
	{

		foreach (List<GameObject> triggerList in triggers)
		{
			foreach (GameObject trigger in triggerList)
			{
				if (trigger_event == trigger)
				{
					triggerList.Remove(trigger_event);
					return;
				}
			}
		}

		foreach (List<GameObject> eventList in events)
		{
			foreach (GameObject eEvent in eventList)
			{
				if (trigger_event == eEvent)
				{
					eventList.Remove(trigger_event);
					return;
				}
			}
		}
	}

	/// <summary>
	/// Starts the creation of the EventSystem. This method specifically calls createEventSystem(0)
	/// </summary>
	public void createEventSystem()
	{
		createEventSystem(0);
	}


	/// <summary>
	/// Creates an EventSystem for this factory and all its following EventSytems
	/// </summary>
	/// <param name="currentEventSystemCount"> A counter that counts hhow many following EventSystems have been created already </param>
	public void createEventSystem(int currentEventSystemCount)
	{
		int eventSystemCount = 0;
		GameObject[] gameObjects = FindObjectsOfType<GameObject>();
		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.transform.root == gameObject.transform)
			{
				EventSystemFactory eSFactory = gameObject.GetComponent<EventSystemFactory>();
				if (eSFactory != null)
				{
					if (eSFactory.getPreviousEventSystemFactory() == this.gameObject)
					{
						eventSystemCount++;
						gameObject.GetComponent<EventSystemFactory>().createEventSystem(eventSystemCount);
					}
				}
			}
		}
		if (previousEventSystemFactory == null)
		{
			eventSystemManager.GetComponent<EventSystemManager>().updateEventSystems();
		}
		StartCoroutine(FinalizeEventSystemAfterDelay(0.2f + (eventSystemCount * 0.2f)));
	}

	/// <summary>
	/// A Coroutine that finalizes an EventSystem after a given Delay
	/// </summary>
	/// <param name="seconds"> The delay in seconds </param>
	/// <returns></returns>
	IEnumerator FinalizeEventSystemAfterDelay(float seconds)
	{
		yield return new WaitForSeconds(seconds);
		gameObject.SetActive(false);
	}

	/// <summary>
	/// Completes modification of an EventSystem and all its following EventSystems
	/// </summary>
	public void modifyEventSystemFactory()
	{
		gameObject.SetActive(true);
		transform.Find("Menu/Canvas/Main/ControlPanel/CancelButton").gameObject.SetActive(false);
		GameObject[] gameObjects = FindObjectsOfType<GameObject>(true);
		foreach (GameObject gameObject in gameObjects)
		{

			if (gameObject.GetComponent<EventSystemFactory>() != null && gameObject.GetComponent<EventSystemFactory>().getPreviousEventSystemFactory() == this.gameObject)
			{
				gameObject.GetComponent<EventSystemFactory>().modifyEventSystemFactory();
			}
		}
	}

	/// <summary>
	/// Finishes an EventSystem and adds it to the EventSystemManager
	/// </summary>
	public void finishEventSystem()
	{
		int amountTriggers = 0;
		foreach (List<GameObject> groups in triggers)
		{
			eS.getEvents().Add(new List<GameObject>());
			eS.getTriggers().Add(new List<GameObject>());
		}

		for (int i = 0; i < triggers.Count; i++)
		{
			for (int j = 0; j < triggers[i].Count; j++)
			{
				triggers[i][j].SetActive(true);
				triggers[i][j].GetComponent<ITrigger>().previewOff();

				eS.addTrigger(triggers[i][j], i);
				amountTriggers++;
			}
		}
		Destroy(transform.gameObject);

		for (int i = 0; i < events.Count; i++)
		{
			for (int j = 0; j < events[i].Count; j++)
			{
				events[i][j].SetActive(true);
				events[i][j].GetComponent<IEvent>().previewOff();

				eS.addEvent(events[i][j], i);
			}
		}

		if (previousEventSystemFactory != null)
		{
			eventSystemObject.SetActive(false);
		}

		else
		{
			//Force all Triggers and Events to be set as child of EventSystem when this EventSystem is naturally active from the beginning.
			eventSystemObject.SetActive(false);
			eventSystemObject.SetActive(true);
		}
		eS.setFinished(true);

	}

	/// <summary>
	/// Renames an EventSystem without saving a template to the database
	/// </summary>
	public void renameTemplate()
	{
		name = transform.Find("Menu/Canvas/TemplateCreator/Template Settings/InputField/Text").gameObject.GetComponent<Text>().text;
	}

	/// <summary>
	/// Starts a Coroutine where an EventSystem Template is posted to the backend database.
	/// </summary>
	public void finishTemplate()
	{
		StartCoroutine(postTemplate());
	}

	/// <summary>
	/// Posts a template to the database via a REST-Route
	/// </summary>
	/// <returns></returns>
	public IEnumerator postTemplate()
	{
		string name = transform.Find("Menu/Canvas/TemplateCreator/Template Settings/InputField/Text").gameObject.GetComponent<Text>().text;
		string json = createJson(name, 0);
		string uri = "http://localhost:4567/updateRegularTemplates";
		using UnityWebRequest request = new UnityWebRequest(uri, "POST");
		request.SetRequestHeader("Content-Type", "application/json");
		request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
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
	/// Returns all triggers
	/// </summary>
	/// <returns></returns>
	public List<List<GameObject>> getTriggers()
	{
		return triggers;
	}

	/// <summary>
	/// Returns all events
	/// </summary>
	/// <returns></returns>
	public List<List<GameObject>> getEvents()
	{
		return events;
	}

	/// <summary>
	/// Returns the check if the json reading for this eventSystem is completed already
	/// </summary>
	/// <returns></returns>
	public bool getJsonReadComplete()
	{
		return jsonReadComplete;
	}

	/// <summary>
	/// Returns the name of this EventSystem
	/// </summary>
	/// <returns></returns>
	public string getName()
	{
		return name;
	}

	/// <summary>
	/// Toggles if this EventSystem can be repeated
	/// </summary>
	public void toggleRepeatable()
	{
		repeatable = !repeatable;
		eS.setRepeatable(repeatable);
		if (previousEventSystemFactory != null)
		{
			transform.Find("Menu/Canvas/Main/DecoupleToggle").gameObject.GetComponent<Toggle>().interactable = !gameObject.transform.Find("Menu/Canvas/Main/DecoupleToggle").gameObject.GetComponent<Toggle>().interactable;
		}
		if (decouple)
		{
			gameObject.transform.Find("Menu/Canvas/Main/DecoupleToggle").gameObject.GetComponent<Toggle>().isOn = false; ;
		}
	}

	/// <summary>
	/// Toggles if this EventSystem can repeat independently of its previous EventSystem
	/// </summary>
	public void toggleDecouple()
	{
		decouple = !decouple;
		eS.setDecouple(decouple);
	}

	/// <summary>
	/// Checks how many Groups are needed for triggers and events and updates the max. amount of groups
	/// </summary>
	public void updateListSizes()
	{
		int amountTriggers = 0;
		int amountEvents = 0;
		foreach (List<GameObject> group in triggers)
		{
			foreach (GameObject item in group)
			{
				amountTriggers++;
			}
		}
		foreach (List<GameObject> group in events)
		{
			foreach (GameObject item in group)
			{
				amountEvents++;
			}
		}
		int newAmountGroups = 0;
		if (amountTriggers >= amountEvents)
		{
			newAmountGroups = amountEvents - events.Count + 1;
			//currentAmountGroups = amountEvents;
		}
		else if (amountTriggers < amountEvents)
		{
			newAmountGroups = amountTriggers - triggers.Count + 1;
			//currentAmountGroups = amountTriggers;
		}

		int neededAdditionalGroups = newAmountGroups; //- currentAmountGroups;
		for (int i = 0; i < neededAdditionalGroups; i++)
		{
			triggers.Add(new List<GameObject>());
			events.Add(new List<GameObject>());
		}
	}

	/// <summary>
	/// Returns the previous EventSystemFactory Gameobject
	/// </summary>
	/// <returns></returns>
	public GameObject getPreviousEventSystemFactory()
	{
		return previousEventSystemFactory;
	}

	/// <summary>
	/// Returns the EventSystem Factory that is the root of all previous EventSystems
	/// </summary>
	/// <returns></returns>
	public GameObject getRootEventSystemFactory()
	{
		if (getPreviousEventSystemFactory() == null)
		{
			return gameObject;
		}
		GameObject rootEventSystem = previousEventSystemFactory.GetComponent<EventSystemFactory>().getRootEventSystemFactory();
		return rootEventSystem;
	}

	/// <summary>
	/// Sets the previous EventSystemFactory in case this is a following EventSystem
	/// </summary>
	/// <param name="previousEventSystemFactory"> The EventSystemFactory gameobject of the previous EventSystemFactory</param>
	public void setPreviousEventSystemFactory(GameObject previousEventSystemFactory)
	{
		this.previousEventSystemFactory = previousEventSystemFactory;
		if (previousEventSystemFactory != null)
		{
			transform.Find("Menu/Canvas/Main/TemplateCreatorButton").gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// Returns the attached EventSystem as a GameObject
	/// </summary>
	/// <returns></returns>
	public GameObject getEventSystemObject()
	{
		return this.eventSystemObject;
	}

	/// <summary>
	/// Returns the attached EventSystem
	/// </summary>
	/// <returns></returns>
	public EventSystem getEventSystem()
	{
		return this.eS;
	}

	/// <summary>
	/// Sets the EventSystemManager
	/// </summary>
	/// <param name="eventSystemManager"> The EventSystemManager of this scene that handles all EventSystems </param>
	public void setEventSystemManager(GameObject eventSystemManager)
	{
		this.eventSystemManager = eventSystemManager;
	}

	/// <summary>
	/// Cancel the creation of this EventSystem
	/// </summary>
	public void cancelEventSystemCreation()
	{
		GameObject[] gameObjects = FindObjectsOfType<GameObject>(true);
		foreach (GameObject potentialObject in gameObjects)
		{
			if (potentialObject.GetComponent<EventSystemFactory>() != null)
			{
				if (potentialObject.GetComponent<EventSystemFactory>().getPreviousEventSystemFactory() == this.gameObject)
				{
					potentialObject.GetComponent<EventSystemFactory>().cancelEventSystemCreation();
				}

			}
		}
		Destroy(eventSystemObject);
		Destroy(gameObject);
	}

	/// <summary>
	/// Updates the ID of this EventSystemFactory for matching it with its EventSystem's ID with a slight delay
	/// </summary>
	/// <param name="seconds"> The delay in seconds after which the id should be updated</param>
	/// <returns></returns>
	IEnumerator updateID(float seconds)
	{
		yield return new WaitForSeconds(seconds);
		int id = eS.getID();
		transform.Find("Menu/Canvas/EventSystemNamePanel/LabelID").gameObject.GetComponent<Text>().text = "Event System: " + id.ToString();
	}

	/// <summary>
	/// Adds an EventsystemObject to a Following EventSystem as a Coroutine
	/// </summary>
	/// <param name="seconds"> The delay in seconds after which the EventSystemObject should be added to the event </param>
	/// <param name="eEvent"> The following EventSystem Gameobject that gets a following EventSystem </param>
	/// <param name="nextFactory"> The factory the following EventSystem is handled by </param>
	/// <returns></returns>
	IEnumerator addNextEventSystemObjectToEvent(float seconds, GameObject eEvent, GameObject nextFactory)
	{
		while (nextFactory.GetComponent<EventSystemFactory>().getEventSystemObject() == null)
		{
			yield return new WaitForSeconds(seconds);
			eEvent.GetComponent<FollowingEventSystemEvent>().setFollowingEventSystem(nextFactory.GetComponent<EventSystemFactory>().getEventSystemObject());
			eEvent.name = "ES_" + nextFactory.GetComponent<EventSystemFactory>().getEventSystem().getID().ToString();
			gameObject.GetComponent<FactoryMenuHandler>().updateEventList();
		}
	}

	/// <summary>
	/// Creates a JsonRepresentation of this EventSystem with all its events and triggers
	/// </summary>
	/// <param name="name"> The name of the EventSystem </param>
	/// <param name="groupID"> The ID in case this is a following EventSystem </param>
	/// <returns></returns>
	public string createJson(string name, int groupID)
	{
		List<string> triggersJSON = new List<string>();
		List<string> eventsJSON = new List<string>();

		for (int i = 0; i < triggers.Count; i++)
		{
			foreach (GameObject trigger in triggers[i])
			{
				triggersJSON.Add(trigger.GetComponent<ITrigger>().createJson(i));
			}
		}

		for (int i = 0; i < events.Count; i++)
		{
			foreach (GameObject eEvent in events[i])
			{
				eventsJSON.Add(eEvent.GetComponent<IEvent>().createJson(i));
			}
		}

		JsonEventSystem jsonObject = new JsonEventSystem {group = groupID, name = name, scene = SceneManager.GetActiveScene().name, type = "EventSystem", repeatable = this.repeatable, decouple = this.decouple, triggers = triggersJSON, events = eventsJSON };
		return JsonUtility.ToJson(jsonObject);
	}

	/// <summary>
	/// Loads an EventSystem from a Json String
	/// </summary>
	/// <param name="json"> The Json string that holds all information about the EventSystem </param>
	/// <returns></returns>
	public IEnumerator loadFromJson(string json)
	{
		yield return new WaitForSeconds(0.1f);
		bool followingEventSystemIncoming = false;
		JsonEventSystem jsonObject = JsonUtility.FromJson<JsonEventSystem>(json);
		List<int> triggerGroups = new List<int>();
		List<int> eventGroups = new List<int>();

		name = jsonObject.name;
		if (jsonObject.repeatable)
		{
			transform.Find("Menu/Canvas/Main/RepeatableToggle").GetComponent<Toggle>().isOn = true;
		}
		else
		{
			transform.Find("Menu/Canvas/Main/RepeatableToggle").GetComponent<Toggle>().isOn = false;
		}
		if (jsonObject.decouple && previousEventSystemFactory != null)
		{
			transform.Find("Menu/Canvas/Main/DecoupleToggle").GetComponent<Toggle>().isOn = true;
		}
		else
		{
			transform.Find("Menu/Canvas/Main/DecoupleToggle").GetComponent<Toggle>().isOn = false;
		}

		// trigger handling
		foreach (string triggerJSON in jsonObject.triggers)
		{
			triggerGroups.Add(extractGroupFromJSON(triggerJSON));
			// TimeTrigger handling
			try
			{
				JsonTimeTrigger trigger = JsonUtility.FromJson<JsonTimeTrigger>(triggerJSON);
				if (trigger.type.Equals("TimeTrigger"))
				{
					createTimer(trigger.seconds);
				}
				else
				{
					throw new Exception();
				}

			}
			catch
			{
				// AreaTrigger handling
				try
				{
					JsonAreaTrigger trigger = JsonUtility.FromJson<JsonAreaTrigger>(triggerJSON);
					if (trigger.type.Equals("AreaTrigger"))
					{
						GameObject collider = GameObject.CreatePrimitive(PrimitiveType.Cube);
						Vector3 origin = trigger.origin;
						Vector3 destination = trigger.destination;
						Vector3[] verticesClone = collider.GetComponent<MeshFilter>().mesh.vertices;
						// Origin
						verticesClone[0] = origin;
						verticesClone[13] = origin;
						verticesClone[23] = origin;

						// Destination
						verticesClone[5] = destination;
						verticesClone[11] = destination;
						verticesClone[18] = destination;

						// Procedual Mesh Corner 1
						Vector3 meshCorner1 = new Vector3(origin.x, destination.y, origin.z);
						verticesClone[2] = meshCorner1;
						verticesClone[8] = meshCorner1;
						verticesClone[22] = meshCorner1;

						// Procedual Mesh Corner 2
						Vector3 meshCorner2 = new Vector3(destination.x, origin.y, destination.z);
						verticesClone[7] = meshCorner2;
						verticesClone[15] = meshCorner2;
						verticesClone[19] = meshCorner2;

						// Procedual Mesh Corner 3
						Vector3 meshCorner3 = new Vector3(destination.x, origin.y, origin.z);
						verticesClone[1] = meshCorner3;
						verticesClone[14] = meshCorner3;
						verticesClone[16] = meshCorner3;

						// Procedual Mesh Corner 4
						Vector3 meshCorner4 = new Vector3(origin.x, origin.y, destination.z);
						verticesClone[6] = meshCorner4;
						verticesClone[12] = meshCorner4;
						verticesClone[20] = meshCorner4;

						// Procedual Mesh Corner 5
						Vector3 meshCorner5 = new Vector3(origin.x, destination.y, destination.z);
						verticesClone[4] = meshCorner5;
						verticesClone[10] = meshCorner5;
						verticesClone[21] = meshCorner5;

						// Procedual Mesh Corner 6
						Vector3 meshCorner6 = new Vector3(destination.x, destination.y, origin.z);
						verticesClone[3] = meshCorner6;
						verticesClone[9] = meshCorner6;
						verticesClone[17] = meshCorner6;
						collider.GetComponent<MeshFilter>().mesh.vertices = verticesClone;
						Destroy(collider.GetComponent<Collider>());
						collider.AddComponent<MeshCollider>();
						collider.AddComponent<AreaTrigger>();

						GameObject target = null;
						GameObject[] gameObjects = FindObjectsOfType<GameObject>();
						foreach (GameObject obj in gameObjects)
						{
							if (obj.name == trigger.gameObjectTarget)
							{
								target = obj;
							}
						}
						createAreaTrigger(collider, target, trigger.activatedOnce, trigger.gameObjectTarget);
					}
					else
					{
						throw new Exception();
					}

				}
				catch
				{
					// InteractionTrigger handling
					try
					{
						JsonInteractionTrigger trigger = JsonUtility.FromJson<JsonInteractionTrigger>(triggerJSON);
						if (trigger.type.Equals("InteractionTrigger"))
						{
							GameObject target = null;
							GameObject[] gameObjects = FindObjectsOfType<GameObject>();
							foreach (GameObject obj in gameObjects)
							{
								if (obj.name.Equals(trigger.gameObjectTarget))
								{
									target = obj;
								}
							}
							createInteraction(target, trigger.triggerOnce, trigger.gameObjectTarget);
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning(e);
					}
				}
			}
		}
		// eventhandling
		foreach (string eventJSON in jsonObject.events)
		{
			eventGroups.Add(extractGroupFromJSON(eventJSON));
			// ObjectDestruction handling
			try
			{
				JsonObjectDestruction eEvent = JsonUtility.FromJson<JsonObjectDestruction>(eventJSON);
				if (eEvent.type.Equals("ObjectDestruction"))
				{
					GameObject target = null;
					GameObject[] gameObjects = FindObjectsOfType<GameObject>();
					foreach (GameObject obj in gameObjects)
					{
						if (obj.name == eEvent.target)
						{
							target = obj;
						}
					}
					createObjectDestruction(target, eEvent.eraze, eEvent.target);
				}
				else
				{
					throw new Exception();
				}
			}
			catch
			{
				try
				{
					JsonObjectMoving eEvent = JsonUtility.FromJson<JsonObjectMoving>(eventJSON);
					if (eEvent.type.Equals("ObjectMoving"))
					{
						GameObject target = null;
						GameObject[] gameObjects = FindObjectsOfType<GameObject>();
						foreach (GameObject obj in gameObjects)
						{
							if (obj.name == eEvent.target)
							{
								target = obj;
							}
						}
						createObjectMoving(target, eEvent.position, eEvent.rotation, eEvent.target);
					}
					else
					{
						throw new Exception();
					}

				}
				catch
				{
					try
					{
						JsonObjectSpawning eEvent = JsonUtility.FromJson<JsonObjectSpawning>(eventJSON);
						if (eEvent.type.Equals("ObjectSpawning"))
						{
							if (eEvent.usedShapeNet == true)
							{
								//TODO IMPLEMENT SHAPENET FUNCTION
								continue;
							}
							else
							{
								string objectName = eEvent.objectName.Remove(eEvent.objectName.Length - 5, 5);
								GameObject target = null;
								GameObject[] gameObjects = FindObjectsOfType<GameObject>();
								foreach (GameObject obj in gameObjects)
								{
									if (obj.name == objectName)
									{
										target = obj;
										break;
									}
								}
								createObjectSpawn(eEvent.spawnPosition, eEvent.spawnRotation, eEvent.spawnScale, target, objectName, eEvent.usedShapeNet, eEvent.objectName);
							}
						}
						else
						{
							throw new Exception();
						}
					}
					catch
					{
						try
						{
							JsonPlayerTeleportation eEvent = JsonUtility.FromJson<JsonPlayerTeleportation>(eventJSON);
							if (eEvent.type.Equals("PlayerTeleportation"))
							{								
								createPlayerTeleportation(eEvent.playerCriteria, eEvent.location, eEvent.rotation);
							}
							else
							{
								throw new Exception();
							}
						}
						catch (Exception)
						{
							try
							{
								JsonFeedbackEvent eEvent = JsonUtility.FromJson<JsonFeedbackEvent>(eventJSON);
								if (eEvent.type.Equals("FeedbackEvent"))
								{
									createFeedback(eEvent.text, eEvent.duration, eEvent.feedbackType, eEvent.position);
								}
								else
								{
									throw new Exception();
								}
							}

							catch
							{
								try
								{
									JsonWeatherEvent eEvent = JsonUtility.FromJson<JsonWeatherEvent>(eventJSON);
									if (eEvent.type.Equals("WeatherEvent"))
									{
										//createEnvironment(eEvent.brightness, eEvent.rainIntensity, eEvent.snowIntensity, eEvent.cloudIntensity, eEvent.fogDensity, eEvent.thunderstromIsOn
										//);
									}
									else
									{
										throw new Exception();
									}
								}
								catch
								{
									try
									{
										JsonEventSystem eEvent = JsonUtility.FromJson<JsonEventSystem>(eventJSON);
										if (eEvent.type.Equals("EventSystem"))
										{
											followingEventSystemIncoming = true;
										}
										else
										{
											throw new Exception();
										}
									}
									catch
									{
										Debug.Log("Couldn't find any JSON that fits here!");
									}
								}

							}
						}

					}
				}

			}
			if (followingEventSystemIncoming)
			{
				yield return new WaitForSeconds(0.1f);
				Debug.Log("Creating new EventSystem because of:" + eventJSON);
				createFollowingEventSystemFromJson(eventJSON);
			}
			followingEventSystemIncoming = false;
		}

		jsonReadComplete = true;

		gameObject.GetComponent<FactoryMenuHandler>().updateTriggerList();
		gameObject.GetComponent<FactoryMenuHandler>().updateEventList();
		updateListSizes();
		FactoryMenuHandler menuHandler = gameObject.GetComponent<FactoryMenuHandler>();
		
		for (int i = triggerGroups.Count - 1; i >= 0; i--)
		{
			yield return new WaitForSeconds(0.5f);
			int group = triggerGroups[i];
			GameObject[] gameObjects = FindObjectsOfType<GameObject>(true);
			foreach (GameObject gObject in gameObjects)
			{
				if (gObject.GetComponent<ItemReferencer>() != null && gObject.GetComponent<ItemReferencer>().getEventGameObject() == triggers[0][i])
				{				
					for (int j = 0; j < group; j++)
					{
						menuHandler.changeGroup(gObject);
					}
					break;
				}
			}
		}

		for (int i = triggerGroups.Count - 1; i >= 0; i--)
		{
			int group = eventGroups[i];
			GameObject[] gameObjects = FindObjectsOfType<GameObject>(true);
			foreach (GameObject gObject in gameObjects)
			{
				if (gObject.GetComponent<ItemReferencer>() != null && gObject.GetComponent<ItemReferencer>().getEventGameObject() == events[0][i])
				{
					for (int j = 0; j < group; j++)
					{
						menuHandler.changeGroup(gObject);
					}
					break;
				}
			}
		}


		if (previousEventSystemFactory == null)
		{
			StartCoroutine(autoCreateEventSystem());
		}


	}

	/// <summary>
	/// Extracts the groupID from an event or trigger
	/// </summary>
	/// <param name="json"> The json from which the groupID gets extracted from </param>
	/// <returns></returns>
	private int extractGroupFromJSON(string json)
	{
		if (json != null)
		{
			JObject group = JObject.Parse(json);
			return group.GetValue("group").Value<Int32>();
		}
		return 0;
	}

	/// <summary>
	/// Creates an EventSystem automatically when all EventSystems have been loaded from the Json-Strings
	/// </summary>
	/// <returns></returns>
	IEnumerator autoCreateEventSystem()
	{
		GameObject[] gameObjects = FindObjectsOfType<GameObject>(true);
		List<GameObject> mandatoryFactories = new List<GameObject>();
		foreach (GameObject gameObject in gameObjects)
		{
			EventSystemFactory factory = gameObject.GetComponent<EventSystemFactory>();
			if (factory != null)
			{
				if (factory.getRootEventSystemFactory().GetComponent<EventSystemFactory>() == this)
				{
					mandatoryFactories.Add(factory.gameObject);
				}
			}
		}
		bool notCompleted = true;
		while (notCompleted)
		{
			notCompleted = false;
			yield return new WaitForSeconds(0.5f);
			foreach (GameObject factory in mandatoryFactories)
			{
				if (!factory.GetComponent<EventSystemFactory>().getJsonReadComplete())
				{
					notCompleted = true;
				}
			}
		}
		createEventSystem();
	}
}
/// <summary>
/// Class that defines the JSON structure of an EventSystem
/// </summary>
public class JsonEventSystem
{
	public int group;
	public string name;
	public string scene;
	public string type;
	public bool repeatable;
	public bool decouple;
	public List<string> triggers;
	public List<string> events;
}

