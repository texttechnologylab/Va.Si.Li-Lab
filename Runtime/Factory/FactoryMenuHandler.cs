using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Ubiq;
using Ubiq.Samples;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

/// <summary>
/// This script handles the UI side of the EventSystem Factories
/// </summary>
public class FactoryMenuHandler : MonoBehaviour
{
	[SerializeField] GameObject mainPanel;
	[SerializeField] GameObject triggerSelectionPanel;
	[SerializeField] GameObject timerPanel;
	[SerializeField] GameObject listItem;
	[SerializeField] GameObject eventSelectionPanel;
	[SerializeField] GameObject objectSpawningPanel;
	[SerializeField] GameObject addAreaPanel;
	[SerializeField] GameObject markerTool;
	[SerializeField] GameObject shapenetSelector;
	[SerializeField] GameObject interactionPanel;
	[SerializeField] GameObject feedbackPanel;
	[SerializeField] GameObject playerTelportationPanel;
	[SerializeField] GameObject objectManipulationPanel;
	[SerializeField] GameObject objectDestructionPanel;
	[SerializeField] GameObject objectMovingPanel;
	[SerializeField] GameObject templateCreatorPanel;
	[SerializeField] GameObject feedbackPanelEventWindow;
	[SerializeField] GameObject environmentPanel;
	[SerializeField] GameObject environmentPanelEventWindow;
	[SerializeField] GameObject votingPanel;
	EventSystemFactory factory;
	List<GameObject> panelList = new List<GameObject>();
	GameObject triggerDisplay;
	GameObject eventDisplay;
	bool previewOn = false;

	// ##### areaTrigger variables ######
	bool areaTriggerActivatedOnce = false;

	// ##### timer variables #####
	private int seconds;
	private int minutes;
	private int hours;
	private GameObject secondsDisplay;
	private GameObject minutesDisplay;
	private GameObject hoursDisplay;

	// ##### objectspawning variables #####
	private GameObject shapeNetObject;
	private string objectName;

	// ##### interaction trigger variables ######
	bool interactionTriggerActivatedOnce = false;


	//## feedback variables  ##
	private GameObject secondsDisplayForFeedback;
	private GameObject textFeedback;
	private UnityEngine.UI.Toggle[] toggles;
	private UnityEngine.UI.Toggle[] toggles2;

	//### environment ui variables

	private GameObject brightnessSlider;
	private GameObject rainIntensitySlider;
	private GameObject snowIntensitySlider;
	private GameObject cloudIntensitySlider;
	private GameObject fogIntensitySlider;
	private UnityEngine.UI.Toggle thunderstromToggle;
	private GameObject secondsDisplayForThunderstrom;


	//### voting ui variables
	private GameObject secondsDisplayForYesNoVoting;
	private GameObject secondsDisplayForMCQVoting;
	//Variable damit die Toggles wie Radio buttons funktionieren
	private UnityEngine.UI.Toggle[] toggles3;
	//Optionen bei der SingleChoiceQueszions hinzuzufügen
	private GameObject optionInputField;
	private GameObject addOptionButton;
	private Transform optionsParent;
	private List<string> scqOptions = new List<string>();
	private Text addedOptionsText;



	// ##### Player Teleportation variables
	private string playerCriteria = "all";

	// Start is called before the first frame update
	private void Start()
	{
		factory = gameObject.GetComponent<EventSystemFactory>();
		panelList.Add(mainPanel);
		panelList.Add(triggerSelectionPanel);
		panelList.Add(timerPanel);
		panelList.Add(objectSpawningPanel);
		panelList.Add(eventSelectionPanel);
		panelList.Add(addAreaPanel);
		panelList.Add(shapenetSelector);
		panelList.Add(interactionPanel);
		panelList.Add(feedbackPanel);
		panelList.Add(playerTelportationPanel);
		panelList.Add(objectManipulationPanel);
		panelList.Add(objectDestructionPanel);
		panelList.Add(objectMovingPanel);
		panelList.Add(templateCreatorPanel);
		panelList.Add(environmentPanel);
		panelList.Add(votingPanel);

		triggerDisplay = transform.Find("Menu/Canvas/Main/Items Panel/Trigger Panel/TriggerItems").gameObject;
		secondsDisplay = transform.Find("Menu/Canvas/Timer/Timer Settings/SecondsDisplay").gameObject;
		minutesDisplay = transform.Find("Menu/Canvas/Timer/Timer Settings/MinutesDisplay").gameObject;
		hoursDisplay = transform.Find("Menu/Canvas/Timer/Timer Settings/HoursDisplay").gameObject;
		eventDisplay = transform.Find("Menu/Canvas/Main/Items Panel/Event Panel/EventItems").gameObject;
		secondsDisplayForFeedback = transform.Find("Menu/Canvas/AddFeedback/Feedback Settings/Duration/SecondsDisplay").gameObject;
		textFeedback = transform.Find("Menu/Canvas/AddFeedback/Feedback Settings/PlaceholderText/Text Input Area/Text").gameObject;

		// ## environment
		brightnessSlider = transform.Find("Menu/Canvas/AddEnvironment/Environment Settings/WeatherSettingsContainer/BrightnessSlider").gameObject;
		rainIntensitySlider = transform.Find("Menu/Canvas/AddEnvironment/Environment Settings/WeatherSettingsContainer/RainIntensitySlider").gameObject;
		snowIntensitySlider = transform.Find("Menu/Canvas/AddEnvironment/Environment Settings/WeatherSettingsContainer/SnowIntensitySlider").gameObject;
		cloudIntensitySlider = transform.Find("Menu/Canvas/AddEnvironment/Additional Environment Settings/CloudIntensitySlider").gameObject;
		fogIntensitySlider = transform.Find("Menu/Canvas/AddEnvironment/Additional Environment Settings/FogIntensitySlider").gameObject;
		thunderstromToggle = transform.Find("Menu/Canvas/AddEnvironment/Additional Environment Settings/ThunderstromToggle").gameObject.GetComponent<UnityEngine.UI.Toggle>();
		secondsDisplayForThunderstrom = transform.Find("Menu/Canvas/AddEnvironment/Additional Environment Settings/Duration/SecondsDisplay").gameObject;

		// ## voting system
		secondsDisplayForYesNoVoting = transform.Find("Menu/Canvas/AddVoting/Voting Settings/YesNoContainer/Duration/SecondsDisplay").gameObject;
		secondsDisplayForMCQVoting = transform.Find("Menu/Canvas/AddVoting/Voting Settings/MCQContainer/Duration/SecondsDisplay").gameObject;
		addOptionButton = transform.Find("Menu/Canvas/AddVoting/Voting Settings/MCQContainer/AddOption/AddOptionButton").gameObject;
		UnityEngine.UI.Button addButton = addOptionButton.GetComponent<UnityEngine.UI.Button>();
		addButton.onClick.AddListener(AddOption); // Add listener to Add Option button
		addedOptionsText = transform.Find("Menu/Canvas/AddVoting/Voting Settings/MCQContainer/OptionText").gameObject.GetComponent<Text>();
		optionInputField = transform.Find("Menu/Canvas/AddVoting/Voting Settings/MCQContainer/AddOption/Text Input Area/Text").gameObject;


		CheckFeedbackToggleState();
		CheckFeedbackPositionToggleState();
		CheckVotingToggleState();

		secondsDisplayForFeedback = transform.Find("Menu/Canvas/AddFeedback/Feedback Settings/Duration/SecondsDisplay").gameObject;
		textFeedback = transform.Find("Menu/Canvas/AddFeedback/Feedback Settings/PlaceholderText/Text Input Area/Text").gameObject;

		updateEventList();
		updateTriggerList();
		if (gameObject.GetComponent<EventSystemFactory>().getPreviousEventSystemFactory() != null)
		{
			transform.Find("Menu/Canvas/Main/ControlPanel/DoneButton").gameObject.SetActive(false);
			LineRenderer lineRenderer = gameObject.GetComponent<LineRenderer>();
			if (lineRenderer == null)
			{
				lineRenderer = gameObject.AddComponent<LineRenderer>();
			}
			lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
			lineRenderer.widthMultiplier = 0.01f;
			lineRenderer.positionCount = 2;
			lineRenderer.startColor = Color.blue;
			lineRenderer.endColor = Color.cyan;
			transform.Find("Menu/Canvas/Main/PreviewToggle").gameObject.GetComponent<UnityEngine.UI.Toggle>().isOn = false;
			transform.Find("Menu/Canvas/Main/PreviewToggle/Background/CheckmarkOff").gameObject.SetActive(true);
			setPreviewOn(false);
		}
	}
	
