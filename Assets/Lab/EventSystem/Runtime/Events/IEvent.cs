using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Defines methods needed for an event to work
/// </summary>
public interface IEvent
{
	/// <summary>
	/// Triggers an event when all requirments are met
	/// </summary>
	void trigger();
	/// <summary>
	/// Shows a preview of the event once it triggers
	/// </summary>
	void previewOn();
	/// <summary>
	/// Disables the preview once it has been enabled
	/// </summary>
	void previewOff();
	/// <summary>
	/// Deletes the event
	/// </summary>
	void delete();
	/// <summary>
	/// Creates JSON representation of the event
	/// </summary>
	/// <param name="groupID"></param>
	/// <returns></returns>
	string createJson(int groupID);
}
