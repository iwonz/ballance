using System.Collections.Generic;
using System.Xml;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* 模块名：     
* I18NProvider.cs
* 
* 用途：
* 国际化支持类，用于加载并处理国际化字符串资源以及读取本地化字符串。
*
* 作者：
* mengyu
*/

namespace Ballance2.Services.I18N
{
    /// <summary>
    /// 国际化字符串提供类
    /// </summary>



    public static class I18NProvider
    {
        private const string TAG = "I18NProvider";

        private static SystemLanguage currentLanguage = SystemLanguage.English;
        private static Dictionary<string, string> LanguageValues = new Dictionary<string, string>();

        //由GameManager调用

        public static void ClearAllLanguageResources()
        {
            currentLanguage = SystemLanguage.English;
            LanguageValues.Clear();
        }

        /// <summary>
        /// 加载语言定义文件
        /// </summary>
        /// <param name="xmlAssets">语言定义XML字符串</param>
        /// <returns>加载是否成功</returns>
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
        /// 预加载语言定义文件
        /// </summary>
        /// <param name="xmlAssets">语言定义XML字符串</param>
        /// <returns>加载是否成功</returns>
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
        /// 加载语言定义文件
        /// </summary>
        /// <param name="xmlAssets">语言定义XML资源文件</param>
        /// <returns>加载是否成功</returns>


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
        /// 设置当前游戏语言。此函数只能设置语言至设置，无法立即生效，必须重启游戏才能生效。
        /// </summary>
        /// <param name="language">语言</param>


        public static void SetCurrentLanguage(SystemLanguage language)
        {
            if (currentLanguage != language)
                currentLanguage = language;
        }
        /// <summary>
        /// 获取当前游戏语言
        /// </summary>
        /// <returns></returns>

        public static SystemLanguage GetCurrentLanguage()
        {
            return currentLanguage;
        }

        /// <summary>
        /// 获取语言字符串
        /// </summary>
        /// <param name="key">字符串键值</param>
        /// <returns>如果找到对应键值字符串，则返回字符串，否则返回null</returns>


        public static string GetLanguageString(string key)
        {
            if (LanguageValues.TryGetValue(key, out var s1))
                return s1;
            return null;
        }
    }
}
