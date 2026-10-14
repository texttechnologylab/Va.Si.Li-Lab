using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This script handles the automatic resizing of GameObjects in the preview sphere
/// </summary>
public class SphereResizer : MonoBehaviour
{
	/// <summary>
	/// Automaticially shrinks the target GameObject for fitting it into the preview sphere
	/// </summary>
	/// <param name="other"> The collider of the GameObject that should be resized </param>
	private void OnTriggerStay(Collider other)
	{
		if (!(GetComponent<Collider>().bounds.Contains(other.bounds.max) && GetComponent<Collider>().bounds.Contains(other.bounds.min)))
		{
			if (transform.parent.parent.GetComponent<MarkerTool>().getPreviewGameObject() == other.gameObject)
			{
				if (other.transform.localScale.x >= 0.001 && other.transform.localScale.y >= 0.001 && other.transform.localScale.z >= 0.001)
					other.transform.localScale -= new Vector3(0.007f, 0.007f, 0.007f);
				else
				{
					other.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
				}
			}
		}

	}
}