	// Calls every frame independently from the fps
	private void FixedUpdate()
	{

		if (gameObject.GetComponent<EventSystemFactory>().getPreviousEventSystemFactory() != null)
		{
			Vector3 endpoint = transform.Find("ConnectingLineEndPoint").position;
			Vector3 startpoint = gameObject.GetComponent<EventSystemFactory>().getPreviousEventSystemFactory().transform.Find("ConnectingLineStartPoint").position;
			LineRenderer lineRenderer = transform.GetComponent<LineRenderer>();
			lineRenderer.SetPosition(0, startpoint);
			lineRenderer.SetPosition(1, endpoint);
		}
	}

	/// <summary>
	/// Switches to TriggerSelection Panel
	/// </summary>
	public void switchToTriggerSelection()
	{
		deactivateAllPanels();
		triggerSelectionPanel.SetActive(true);
	}

	/// <summary>
	/// Switches to Main Panel
	/// </summary>
	public void switchToMain()
	{
		deactivateAllPanels();
		//gameObject.GetComponent<HandGrabInteractable>().enabled = true;
		mainPanel.SetActive(true);
	}

	/// <summary>
	/// Switches to Timer Panel
	/// </summary>
	public void switchToTimer()
	{
		deactivateAllPanels();
		seconds = 0;
		minutes = 0;
		hours = 0;
		timerPanel.SetActive(true);
		updateTimerDisplay();

	}

	/// <summary>
	/// Switches toInteraction Panel
	/// </summary>
	public void switchToInteraction()
	{
		deactivateAllPanels();
		interactionPanel.transform.Find("Interaction Settings/ObjectSelectionMarker").localPosition = new Vector3(-50f, -20.2f, 0);
		interactionPanel.transform.Find("Interaction Settings/ObjectSelectionMarker").rotation = Quaternion.identity;
		interactionPanel.transform.Find("Interaction Settings/ObjectSelectionMarker/FlagMarker1").localPosition = Vector3.zero;
		interactionPanel.transform.Find("Interaction Settings/ObjectSelectionMarker").GetComponent<MarkerTool>().setTarget(null);
		interactionPanel.transform.Find("Interaction Settings/ObjectSelectionMarker").GetComponent<MarkerTool>().setTempTarget(null);
		interactionPanel.transform.Find("Interaction Settings/SelectedObjectLabel").GetComponent<Text>().text = "Selected Object:\r\nNone";
		interactionPanel.SetActive(true);
	}

	/// <summary>
	/// Switches to Feedback Panel
	/// </summary>
	public void switchToFeeback()
	{
		deactivateAllPanels();
		feedbackPanel.transform.Find("Feedback Settings").gameObject.SetActive(true);
		feedbackPanelEventWindow.SetActive(false);
		feedbackPanel.transform.Find("ControlPanel/BackButton").gameObject.SetActive(false);
		feedbackPanel.transform.Find("ControlPanel/NextButton").gameObject.SetActive(true);
		feedbackPanel.SetActive(true);
	}

	/// <summary>
	/// Switches to MoreFeedbacksettings Panel
	/// </summary>
	public void switchToMoreFeedbackSettings()
	{
		feedbackPanel.transform.Find("Feedback Settings").gameObject.SetActive(false);
		feedbackPanelEventWindow.SetActive(true);
		feedbackPanel.transform.Find("ControlPanel/BackButton").gameObject.SetActive(true);
		feedbackPanel.transform.Find("ControlPanel/NextButton").gameObject.SetActive(false);
		feedbackPanelEventWindow.SetActive(true);
	}

