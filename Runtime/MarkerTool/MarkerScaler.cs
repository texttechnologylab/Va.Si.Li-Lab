using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Handles the scaling of Object selected by the MarkerTool
/// </summary>
public class MarkerScaler : MonoBehaviour
{
	//type = 0 for bigger scaling, type = 1 for smaller scaling
	[SerializeField] short type = 0;
	private GameObject sphere = null;
	[SerializeField] float scaleFactor = 1.0f;
	[SerializeField] float resetFactor = 1.0f;
	// Start is called before the first frame update
	void Start()
	{
		sphere = transform.parent.gameObject;
	}

	// Calls every frame independently from the fps
	private void FixedUpdate()
	{
		if (transform.localPosition.y != 0)
		{
			transform.localPosition = new Vector3(transform.localPosition.x, 0, transform.localPosition.z);
		}
		if (transform.localPosition.z != 0)
		{
			transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
		}
		if (type == 0)
		{
			if (transform.localPosition.x < -0.015f)
			{
				float scaleSpeed = scaleFactor * (Mathf.Abs(transform.localPosition.x) + -0.015f);
				float resetSpeed = resetFactor * (Mathf.Pow(Mathf.Abs(transform.localPosition.x) + -0.015f, 1.5f) + 0.0001f);
				sphere.transform.localScale += new Vector3(scaleSpeed, scaleSpeed, scaleSpeed);
				transform.localPosition -= new Vector3(-resetSpeed, 0, 0);
			}
			else if (transform.localPosition.x > -0.015f)
			{
				transform.localPosition = new Vector3(-0.015f, 0, 0);
			}
		}
		if (type == 1)
		{
			if (transform.localPosition.x > 0.015f && sphere.transform.localScale.x > 1f)
			{
				float scaleSpeed = scaleFactor * (Mathf.Abs(transform.localPosition.x) + 0.015f);
				float resetSpeed = resetFactor * (Mathf.Pow(Mathf.Abs(transform.localPosition.x), 1.5f) + 0.0001f);
				sphere.transform.localScale -= new Vector3(scaleSpeed, scaleSpeed, scaleSpeed);
				transform.localPosition -= new Vector3(resetSpeed, 0, 0);
			}
			else if (transform.localPosition.x < 0.015f)
			{
				transform.localPosition = new Vector3(0.015f, 0, 0);
			}
		}
	}
}
