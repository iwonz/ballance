using System.Collections.Generic;
using System.Xml;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* I18NProvider.cs
* 
* Purpose:
* Load and resolve localized language resources.
*
* Author:
* mengyu
*/

namespace Ballance2.Services.I18N
{
    /// <summary>
    /// Language resource provider.
    /// </summary>



    public static class I18NProvider
    {
        private const string TAG = "I18NProvider";

        private static SystemLanguage currentLanguage = SystemLanguage.English;
        private static Dictionary<string, string> LanguageValues = new Dictionary<string, string>();

        // Called by GameManager.

        public static void ClearAllLanguageResources()
        {
            currentLanguage = SystemLanguage.English;
            LanguageValues.Clear();
        }

        /// <summary>
        /// Load a language definition.
        /// </summary>
        /// <param name="xmlAssets">Language definition XML string.</param>
        /// <returns>Whether loading succeeded.</returns>
        public static bool LoadLanguageResources(string xmlAssets)
        {
            try
            {
                ReadLanguageResources(xmlAssets, LanguageValues);
                return true;
            }
            catch (XmlException e)
            {
                Log.E(TAG, "LoadLanguageResources failed: " + e.ToString());
            }
            return false;
        }

        /// <summary>
        /// Preload a language definition.
        /// </summary>
        /// <param name="xmlAssets">Language definition XML string.</param>
        /// <returns>Whether loading succeeded.</returns>
        public static Dictionary<string, string> PreLoadLanguageResources(string xmlAssets)
        {
            Dictionary<string, string> LanguageValues = new Dictionary<string, string>();
            try
            {
                ReadLanguageResources(xmlAssets, LanguageValues);
                return LanguageValues;
            }
            catch (XmlException e)
            {
                Log.E(TAG, "LoadLanguageResources failed: " + e.ToString());
            }
            return null;
        }

        /// <summary>
        /// Load a language definition.
        /// </summary>
        /// <param name="xmlAssets">Language definition TextAsset.</param>
        /// <returns>Whether loading succeeded.</returns>


        // Merge English first so partially translated third-party mods also have
        // a per-key fallback. Select only Text elements (XML comments are valid).
        private static void ReadLanguageResources(string xml, Dictionary<string, string> values)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var root = doc.DocumentElement;
            ReadLanguage(root.SelectSingleNode("./Language[@name='English']"), values);
            if (currentLanguage != SystemLanguage.English)
                ReadLanguage(root.SelectSingleNode($"./Language[@name='{currentLanguage}']"), values);
        }

        private static void ReadLanguage(XmlNode language, Dictionary<string, string> values)
        {
            if (language == null) return;
            foreach (XmlElement text in language.SelectNodes("Text"))
            {
                var key = text.GetAttribute("name");
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(text.InnerXml))
                    values[key] = text.InnerXml;
            }
        }

        public static bool LoadLanguageResources(TextAsset xmlAssets)
        {
            return LoadLanguageResources(xmlAssets.text);
        }

        /// <summary>
        /// Select the language for subsequent resource loads; existing resources are not reloaded by this method.
        /// </summary>
        /// <param name="language">Language.</param>


        public static void SetCurrentLanguage(SystemLanguage language)
        {
            if (currentLanguage != language)
                currentLanguage = language;
        }
        /// <summary>
        /// Get the current game language.
        /// </summary>
        /// <returns></returns>

        public static SystemLanguage GetCurrentLanguage()
        {
            return currentLanguage;
        }

        /// <summary>
        /// Get a language string.
        /// </summary>
        /// <param name="key">String key.</param>
        /// <returns>The string for the key, or null if missing.</returns>


        public static string GetLanguageString(string key)
        {
            if (LanguageValues.TryGetValue(key, out var s1))
                return s1;
            return null;
        }
    }
}
