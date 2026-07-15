using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Defines methods needed for a trigger to work
/// </summary>
public interface ITrigger
{
	/// <summary>
	/// Notifies the EventSystem that this trigger has triggered and other triggers should be check
	/// </summary>
	void notifySystem();

	/// <summary>
	/// Gets completion state of this trigger
	/// </summary>
	/// <returns></returns>
	bool getState();
	/// <summary>
	/// Resets this trigger if the EventSystem is repeatable
	/// </summary>
	void resetTrigger();
	/// <summary>
	/// Sets the corrosponding EventSystem
	/// </summary>
	/// <param name="es"></param>
	void setEventSystem(EventSystem es);
	/// <summary>
	/// Shows a preview of the trigger
	/// </summary>
	void previewOn();
	/// <summary>
	/// Disables the preview once it has been enabled
	/// </summary>
	void previewOff();
	/// <summary>
	/// Deletes the trigger
	/// </summary>
	void delete();
	/// <summary>
	/// Creates JSON representation of the trigger
	/// </summary>
	/// <param name="groupID"></param>
	/// <returns></returns>
	string createJson(int groupID);
}

