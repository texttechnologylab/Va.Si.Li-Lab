using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Ubiq.XR;
using UnityEngine;
using UnityEngine.UI;
using VaSiLi.Object;
/// <summary>
/// This script handles the MarkerTool that is used for creating events and triggers
/// </summary>
public class MarkerTool : MonoBehaviour
{
	[SerializeField] short mode = 1;
	private GameObject flagMarker1 = null;
	private GameObject flagMarker2 = null;
	[SerializeField] GameObject target = null;
	private GameObject tempTarget = null;
	private Color[] originalColorsTemp = null;
	private Color[] originalColors = null;
	private GameObject previousTarget = null;
	private GameObject previewGameObject = null;
	// Mesh attributes
	private GameObject area = null;
	private MeshFilter meshFilter = null;
	public Vector3[] verticesClone = null;

	// Flag Materials
	[SerializeField] Material flagColorRed = null;
	[SerializeField] Material flagColorBlue = null;
	[SerializeField] Material flagColorGreen = null;

	// Start is called before the first frame update
	void Start()
	{
		flagMarker1 = transform.Find("FlagMarker1").gameObject;
		flagMarker2 = transform.Find("FlagMarker2").gameObject;
		area = transform.Find("Area").gameObject;
		meshFilter = area.GetComponent<MeshFilter>();
		verticesClone = meshFilter.mesh.vertices;
		verticesClone[0] = new Vector3(0, 0, 0);
		verticesClone[13] = new Vector3(0, 0, 0);
		verticesClone[23] = new Vector3(0, 0, 0);
		setMode(mode);
	}

	// Update is called once per frame
	void Update()
	{
		if (mode == 1)
		{
			applyVerticesToArea();
		}
		else if (mode == 0)
		{
			if (target != null && previousTarget != target)
			{
				Destroy(previewGameObject);
				transform.Find("FlagMarker1").Find("Sphere").Find("ScalerBigger").gameObject.SetActive(true);
				transform.Find("FlagMarker1").Find("Sphere").Find("ScalerSmaller").gameObject.SetActive(true);
				previewGameObject = Instantiate(target);
				previewGameObject.transform.SetParent(transform.Find("FlagMarker1").Find("Sphere"));
				previewGameObject.transform.localPosition = Vector3.zero;

				if (previewGameObject.GetComponent<Rigidbody>() != null)
				{
					previewGameObject.GetComponent<Rigidbody>().isKinematic = true;
					previewGameObject.GetComponent<Rigidbody>().useGravity = false;
				}


				previousTarget = target;
				previewGameObject.SetActive(true);
			}
		}
	}

	/// <summary>
	/// Sets the mode the MarkerTool is used in
	/// [0 -> PositionMarker; 1 -> AreaMarker; 2 -> ObjectMarker]
	/// </summary>
	/// <param name="mode"> [0 -> PositionMarker; 1 -> AreaMarker; 2 -> ObjectMarker] </param>
	public void setMode(short mode)
	{
		Material[] mats = flagMarker1.GetComponent<Renderer>().materials;
		this.mode = mode;
		if (mode == 0)
		{
			mats[1] = flagColorRed;
			flagMarker2.SetActive(false);
			area.SetActive(false);
			flagMarker1.GetComponent<BoxCollider>().enabled = true;
			flagMarker1.GetComponent<Renderer>().materials = mats;
		}
		else if (mode == 1)
		{
			mats[1] = flagColorGreen;
			flagMarker2.SetActive(true);
			area.SetActive(true);
			flagMarker1.GetComponent<BoxCollider>().enabled = true;
			flagMarker1.GetComponent<Renderer>().materials = mats;
			flagMarker2.GetComponent<Renderer>().materials = mats;
		}
		else
		{
			mats[1] = flagColorBlue;
			flagMarker2.SetActive(false);
			area.SetActive(true);
			flagMarker1.GetComponent<BoxCollider>().enabled = true;
			flagMarker1.GetComponent<Renderer>().materials = mats;
		}
	}

	/// <summary>
	/// Returns the target of this MarkerTool
	/// </summary>
	/// <returns></returns>
	public GameObject getTarget()
	{
		return target;
	}

	/// <summary>
	/// Sets the target of the MarkerTool
	/// </summary>
	/// <param name="target"></param>
	public void setTarget(GameObject target)
	{
		this.target = target;
	}

	/// <summary>
	/// Sets the temporary target of the MarkerTool
	/// </summary>
	/// <param name="target"></param>
	public void setTempTarget(GameObject target)
	{
		this.tempTarget = target;
	}

	/// <summary>
	/// Returns the GameObject that is preview in the sphere
	/// </summary>
	/// <returns></returns>
	public GameObject getPreviewGameObject()
	{
		return previewGameObject;
	}

	/// <summary>
	/// Sets the GameObject that should be previewed in the sphere
	/// </summary>
	/// <param name="gameObject"></param>
	public void setPreviewGameObject(GameObject gameObject)
	{
		this.previewGameObject = gameObject;
	}

	/// <summary>
	/// Updates the vertices of the area that gets drawn in mode 1 (Area-MarkerTool)
	/// </summary>
	void applyVerticesToArea()
	{
		int localToLocalPositionFactor = 11;
		Vector3 origin = flagMarker1.transform.localPosition * localToLocalPositionFactor;
		Vector3 destination = flagMarker2.transform.localPosition * localToLocalPositionFactor;

		// Flag Corner 1
		verticesClone[0] = origin;
		verticesClone[13] = origin;
		verticesClone[23] = origin;

		// Flag Corner 2
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

		meshFilter.mesh.vertices = verticesClone;
	}

