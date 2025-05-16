using UnityEngine;
using UnityEngine.Events;
using System;
using Ubiq.Samples;

namespace VaSiLi.Misc
{
    public class LanguageManager : MonoBehaviour
    {

        public GameObject languagePanel;

        private static string LANGUAGE_PLAYER_PREFS = "language";

        public enum Language
        {
            EN,
            DE
        }

        public static UnityAction<Language> languageChanged = delegate { };

        private static Language _selectedLanguage = Language.EN;
        public static Language SelectedLanguage
        {
            get
            {
                return _selectedLanguage;
            }

            set
            {
                PlayerPrefs.SetString(LANGUAGE_PLAYER_PREFS, value.ToString());
                if (value == _selectedLanguage) return;

                _selectedLanguage = value;
                languageChanged.Invoke(value);
            }
        }

        public void SetLanguage(string language)
        {
            if (Enum.TryParse(language, out Language lang))
                SelectedLanguage = lang;
        }

        void Awake()
        {
            string langPref = PlayerPrefs.GetString(LANGUAGE_PLAYER_PREFS);
            Debug.Log(langPref);
            if (langPref.Length > 0 && Enum.TryParse(langPref, out Language language))
            {
                SelectedLanguage = language;
            }
            else
            {
                GetComponentInChildren<PanelSwitcher>().SwitchPanel(languagePanel);
            }
        }
    }
}