	/// <summary>
	/// Switches to Environment Panel
	/// </summary>
	public void switchToEnvironment()
	{
		deactivateAllPanels();
		environmentPanel.transform.Find("Environment Settings").gameObject.SetActive(true);
		environmentPanelEventWindow.SetActive(false);
		environmentPanel.transform.Find("ControlPanel/BackButton").gameObject.SetActive(false);
		environmentPanel.transform.Find("ControlPanel/NextButton").gameObject.SetActive(true);
		environmentPanel.SetActive(true);
	}

	/// <summary>
	/// Switches to EnvironmentPanelWindow Panel
	/// </summary>
	public void switchToEnvironmentPanelWindow()
	{
		environmentPanel.transform.Find("Environment Settings").gameObject.SetActive(false);
		environmentPanelEventWindow.SetActive(true);
		environmentPanel.transform.Find("ControlPanel/BackButton").gameObject.SetActive(true);
		environmentPanel.transform.Find("ControlPanel/NextButton").gameObject.SetActive(false);
		environmentPanelEventWindow.SetActive(true);
	}
	
	/// <summary>
	/// Switches to ObjectDestruction Panel
	/// </summary>
	public void switchToVotingSystem()
	{
		deactivateAllPanels();
		votingPanel.SetActive(true);
	}

	public void switchToObjectDestruction()
	{
		deactivateAllPanels();
		objectDestructionPanel.SetActive(true);
		objectDestructionPanel.transform.Find("Object Settings/ObjectSelectionMarker").GetComponent<MarkerTool>().setTarget(null);
		objectDestructionPanel.transform.Find("Object Settings/ObjectSelectionMarker").GetComponent<MarkerTool>().setTempTarget(null);
		objectDestructionPanel.transform.Find("Object Settings/ObjectSelectionMarker").localPosition = new Vector3(-51.4f, -5f, 0);
		objectDestructionPanel.transform.Find("Object Settings/ObjectSelectionMarker").rotation = Quaternion.identity;
		objectDestructionPanel.transform.Find("Object Settings/ObjectSelectionMarker/FlagMarker1").localPosition = Vector3.zero;
		objectDestructionPanel.transform.Find("Object Settings/SelectedObjectLabel").GetComponent<Text>().text = "Selected Object:\r\nNone";
	}

	/// <summary>
	/// Switches to ObjectManipulation Panel
	/// </summary>
	public void switchToObjectManipulation()
	{
		deactivateAllPanels();
		objectManipulationPanel.SetActive(true);
	}