	/// <summary>
	/// Is called when a GameObject collides with the MarkerTool
	/// </summary>
	/// <param name="other"> The collider of the other GameObject </param>
	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.GetComponent<GrabInteractable>() != null && other.transform.root.name != "EventSystemFactory" && tempTarget != other.gameObject)
		{
			Material[] materials;
			if (tempTarget != null && tempTarget != target)
			{
				materials = tempTarget.GetComponent<MeshRenderer>().materials;
				for (int i = 0; i < originalColorsTemp.Count(); i++)
				{
					if (materials[i].name.Equals("Default-Material (Instance)"))
					{
						Destroy(materials[i]);
					}
					else
					{
						materials[i].color = originalColorsTemp[i];
					}
				}
			}

			tempTarget = other.gameObject;
			materials = tempTarget.GetComponent<MeshRenderer>().materials;
			originalColorsTemp = new Color[materials.Count()];
			for (int i = 0; i < materials.Count(); i++)
			{
				if (materials[i] != null && tempTarget != target)
				{
					originalColorsTemp[i] = materials[i].color;
					materials[i].color = new Color(1, 0, 0, 1);

				}
			}
		}

	}

	/// <summary>
	/// This method gets called when used. It is for finally selecting a GameObject that has been selected only temporarely
	/// </summary>
	/// <param name="controller"> The controller that has used the MarkerTool </param>
	public void Use(Hand controller)
	{
		if (tempTarget != target && mode == 2)
		{
			Material[] materials;
			if (target != null)
			{
				materials = target.GetComponent<MeshRenderer>().materials;
				for (int i = 0; i < materials.Count(); i++)
				{
					if (materials[i].name.Equals("Default-Material (Instance)"))
					{
						Destroy(materials[i]);
					}
					else
					{
						materials[i].color = originalColors[i];
					}
				}
			}

			target = tempTarget;
			GameObject label = transform.parent.Find("SelectedObjectLabel").gameObject;
			label.GetComponent<Text>().text = "Selected Object:\n" + target.name;
			if (transform.parent.Find("MarkerTool1") != null)
			{
				transform.parent.Find("MarkerTool1").gameObject.GetComponent<MarkerTool>().setTarget(target);
			}

			materials = target.GetComponent<MeshRenderer>().materials;
			originalColors = new Color[originalColorsTemp.Count()];
			for (int i = 0; i < materials.Count(); i++)
			{
				originalColors[i] = originalColorsTemp[i];
				materials[i].color = new Color(0, 1, 0, 1);

			}

			if (transform.parent.Find("MarkerTool1") != null)
			{
				StartCoroutine(greenblink(4, 0.04f));
			}

		}
	}

	/// <summary>
	/// Called when GameObject is not used anymore
	/// </summary>
	/// <param name="controller"></param>
	public void UnUse(Hand controller)
	{
		//Nothing
	}

	/// <summary>
	/// Called when this GameObject is disabled
	/// </summary>
	private void OnDisable()
	{
		if (mode == 2)
		{
			resetColors();
		}

	}

	/// <summary>
	/// Resets the color of the material of the target
	/// </summary>
	private void resetColors()
	{
		Material[] materials;
		if (target != null)
		{
			materials = target.GetComponent<MeshRenderer>().materials;
			for (int i = 0; i < materials.Count(); i++)
			{
				if (materials[i] != null)
				{
					if (materials[i].name.Equals("Default-Material (Instance)"))
					{
						Destroy(materials[i]);
					}
					else
					{
						materials[i].color = originalColors[i];
					}

				}
			}
		}


		if (tempTarget != null && tempTarget != target)
		{
			materials = tempTarget.GetComponent<MeshRenderer>().materials;
			for (int i = 0; i < originalColorsTemp.Count(); i++)
			{
				if (materials[i].name.Equals("Default-Material (Instance)"))
				{
					Destroy(materials[i]);
				}
				else
				{
					materials[i].color = originalColorsTemp[i];
				}
			}
		}
	}

	/// <summary>
	/// Lets the selected target blink in green when selected
	/// </summary>
	/// <param name="times"> The amount of blinks that should happen </param>
	/// <param name="seconds"> The amount of seconds that pass between each blink </param>
	/// <returns></returns>
	IEnumerator greenblink(int times, float seconds)
	{
		Material[] materials;
		materials = target.GetComponent<MeshRenderer>().materials;
		for (int i = 0; i < times; i++)
		{
			for (int j = 0; j < materials.Count(); j++)
			{
				if (materials[j] != null)
				{
					if (materials[j].name.Equals("Default-Material (Instance)"))
					{
						Destroy(materials[j]);
					}
					materials[j].color = originalColorsTemp[j];
				}

			}
			yield return new WaitForSeconds(seconds);

			for (int j = 0; j < materials.Count(); j++)
			{
				if (materials[j] != null)
				{
					if (materials[j].name.Equals("Default-Material (Instance)"))
					{
						Destroy(materials[j]);
					}
					materials[j].color = new Color(0, 1, 0, 1);
				}
			}
			yield return new WaitForSeconds(seconds);

		}


		resetColors();

	}
}
