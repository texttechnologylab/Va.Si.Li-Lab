using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
Modifier_Handler

@date 24.01.2026

@author Nick Korjakin
@version 1.0

This class handles the modifiers
*/
public class Modefier_Handler : MonoBehaviour
{
    public static Modefier_Handler Instance { get; private set; }

    [Header("Scene Settings")]
    [SerializeField] private ModifierSettings sceneSettings = new ModifierSettings();
    [SerializeField] private Modifier_Container _modifierContainer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyAllModifiers();
            return;
        }

        if (Instance != this)
        {
            Instance.OverwriteSettingsFrom(this);
            Instance.ApplyAllModifiers();
            Destroy(gameObject);
        }
    }

    /**
    * Copies Settings from modifier Handler
    */
    private void OverwriteSettingsFrom(Modefier_Handler sceneHandler)
    {
        this.sceneSettings = sceneHandler.sceneSettings != null
            ? sceneHandler.sceneSettings.Clone()
            : new ModifierSettings();

        Debug.Log($"[Modifier_Handler] Scene-Settings übernommen aus Scene: {sceneHandler.gameObject.scene.name}");
    }

    /**
    * all modifiers from Container will be applied
    */
    public void ApplyAllModifiers()
    {
        foreach (ModifierEntry mod in _modifierContainer.entries)
        {
            IModifier modifier = mod.target.GetComponent<IModifier>();
            if (sceneSettings.IsAllowed(modifier.getName()) ){//&& modifier.IsActive()) {
                modifier.Activate();
                _modifierContainer.SetModifierState(mod.target, true);
                Debug.Log("Activate: " + modifier.getName());
            }
            else{
                modifier.Deactivate();
                _modifierContainer.SetModifierState(mod.target, false);
                Debug.Log("Deactivate: " + modifier.getName());
             }
        }

    }

    public ModifierSettings ActiveSettings => sceneSettings;

    [System.Serializable]

    /*
    ModifierSettings

    @date 26.01.2026

    @author Nick Korjakin
    @version 1.0

    This class handles the modifier settings
    */
    public class ModifierSettings
    {
        [Header("Global toggles")]
        public bool modifiersEnabled = true;

        [Header("Allowed modifiers in this scene")]
        public List<ModifierRule> rules = new List<ModifierRule>();

        /**
        * is a modifier allowed in the settings
        * @return true or false
        */
        public bool IsAllowed(string id)
        {
            if (!modifiersEnabled) return false;

            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i].id == id)
                    return rules[i].allowed;
            }

            return false;
        }

        /**
        * clamps a value
        * @return clamped value
        */
        public float ClampValue(string id, float value)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i].id == id)
                    return Mathf.Clamp(value, rules[i].minValue, rules[i].maxValue);
            }
            return value;
        }

        /**
        * checks if a modifier is allowed and adds it to rules
        * @return copy of rules list
        */
        public ModifierSettings Clone()
        {
            var copy = new ModifierSettings
            {
                modifiersEnabled = this.modifiersEnabled,
                rules = new List<ModifierRule>(this.rules.Count)
            };

            for (int i = 0; i < this.rules.Count; i++)
                copy.rules.Add(this.rules[i]);

            return copy;
        }
    }

    [System.Serializable]
    /**
    * is a modifier allowed or not
    */
    public struct ModifierRule
    {
        public string id;
        public bool allowed;

        [Tooltip("Optional: min/max fürs Clamping (falls du Values hast)")]
        public float minValue;
        public float maxValue;
    }
}