	/// <summary>
	/// Switches to ObjectMoving Panel
	/// </summary>
	public void switchToObjectMoving()
	{
		deactivateAllPanels();

		objectMovingPanel.transform.Find("Object Settings/MarkerTool1").GetComponent<MarkerTool>().setPreviewGameObject(null);
		objectMovingPanel.transform.Find("Object Settings/MarkerTool1").localPosition = new Vector3(6.8f, -16.1f, -23f);
		objectMovingPanel.transform.Find("Object Settings/MarkerTool1").rotation = Quaternion.identity;
		objectMovingPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1").localPosition = Vector3.zero;
		objectMovingPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere").gameObject.SetActive(true);
		objectMovingPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere").localScale = new Vector3(4f, 4f, 4f);
		objectMovingPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere/ScalerBigger").gameObject.SetActive(false);
		objectMovingPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere/ScalerBigger").localPosition = new Vector3(-0.015f, 0f, 0f);
		objectMovingPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere/ScalerSmaller").gameObject.SetActive(false);
		objectMovingPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere/ScalerSmaller").localPosition = new Vector3(0.015f, 0f, 0f);
		objectMovingPanel.transform.Find("Object Settings/MarkerTool1").gameObject.GetComponent<MarkerTool>().setTarget(null);
		objectMovingPanel.transform.Find("Object Settings/MarkerTool1").gameObject.GetComponent<MarkerTool>().setTempTarget(null);
		if (objectMovingPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere").childCount == 3)
		{
			Destroy(objectMovingPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere").GetChild(2).gameObject);
		}
		objectMovingPanel.transform.Find("Object Settings/MarkerTool2").localPosition = new Vector3(-56.8f, -16.1f, -17.2449f);
		objectMovingPanel.transform.Find("Object Settings/MarkerTool2").rotation = Quaternion.identity;
		objectMovingPanel.transform.Find("Object Settings/MarkerTool2/FlagMarker1").localPosition = Vector3.zero;
		objectMovingPanel.transform.Find("Object Settings/MarkerTool2").gameObject.SetActive(true);
		objectMovingPanel.transform.Find("Object Settings/MarkerTool2").gameObject.GetComponent<MarkerTool>().setTarget(null);
		objectMovingPanel.transform.Find("Object Settings/MarkerTool2").gameObject.GetComponent<MarkerTool>().setTempTarget(null);
		objectMovingPanel.transform.Find("Object Settings/SelectedObjectLabel").gameObject.GetComponent<Text>().text = "Selected Object:\nNothing";
		objectMovingPanel.SetActive(true);
	}

	/// <summary>
	/// Switches to TemplateCreator Panel
	/// </summary>
	public void switchToTemplateCreator()
	{
		deactivateAllPanels();
		templateCreatorPanel.transform.Find("Template Settings/InputField/Text").gameObject.GetComponent<Text>().text = "";
		templateCreatorPanel.SetActive(true);
	}

	/// <summary>
	/// Disables Preview after EventSystem is created
	/// </summary>
	public void togglePreviewOnDone()
	{
		FactoryMenuHandler factoryMenuHandler;
		foreach (GameObject gameObject in GameObject.FindObjectsOfType<GameObject>())
		{
			factoryMenuHandler = gameObject.GetComponent<FactoryMenuHandler>();
			if (factoryMenuHandler != null && factoryMenuHandler.getPreviewOn())
			{
				factoryMenuHandler.updatePreview(false);
			}
		}
	}

	/// <summary>
	/// Toggles if preview of EventSystem is shown
	/// </summary>
	public void togglePreview()
	{
		FactoryMenuHandler factoryMenuHandler;
		foreach (GameObject gameObject in GameObject.FindObjectsOfType<GameObject>())
		{
			factoryMenuHandler = gameObject.GetComponent<FactoryMenuHandler>();
			if (factoryMenuHandler != null && factoryMenuHandler.gameObject.transform.parent == null)
			{
				if (factoryMenuHandler == this)
				{
					previewOn = !previewOn;
					GameObject checkmark_off = gameObject.transform.Find("Menu/Canvas/Main/PreviewToggle/Background/CheckmarkOff").gameObject;
					checkmark_off.SetActive(!checkmark_off.activeSelf);
					updatePreview(previewOn);
				}
				else if (factoryMenuHandler.getPreviewOn())
				{
					factoryMenuHandler.transform.Find("Menu/Canvas/Main/PreviewToggle").gameObject.GetComponent<UnityEngine.UI.Toggle>().SetIsOnWithoutNotify(false);
					factoryMenuHandler.transform.Find("Menu/Canvas/Main/PreviewToggle/Background/CheckmarkOff").gameObject.SetActive(true);
					factoryMenuHandler.setPreviewOn(false);
					factoryMenuHandler.updatePreview(false);
				}
			}

		}

	}

	/// <summary>
	/// Switches to ObjectSpawn Panel
	/// </summary>
	public void switchToObjectSpawn()
	{
		deactivateAllPanels();
		shapeNetObject = null;
		objectName = null;
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1").GetComponent<MarkerTool>().setPreviewGameObject(null);
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1").localPosition = new Vector3(58.2f, -11.1f, -14.3f);
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1").rotation = Quaternion.identity;
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1").gameObject.GetComponent<MarkerTool>().setTarget(null);
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1").gameObject.GetComponent<MarkerTool>().setTempTarget(null);
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1").localPosition = Vector3.zero;
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere").gameObject.SetActive(true);
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere").localScale = new Vector3(4f, 4f, 4f);
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere/ScalerBigger").gameObject.SetActive(false);
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere/ScalerBigger").localPosition = new Vector3(-0.015f, 0f, 0f);
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere/ScalerSmaller").gameObject.SetActive(false);
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere/ScalerSmaller").localPosition = new Vector3(0.015f, 0f, 0f);
		if (objectSpawningPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere").childCount == 3)
		{
			Destroy(objectSpawningPanel.transform.Find("Object Settings/MarkerTool1/FlagMarker1/Sphere").GetChild(2).gameObject);
		}
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool2").localPosition = new Vector3(-44f, -11.1f, -14.3f);
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool2").rotation = Quaternion.identity;
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool2").gameObject.GetComponent<MarkerTool>().setTarget(null);
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool2").gameObject.GetComponent<MarkerTool>().setTempTarget(null);
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool2/FlagMarker1").localPosition = Vector3.zero;
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool2").gameObject.SetActive(false);

		objectSpawningPanel.transform.Find("Object Settings/SelectedObjectLabel").gameObject.GetComponent<Text>().text = "Selected Object:\nNothing";
		objectSpawningPanel.transform.Find("Object Settings/SelectShapeNet").gameObject.SetActive(true);
		objectSpawningPanel.transform.Find("Object Settings/SelectMarker").gameObject.SetActive(true);
		objectSpawningPanel.SetActive(true);

	}

	/// <summary>
	/// Switches to EventSelection Panel
	/// </summary>
	public void switchToEventSelection()
	{
		deactivateAllPanels();
		eventSelectionPanel.SetActive(true);
	}

	/// <summary>
	/// Switches to Area-Trigger-Creation Panel
	/// </summary>
	public void switchToAddArea()
	{
		//Destroy(addAreaPanel.transform.Find("Area Settings").Find("MarkerTool").gameObject);
		//GameObject markerToolClone = Instantiate(markerTool, addAreaPanel.transform.Find("Area Settings"));
		//markerToolClone.name = "MarkerTool";
		deactivateAllPanels();

		//addAreaPanel.transform.Find("Area Settings/MarkerTool/FlagMarker1").localPosition = Vector3.zero;
		//addAreaPanel.transform.Find("Area Settings/MarkerTool/FlagMarker2").localPosition = new Vector3(2.8f, -0.045f, 0.011f);
		//markerToolClone.transform.rotation = Quaternion.identity;

		addAreaPanel.transform.Find("Area Settings/ObjectSelectionMarker/FlagMarker1").localPosition = Vector3.zero;
		addAreaPanel.transform.Find("Area Settings/ObjectSelectionMarker").localPosition = new Vector3(-30.5f, -24.5f, 0);
		//addAreaPanel.transform.Find("Area Settings/ObjectSelectionMarker").rotation = Quaternion.identity;
		
		addAreaPanel.transform.Find("Area Settings/ObjectSelectionMarker").gameObject.GetComponent<MarkerTool>().setTarget(null);
		addAreaPanel.transform.Find("Area Settings/ObjectSelectionMarker").gameObject.GetComponent<MarkerTool>().setTempTarget(null);
	
		addAreaPanel.transform.Find("Area Settings/SelectedObjectLabel").GetComponent<Text>().text = "Selected Object:\r\nNone";
		
		shapenetSelector.SetActive(false);
		addAreaPanel.SetActive(true);
	}

	/// <summary>
	/// Switches to PlayerTeleportation Panel
	/// </summary>
	public void switchToPlayerTeleportation()
	{
		deactivateAllPanels();
		
		playerCriteria = null;
		playerTelportationPanel.transform.Find("TargetText").gameObject.GetComponent<Text>().text = "Target:\nNobody";
		playerTelportationPanel.transform.Find("Teleportation Settings/MarkerTool/FlagMarker1").localPosition = Vector3.zero;
		playerTelportationPanel.SetActive(true);
	}

	/// <summary>
	/// Deactives all Panels
	/// </summary>
	public void deactivateAllPanels()
	{
		foreach (GameObject panel in panelList)
		{
			panel.SetActive(false);
		}
        //gameObject.GetComponent<HandGrabInteractable>().enabled = false;
    }

	/// <summary>
	/// Adds time to the timer trigger
	/// </summary>
	public void addTime(string type)
	{
		if (type.Equals("seconds"))
		{
			seconds++;
		}
		else if (type.Equals("minutes"))
		{
			minutes++;
		}
		else
		{
			hours++;
		}
		updateTimerDisplay();
	}

	/// <summary>
	/// Subtracts time from the timer trigger
	/// </summary>
	public void subtractTime(string type)
	{
		if (type.Equals("seconds"))
		{
			seconds--;
			if (seconds < 0)
			{
				seconds = 0;
			}
		}
		else if (type.Equals("minutes"))
		{
			minutes--;
			if (minutes < 0)
			{
				minutes = 0;
			}
		}
		else
		{
			hours--;
			if (hours < 0)
			{
				hours = 0;
			}
		}

		updateTimerDisplay();
	}

	/// <summary>
	/// Updates the current selected timer texts on the timer panel and the feedback panel
	/// </summary>
	public void updateTimerDisplay()
	{
		secondsDisplayForFeedback.GetComponent<Text>().text = seconds.ToString() + " seconds";
		secondsDisplayForThunderstrom.GetComponent<Text>().text = seconds.ToString() + " seconds delay after each lightning strike";
		secondsDisplay.GetComponent<Text>().text = seconds.ToString() + " seconds";
		minutesDisplay.GetComponent<Text>().text = minutes.ToString() + " minutes";
		hoursDisplay.GetComponent<Text>().text = hours.ToString() + " hours";

		// ## seconds update for Voting Types
		secondsDisplayForYesNoVoting.GetComponent<Text>().text = seconds.ToString() + " seconds";
		secondsDisplayForMCQVoting.GetComponent<Text>().text = seconds.ToString() + " seconds";
	}

	/// <summary>
	/// Gets information from the panel and calls factory to create the environment change event
	/// </summary>
	public void createEnvironment()
	{
		updateTimerDisplay();

		float brightnessValue = brightnessSlider.GetComponent<UnityEngine.UI.Slider>().value;
		float rainValue = rainIntensitySlider.GetComponent<UnityEngine.UI.Slider>().value;
		float snowValue = snowIntensitySlider.GetComponent<UnityEngine.UI.Slider>().value;
		float cloudValue = cloudIntensitySlider.GetComponent<UnityEngine.UI.Slider>().value;
		float fogValue = fogIntensitySlider.GetComponent<UnityEngine.UI.Slider>().value;

		//Parametrise the delay of the Thunderstrom
		secondsDisplayForThunderstrom.GetComponent<Text>().text = seconds.ToString();
		float delay = float.Parse(secondsDisplayForThunderstrom.GetComponent<Text>().text);

		Boolean thunderstromIsOn = thunderstromToggle.isOn;

		//Read environment Settings
		factory.createEnvironment(brightnessValue, rainValue, snowValue, cloudValue, fogValue, thunderstromIsOn, delay);
		updateEventList();
	}

	/// <summary>
	/// Get value of the active toggle
	/// </summary>
	public void createVotingWindow()
	{	

		updateTimerDisplay();
		UnityEngine.UI.Toggle yesNoToggle = votingPanel.transform.Find("Voting Settings/ToggleTypes/YesNoToggle").gameObject.GetComponent<UnityEngine.UI.Toggle>();
		UnityEngine.UI.Toggle mcqToggle = votingPanel.transform.Find("Voting Settings/ToggleTypes/MCQToggle").gameObject.GetComponent<UnityEngine.UI.Toggle>();

		secondsDisplayForYesNoVoting.GetComponent<Text>().text = seconds.ToString();
		secondsDisplayForMCQVoting.GetComponent<Text>().text = seconds.ToString();

		string type = null;
		string question = null;
		float duration = 0;
		if (yesNoToggle.isOn)
		{
			type = yesNoToggle.GetComponentInChildren<Text>().text;
			question = votingPanel.transform.Find("Voting Settings/YesNoContainer/QuestionText/Text Input Area/Text").GetComponent<Text>().text;
			duration = float.Parse(secondsDisplayForYesNoVoting.GetComponent<Text>().text);
			Debug.Log(type +", "+question+", "+duration);
			
		}
		else if (mcqToggle.isOn)
		{
			type = mcqToggle.GetComponentInChildren<Text>().text;
			question = votingPanel.transform.Find("Voting Settings/MCQContainer/QuestionText/Text Input Area/Text").GetComponent<Text>().text;
			duration = float.Parse( secondsDisplayForMCQVoting.GetComponent<Text>().text);
			Debug.Log(type +", "+question+", "+duration);
		}

		factory.createVoting(type, question, duration , scqOptions);
		updateEventList();


	}
	/// <summary>
	/// Um die Auswahl bei den SingleChoiceQuestions während der Laufzeit zu erfassen
	/// </summary>
	void AddOption()
	{
		string optionText = optionInputField.GetComponent<Text>().text.Trim(); // Get text from input field and trim whitespace

		if (!string.IsNullOrEmpty(optionText) && !scqOptions.Contains(optionText))
		{
			scqOptions.Add(optionText); // Add option to list

			// Update UI to display added options
			UpdateAddedOptionsText();

			GameObject optionButtonObject = new GameObject("OptionToggle");
			optionButtonObject.transform.SetParent(optionsParent);

			UnityEngine.UI.Toggle optionButton = optionButtonObject.AddComponent<UnityEngine.UI.Toggle>();
			optionButton.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 30f); // Set button size

			Text buttonText = optionButtonObject.AddComponent<Text>();
			buttonText.text = optionText;
			buttonText.alignment = TextAnchor.MiddleLeft;

			// Clear input field
			optionInputField.GetComponent<Text>().text = "";
		}
	}

	// Update the text field to display added options
	void UpdateAddedOptionsText()
	{
		string optionsString = "Options added: ";
		foreach (string option in scqOptions)
		{
			optionsString += option + ", ";
		}
		addedOptionsText.text = optionsString.TrimEnd(',', ' '); // Remove the trailing comma and space
	}


	// Get the value of the active toggle
	private string GetActiveToggleValue()
	{
		foreach (UnityEngine.UI.Toggle toggle in toggles)
		{
			if (toggle.isOn)
			{
				return toggle.GetComponentInChildren<Text>().text;
			}
		}

		// Default value if no toggle is active
		return "DefaultFeedbackType";
	}

	/// <summary>
	/// Get value of the active toggle
	/// </summary>
	/// <param name="parentObject"> The parent GameObject </param>
	/// <returns></returns>
	private string GetActiveToggleValue(GameObject parentObject)
	{
		UnityEngine.UI.Toggle[] toggles = parentObject.GetComponentsInChildren<UnityEngine.UI.Toggle>();

		foreach (UnityEngine.UI.Toggle toggle in toggles)
		{
			if (toggle.isOn)
			{
				return toggle.GetComponentInChildren<Text>().text;
			}
		}

		// Default value if no toggle is active
		return "DefaultFeedbackType";
	}

	///
	public void CheckFeedbackToggleState()
	{
		GameObject feedbackSettings = feedbackPanel.transform.Find("Feedback Settings/TypeText").gameObject;

		if (feedbackSettings != null)
		{
			toggles = feedbackSettings.GetComponentsInChildren<UnityEngine.UI.Toggle>();


			if (feedbackSettings != null)
			{
				// Get the Toggle components from the FeedbackSettings GameObject
				toggles = feedbackSettings.GetComponentsInChildren<UnityEngine.UI.Toggle>();

				// Disable all toggles except the one that is active
				foreach (UnityEngine.UI.Toggle toggle in toggles)
				{
					toggle.onValueChanged.AddListener(delegate { ToggleStateChanged(toggle); });
				}
			}
			else
			{
				Debug.LogError("FeedbackSettings object not found.");
			}

		}
	}

	public void CheckFeedbackPositionToggleState()
	{

		GameObject feedbackMoreSettings = feedbackPanelEventWindow.transform.Find("Position").gameObject;

		if (feedbackMoreSettings != null)
		{
			// Get the Toggle components from the FeedbackSettings GameObject
			toggles2 = feedbackMoreSettings.GetComponentsInChildren<UnityEngine.UI.Toggle>();

			// Disable all toggles except the one that is active
			foreach (UnityEngine.UI.Toggle toggle in toggles2)
			{
				toggle.onValueChanged.AddListener(delegate { ToggleStateChanged2(toggle); });
			}
		}
		else
		{
			Debug.LogError("FeedbackSettings object not found.");
		}

	}

	public void CheckVotingToggleState()
	{

		GameObject votingSettings = votingPanel.transform.Find("Voting Settings/ToggleTypes").gameObject;

		if (votingSettings != null)
		{

			toggles3 = votingSettings.GetComponentsInChildren<UnityEngine.UI.Toggle>();
			foreach (UnityEngine.UI.Toggle toggle in toggles3)
			{
				toggle.onValueChanged.AddListener(delegate { ToggleStateChanged3(toggle); });
			}
		}
		else
		{
			Debug.LogError("FeedbackSettings object not found.");
		}

	}
	public void ToggleStateChanged(UnityEngine.UI.Toggle changedToggle)
	{
		if (changedToggle.isOn)
		{
			foreach (UnityEngine.UI.Toggle toggle in toggles)
			{
				if (toggle != changedToggle)
				{
					toggle.interactable = false;
				}
			}
		}
		else
		{
			foreach (UnityEngine.UI.Toggle toggle in toggles)
			{
				toggle.interactable = true;
			}
		}
	}
	public void ToggleStateChanged2(UnityEngine.UI.Toggle changedToggle)
	{
		if (changedToggle.isOn)
		{
			foreach (UnityEngine.UI.Toggle toggle in toggles2)
			{
				if (toggle != changedToggle)
				{
					toggle.interactable = false;
				}
			}
		}
		else
		{
			foreach (UnityEngine.UI.Toggle toggle in toggles2)
			{
				toggle.interactable = true;
			}
		}
	}

	/// <summary>
	/// Gets information from the panel and calls factory to create the a timer trigger
	/// </summary>
	public void ToggleStateChanged3(UnityEngine.UI.Toggle changedToggle)
	{
		if (changedToggle.isOn)
		{
			foreach (UnityEngine.UI.Toggle toggle in toggles3)
			{
				if (toggle != changedToggle)
				{
					toggle.interactable = false;
				}
			}
		}
		else
		{
			foreach (UnityEngine.UI.Toggle toggle in toggles3)
			{
				toggle.interactable = true;
			}
		}
	}

	public void createTimer()
	{
		factory.createTimer(seconds + 60 * minutes + (60 * 60) * hours);
		updateTriggerList();
	}

	/// <summary>
	/// Gets information from the panel and calls factory to create an area trigger
	/// </summary>
	public void createAreaTrigger()
	{
		GameObject markerTool = addAreaPanel.transform.Find("Area Settings/MarkerTool").gameObject;
		GameObject objectMarkerTool = addAreaPanel.transform.Find("Area Settings/ObjectSelectionMarker").gameObject;
		factory.createAreaTrigger(markerTool.transform.Find("Area").gameObject, objectMarkerTool.GetComponent<MarkerTool>().getTarget(), areaTriggerActivatedOnce, "");
		updateTriggerList();
	}

	/// <summary>
	/// Gets information from the panel and calls factory to create an interaction trigger
	/// </summary>
	public void createInteraction()
	{
		GameObject objectMarkerTool = interactionPanel.transform.Find("Interaction Settings/ObjectSelectionMarker").gameObject;
		factory.createInteraction(objectMarkerTool.GetComponent<MarkerTool>().getTarget(), interactionTriggerActivatedOnce, objectMarkerTool.GetComponent<MarkerTool>().getTarget().name);
		updateTriggerList();
	}

	/// <summary>
	/// Gets information from the panel and calls factory to create a feedback event
	/// </summary>
	public void createFeedback()
	{
		updateTimerDisplay();
		secondsDisplayForFeedback.GetComponent<Text>().text = seconds.ToString();

		// Get active toggle and its value
		string feedbackType = GetActiveToggleValue();
		string feedbackPositon = GetActiveToggleValue(feedbackPanelEventWindow.transform.Find("Position").gameObject);
		factory.createFeedback(textFeedback.GetComponent<Text>().text, float.Parse(secondsDisplayForFeedback.GetComponent<Text>().text), feedbackType, feedbackPositon);
		updateEventList();

	}

	/// <summary>
	/// Gets information from the panel and calls factory to create the an object spawn event
	/// </summary>
	public void createObjectSpawn()
	{
		bool usedShapeNetObject = true;
		if (shapeNetObject == null)
		{
			if (objectSpawningPanel.transform.Find("Object Settings/SelectShapeNet").gameObject.activeSelf == false)
			{
				usedShapeNetObject = false;
				shapeNetObject = objectSpawningPanel.transform.Find("Object Settings/MarkerTool2").gameObject.GetComponent<MarkerTool>().getTarget();
				objectName = shapeNetObject.name;
			}

			if (shapeNetObject == null)
			{
				return;
			}
		}
		string randomString = " TEST";

		bool objectNameAlreadyExists;
		do
		{
			objectNameAlreadyExists = false;
			randomString = createRandomString(4);
			GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>(true);
			foreach (GameObject gameObject in gameObjects)
			{
				if (gameObject.name.Equals(objectName + " " + randomString))
				{
					objectNameAlreadyExists = true;
					break;
				}
			}

		}
		while (objectNameAlreadyExists);
		objectName += " " + randomString;
		GameObject markerTool = objectSpawningPanel.transform.Find("Object Settings/MarkerTool1").gameObject;
		Vector3 spawnPosition = markerTool.transform.Find("FlagMarker1").position;
		Quaternion spawnRotation = markerTool.GetComponent<MarkerTool>().getPreviewGameObject().transform.rotation;
		Vector3 spawnScale = markerTool.GetComponent<MarkerTool>().getPreviewGameObject().transform.lossyScale;
		factory.createObjectSpawn(spawnPosition, spawnRotation, spawnScale, shapeNetObject, objectName, usedShapeNetObject, null);
		updateEventList();
	}

	/// <summary>
	/// Creates a random string
	/// </summary>
	/// <param name="stringLength"> Defines the size of the random string </param>
	/// <returns></returns>
	public string createRandomString(int stringLength)
	{
		System.Random random = new System.Random();
		int randomValue;
		string generatedString = "";
		char newLetter;
		for (int i = 0; i < stringLength; i++)
		{
			randomValue = random.Next(0, 26);
			newLetter = Convert.ToChar(randomValue + 65);
			generatedString += newLetter;
		}
		return generatedString;
	}

	/// <summary>
	/// Sets the player criteria for player teleportation and updates the displayed text
	/// </summary>
	/// <param name="criteria"> The selected criteria </param>
	public void setPlayerCriteria(string criteria)
	{
		playerCriteria = criteria;
		if (playerTelportationPanel.activeSelf == true)
		{
			Text textToModify = playerTelportationPanel.transform.Find("TargetText").GetComponent<Text>();
			switch (criteria)
			{
				case "all":
					textToModify.text = "Target:\nEverybody";
					break;

				case "triggering users":
					textToModify.text = "Target:\nUsers who participate";
					break;

			}
		}
	}

	/// <summary>
	/// Gets information from the panel and calls factory to create the a player teleportation event
	/// </summary>
	public void createPlayerTeleportation()
	{
		Vector3 position = playerTelportationPanel.transform.Find("Teleportation Settings/MarkerTool/FlagMarker1").position;
		position.y = 0;
		Vector3 rotationEuler = playerTelportationPanel.transform.Find("Teleportation Settings/MarkerTool/FlagMarker1").localRotation.eulerAngles;
		rotationEuler.y += 90;

		factory.createPlayerTeleportation(playerCriteria, position, Quaternion.Euler(rotationEuler));
		updateEventList();
	}

	/// <summary>
	/// Gets information from the panel and calls factory to create the an object destruction event
	/// </summary>
	public void createObjectDestruction()
	{
		GameObject objectMarkerTool = objectDestructionPanel.transform.Find("Object Settings/ObjectSelectionMarker").gameObject;
		bool eraze = objectDestructionPanel.transform.Find("Object Settings/ErazorToggle").gameObject.GetComponent<UnityEngine.UI.Toggle>().isOn;
		GameObject target = objectMarkerTool.GetComponent<MarkerTool>().getTarget();
		if (target != null)
		{
			factory.createObjectDestruction(objectMarkerTool.GetComponent<MarkerTool>().getTarget(), eraze, "");
		}
		updateEventList();
	}

	/// <summary>
	/// Gets information from the panel and calls factory to create the an object moving event
	/// </summary>
	public void createObjectMoving()
	{
		GameObject markerTool1 = objectMovingPanel.transform.Find("Object Settings/MarkerTool1").gameObject;
		GameObject markerTool2 = objectMovingPanel.transform.Find("Object Settings/MarkerTool2").gameObject;
		GameObject target = markerTool2.GetComponent<MarkerTool>().getTarget();
		Vector3 position = markerTool1.transform.Find("FlagMarker1").position;
		Quaternion rotation = markerTool1.transform.Find("FlagMarker1/Sphere").GetChild(2).rotation;
		factory.createObjectMoving(target, position, rotation, "");
		updateEventList();
	}

	/// <summary>
	/// Updates the list of triggers on the main panel
	/// </summary>
	public void updateTriggerList()
	{
		triggerDisplay.transform.parent.Find("DefaultCaseText").gameObject.SetActive(false);

		for (int i = triggerDisplay.transform.childCount - 1; i >= 0; i--)
		{
			Destroy(triggerDisplay.transform.GetChild(i).gameObject);
		}
		for (int i = 0; i < factory.getTriggers().Count; i++)
		{
			foreach (GameObject item in factory.getTriggers()[i])
			{
				GameObject trigger = Instantiate(listItem);
				trigger.transform.Find("Label").GetComponent<Text>().text = item.name;
				if (i == 0)
				{
					trigger.transform.Find("Group/GroupLabel").GetComponent<Text>().text = "ALL";
				}
				else
				{
					trigger.transform.Find("Group/GroupLabel").GetComponent<Text>().text = i.ToString();
				}
				trigger.transform.SetParent(triggerDisplay.transform, false);
				trigger.GetComponent<ItemReferencer>().setEventGameObject(item);
				trigger.GetComponent<ItemReferencer>().setMenuHandler(this);
			}
		}
		if (triggerDisplay.transform.childCount == 0)
		{
			triggerDisplay.transform.parent.Find("DefaultCaseText").gameObject.SetActive(true);
		}
	}

	/// <summary>
	/// Updates the list of events on the main panel
	/// </summary>
	public void updateEventList()
	{
		for (int i = eventDisplay.transform.childCount - 1; i >= 0; i--)
		{
			Destroy(eventDisplay.transform.GetChild(i).gameObject);
		}

		for (int i = 0; i < factory.getEvents().Count; i++)
		{
			foreach (GameObject item in factory.getEvents()[i])
			{
				GameObject eSevent = Instantiate(listItem);
				eSevent.transform.Find("Label").GetComponent<Text>().text = item.name;
				if (i == 0)
				{
					eSevent.transform.Find("Group/GroupLabel").GetComponent<Text>().text = "ALL";
				}
				else
				{
					eSevent.transform.Find("Group/GroupLabel").GetComponent<Text>().text = i.ToString();
				}
				eSevent.transform.SetParent(eventDisplay.transform, false);
				eSevent.GetComponent<ItemReferencer>().setEventGameObject(item);
				eSevent.GetComponent<ItemReferencer>().setMenuHandler(this);
			}
		}
	}

	/// <summary>
	/// Toggles if an area trigger has to be activated only once
	/// </summary>
	public void toggleAreaActivatedOnlyOnce()
	{
		areaTriggerActivatedOnce = !areaTriggerActivatedOnce;
	}

	/// <summary>
	/// Toggles if an interaction trigger has to be activated only once
	/// </summary>
	public void toggleInteractionTriggerActivatedOnlyOnce()
	{
		interactionTriggerActivatedOnce = !interactionTriggerActivatedOnce;
	}

	/// <summary>
	/// Sets the GameObject selected via Shapenet
	/// </summary>
	/// <param name="shapeNetObject"> The desired GameObject from ShapeNet </param>
	public void setShapeNetObject(GameObject shapeNetObject)
	{
		this.shapeNetObject = shapeNetObject.transform.GetChild(1).gameObject;
		this.shapeNetObject.GetComponent<Rigidbody>().useGravity = false;
		objectSpawningPanel.transform.Find("Object Settings/SelectedObjectLabel").gameObject.GetComponent<Text>().text = "Selected Object:\n" + shapeNetObject.name;
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool1").GetComponent<MarkerTool>().setTarget(this.shapeNetObject);

	}

	/// <summary>
	/// Sets the name of the spawning GameObject
	/// </summary>
	/// <param name="objectName"> The name of the GameObject</param>
	public void setObjectName(string objectName)
	{
		this.objectName = objectName;
	}

	/// <summary>
	/// Enables the ShapeNetBrowser for object spawning
	/// </summary>
	public void selectShapeNetBrowser()
	{
		shapenetSelector.SetActive(true);
		objectSpawningPanel.transform.Find("Object Settings/SelectShapeNet").gameObject.SetActive(false);
		objectSpawningPanel.transform.Find("Object Settings/SelectMarker").gameObject.SetActive(false);
	}

	/// <summary>
	/// Enables the MarkerTool for object spawning
	/// </summary>
	public void selectFlagMarker()
	{
		objectSpawningPanel.transform.Find("Object Settings/MarkerTool2").gameObject.SetActive(true);
		objectSpawningPanel.transform.Find("Object Settings/SelectShapeNet").gameObject.SetActive(false);
		objectSpawningPanel.transform.Find("Object Settings/SelectMarker").gameObject.SetActive(false);
	}

	/// <summary>
	/// Increases the groupID of an event or trigger by one
	/// </summary>
	/// <param name="itemUI"> The UI Panel GameObject that represents the event/trigger </param>
	public void changeGroup(GameObject itemUI)
	{
		GameObject label = itemUI.transform.Find("Group/GroupLabel").gameObject;
		Text text = label.GetComponent<Text>();
		int oldGroup = 0;
		if (!text.text.Equals("ALL"))
		{
			oldGroup = Int32.Parse(text.text);
		}

		int newGroup = 0;
		if (text.text.Equals("ALL"))
		{
			if (factory.getTriggers().Count > 1 && factory.getEvents().Count > 1)
			{
				newGroup = 1;
			}
		}
		else
		{
			newGroup += 1 + oldGroup;
			if (newGroup > factory.getTriggers().Count - 1)
			{
				newGroup -= factory.getTriggers().Count;
			}
		}

		if (newGroup == 0)
		{
			text.text = "ALL";
		}
		else
		{
			text.text = newGroup.ToString();
		}
		GameObject targetedItem = itemUI.GetComponent<ItemReferencer>().getEventGameObject();

		foreach (GameObject item in factory.getEvents()[oldGroup])
		{
			if (item == targetedItem)
			{

				factory.getEvents()[newGroup].Add(item);
				factory.getEvents()[oldGroup].Remove(item);
				return;
			}
		}
		foreach (GameObject item in factory.getTriggers()[oldGroup])
		{
			if (item == targetedItem)
			{

				factory.getTriggers()[newGroup].Add(item);
				factory.getTriggers()[oldGroup].Remove(item);
				return;
			}
		}

	}

	/// <summary>
	/// Returns if the preview is turned on
	/// </summary>
	/// <returns></returns>
	public bool getPreviewOn()
	{
		return previewOn;
	}

	/// <summary>
	/// Sets if the preview is turned on
	/// </summary>
	/// <param name="previewOn"> Boolean value if preview is turned on </param>
	public void setPreviewOn(bool previewOn)
	{
		this.previewOn = previewOn;
	}

	/// <summary>
	/// Turns the preview on or off
	/// </summary>
	/// <param name="on"></param>
	public void updatePreview(bool on)
	{
		foreach (List<GameObject> eventList in factory.getEvents())
		{
			foreach (GameObject eSEvent in eventList)
			{
				if (on)
				{
					eSEvent.GetComponent<IEvent>().previewOn();
				}
				else
				{
					eSEvent.GetComponent<IEvent>().previewOff();
				}

			}
		}

		foreach (List<GameObject> triggerList in factory.getTriggers())
		{
			foreach (GameObject trigger in triggerList)
			{
				if (on)
				{
					trigger.GetComponent<ITrigger>().previewOn();
				}
				else
				{
					trigger.GetComponent<ITrigger>().previewOff();
				}
			}
		}
	}


}
