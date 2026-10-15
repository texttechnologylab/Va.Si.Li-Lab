using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class ModifierEntry
{
    public GameObject target;
    public bool value;
}
/*
Modifier_Container

@date 01.03.2026

@author Nick Korjakin
@author Elias Morello
@version 1.0

All Objects within the rules are in this class. This class connects the modifiers with the players.
*/
public class Modifier_Container : MonoBehaviour
{
    [SerializeField]
    public List<ModifierEntry> entries;

    private readonly Dictionary<GameObject, bool> _lastValues = new();

    private readonly Dictionary<GameObject, IModifier> _modifierCache = new();

    [AgentTool(
        "show_position",
        "Call when the player wants to know where he is."
    )]
    public string ShowPosition() {
        SetModifierState("Modifier_Map", true);
        return "Bitte schön! Jetzt weißt du, wo du bist!";
    }

    private void Start()
    {
        RefreshCachesAndLastValues();
        ApplyAllStates();
    }

    private void Update()
    {
        RefreshCachesAndLastValues();

        CheckForBoolChangesAndToggle();
    }

    /**
    * Check if the modifier is true or false. It checks if the bool was changed.
    */
    private void RefreshCachesAndLastValues()
    {
        var currentTargets = new HashSet<GameObject>();
        foreach (var e in entries)
        {
            if (e == null || e.target == null) continue;
            currentTargets.Add(e.target);

            if (!_lastValues.ContainsKey(e.target))
                _lastValues[e.target] = e.value;

            if (!_modifierCache.TryGetValue(e.target, out var mod) || mod == null)
            {
                mod = e.target.GetComponent<IModifier>();
                if (mod != null)
                    _modifierCache[e.target] = mod;
                else
                    _modifierCache.Remove(e.target);
            }
        }

        RemoveMissingKeys(_lastValues, currentTargets);
        RemoveMissingKeys(_modifierCache, currentTargets);
    }

    /**
    * Removes missing keys
    */
    private static void RemoveMissingKeys<T>(Dictionary<GameObject, T> dict, HashSet<GameObject> currentTargets)
    {
        var toRemove = new List<GameObject>();
        foreach (var key in dict.Keys)
        {
            if (key == null || !currentTargets.Contains(key))
                toRemove.Add(key);
        }
        foreach (var key in toRemove)
            dict.Remove(key);
    }

    /**
    * Modifier gets activated or deactivated based on the rules
    */
    private void ApplyAllStates()
    {
        foreach (var e in entries)
        {
            if (e == null || e.target == null) continue;

            if (TryGetModifier(e.target, out var modifier))
            {
                if (e.value) modifier.Activate();
                else modifier.Deactivate();
            }

            _lastValues[e.target] = e.value;
        }
    }

    /**
    * Checks bool changes and toggles
    */
    private void CheckForBoolChangesAndToggle()
    {
        foreach (var e in entries)
        {
            if (e == null || e.target == null) continue;

            if (!_lastValues.TryGetValue(e.target, out var last))
            {
                _lastValues[e.target] = e.value;
                continue;
            }

            if (e.value != last)
            {
                if (TryGetModifier(e.target, out var modifier))
                {
                    //modifier.Toggle(null, 0);
                    if(e.value) modifier.Activate(); else modifier.Deactivate();
                }
                else
                {
                    Debug.LogWarning($"[{name}] {e.target.name} hat keinen Component mit IModifier.", e.target);
                }

                _lastValues[e.target] = e.value;
            }
        }
    }

    /**
    * 
    */
    private bool TryGetModifier(GameObject target, out IModifier modifier)
    {
        modifier = null;
        if (target == null) return false;

        if (_modifierCache.TryGetValue(target, out modifier) && modifier != null)
            return true;

        modifier = target.GetComponent<IModifier>();
        if (modifier != null)
        {
            _modifierCache[target] = modifier;
            return true;
        }

        return false;
    }

    /**
    * @return if a modifier is enabled or diabled
    */
    public bool GetModifierStatus(string modifier)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].target.GetComponent<IModifier>().getName() == modifier)
            {
                return entries[i].target.GetComponent<IModifier>().IsActive();
            }
        }

        return false;
    }

    /**
    * enables or disables a modifier
    */
    public void SetModifierState(GameObject target, bool newValue)
    {
        if (target == null) return;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].target == target)
            {
                entries[i].value = newValue;
                return;
            }
        }

        entries.Add(new ModifierEntry { target = target, value = newValue });
        if (TryGetModifier(target, out var modifier))
        {
            modifier.Activate();
        }
    }

    /**
    * Gives the modifier new values
    */
    public void SetModifierState(string modifier_name, bool newValue)
    {

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].target.GetComponent<IModifier>().getName() == modifier_name)
            {
                entries[i].value = newValue;
                return;
            }
        }
    }

    /**
    * removes a value from modifier container
    */
    public void RemoveValue(GameObject target)
    {
        if (target == null) return;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].target == target)
            {
                if (TryGetModifier(target, out var modifier))
                {
                    modifier.Deactivate();
                }
                entries.RemoveAt(i);
                return;
            }
        }
    }
}
