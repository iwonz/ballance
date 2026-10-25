using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* CommonUtils.cs
* 
* Purpose:
* General helpers.
*
* Author:
* mengyu
*
*/

namespace Ballance2.Utils
{
  /// <summary>
  /// General utility methods.
  /// </summary>
  public class CommonUtils
  {
    private static System.Random random = new System.Random();
    private static int idPool = 1000;
    private static int idPoolRm = 1000;
    private static int idPoolSq = 0;

    /// <summary>
    /// Generate a random ID.
    /// </summary>
    /// <returns></returns>
    public static int GenRandomID()
    {
      return random.Next(128) * idPoolRm++ / random.Next(idPoolRm, idPoolRm + random.Next(16));
    }
    /// <summary>
    /// Generate an incrementing ID.
    /// </summary>
    /// <returns></returns>
    public static int GenAutoIncrementID()
    {
      if (idPoolSq > 0xffffffe)
        idPoolSq = 0;
      return idPoolSq++;
    }
    /// <summary>
    /// Generate a unique ID.
    /// </summary>
    /// <returns></returns>
    public static int GenNonDuplicateID()
    {
      return idPool++;
    }

    /// <summary>
    /// Generate a random number between min and max.
    /// </summary>
    /// <param name="min">Minimum value.</param>
    /// <param name="max">Maximum value.</param>
    /// <returns></returns>
    public static float RandomFloat(float min, float max)
    {
      return UnityEngine.Random.Range(min, max);
    }
    /// <summary>
    /// Generate a random number between zero and max.
    /// </summary>
    /// <param name="max">Maximum value.</param>
    /// <returns></returns>
    public static float RandomFloat(float max)
    {
      return UnityEngine.Random.Range(0, max);
    }

    /// <summary>
    /// Check whether an array is empty.
    /// </summary>
    /// <param name="arr">Array to check.</param>
    /// <returns>True if the array is null or empty; otherwise false.</returns>
    public static bool IsArrayNullOrEmpty(object[] arr)
    {
      return (arr == null || arr.Length == 0);
    }
    /// <summary>
    /// Check whether a dictionary is empty.
    /// </summary>
    /// <param name="arr">Dictionary to check.</param>
    /// <returns>True if the dictionary is null or empty; otherwise false.</returns>
    public static bool IsDictionaryNullOrEmpty(IDictionary arr)
    {
      return (arr == null || arr.Keys.Count == 0);
    }
    /// <summary>
    /// Create an array containing repeated copies of a string.
    /// </summary>
    /// <param name="val">String value.</param>
    /// <param name="count">Array length.</param>
    /// <returns></returns>
    public static string[] GenSameStringArray(string val, int count)
    {
      string[] arr = new string[count];
      for (int i = 0; i < count; i++)
        arr[i] = val;
      return arr;
    }
    /// <summary>
    /// Validate a variable argument.
    /// </summary>
    /// <param name="param">Variable argument array.</param>
    /// <param name="index">Argument index.</param>
    /// <param name="typeName">Expected type.</param>
    /// <returns>True if the type matches; otherwise false.</returns>
    public static bool CheckParam(object[] param, int index, string typeName)
    {
      if (param.Length <= index)
        return false;

      return param.GetType().Name == typeName;
    }

    /// <summary>
    /// Get dictionary values as strings; potentially expensive.
    /// </summary>
    /// <param name="keyValuePairs">Dictionary</param>
    /// <returns></returns>
    public static string[] GetStringArrayFromDictionary(Dictionary<string, string> keyValuePairs)
    {
      return GetArrayFromDictionary(keyValuePairs);
    }
    /// <summary>
    /// Get dictionary values as an array; potentially expensive.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    /// <param name="keyValuePairs">Dictionary</param>
    /// <returns></returns>
    public static T[] GetArrayFromDictionary<T>(Dictionary<string, T> keyValuePairs)
    {
      Dictionary<string, T>.ValueCollection ts = keyValuePairs.Values;
      return ts.ToList<T>().ToArray();
    }
    /// <summary>
    /// Change only a color's alpha.
    /// </summary>
    /// <param name="o">Original color.</param>
    /// <param name="a">Alpha value.</param>
    /// <returns>New color.</returns>
    public static Color ChangeColorAlpha(Color o, float a)
    {
      return new Color(o.r, o.g, o.b, a);
    }

    /// <summary>
    /// Calculate the distance between two objects.
    /// </summary>
    /// <param name="o1">Object 1.</param>
    /// <param name="o2">Object 2.</param>
    /// <returns></returns>
    public static float DistanceBetweenTwoObjects(GameObject o1, GameObject o2)
    {
      return (o1.transform.position - o2.transform.position).sqrMagnitude;
    }
    /// <summary>
    /// Calculate the distance between two positions.
    /// </summary>
    /// <param name="o1">Position 1.</param>
    /// <param name="o2">Position 2.</param>
    /// <returns></returns>
    public static float DistanceBetweenTwoPos(Vector3 o1, Vector3 o2)
    {
      return (o1 - o2).sqrMagnitude;
    }

    /// <summary>
    /// Check for a null object.
    /// </summary>
    /// <param name="o">Object.</param>
    /// <returns></returns>
    public static bool IsObjectNull(UnityEngine.Object o)
    {
      return o == null;
    }

    /// <summary>
    /// Check whether a Vector3 is valid.
    /// </summary>
    /// <param name="v">Vector.</param>
    /// <returns></returns>
    public static bool IsValid(Vector3 v)
    {
      return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z);
    }
    /// <summary>
    /// Check whether a Vector2 is valid.
    /// </summary>
    /// <param name="v">Vector.</param>
    /// <returns></returns>
    public static bool IsValid(Vector2 v)
    {
      return !float.IsNaN(v.x) && !float.IsNaN(v.y);
    }

    /// <summary>
    /// Check network availability.
    /// </summary>
    public static bool NetAvailable {
      get {
        return Application.internetReachability != NetworkReachability.NotReachable;
      }
    }

    /// <summary>
    /// Check whether the network is wireless.
    /// </summary>
    public static bool IsWifi {
      get {
        return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
      }
    }
  }
}
