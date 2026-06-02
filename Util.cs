using System;
using System.Collections.Generic;
using System.Linq;
namespace Genesis.ContentLoader
{
    /// <summary>
    /// Random utility extension for frequently used storage types.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Checks if the list is null or empty.
        /// </summary>
        /// <param name="list">The list to be checked.</param>
        /// <returns>Comparation result, as bool.</returns>
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }
        /// <summary>
        /// Checks if the string is null or empty.
        /// </summary>
        /// <param name="str">The string to be checked.</param>
        /// <returns>Comparation result, as bool.</returns>
        public static bool IsNullOrEmpty(this string str)
        {
            return str == null || str.Length == 0;
        }
        /// <summary>
        /// Checks if the list of mod have the selected mod as string id.
        /// </summary>
        /// <param name="list">The list of mod to be checked</param>
        /// <param name="id">The mod id to be found</param>
        /// <returns>Search result, as bool.</returns>
        public static bool Contains(this List<Mod> list, string id)
        {
            foreach (Mod mod in list)
                if (mod.Info.ModID == id)
                    return true;
            return false;
        }

        /// <summary>
        /// Adds the specific kvp into dictionary.
        /// </summary>
        /// <param name="dict">Dictionary to add the pair</param>
        /// <param name="pair">The pair to be added</param>
        public static void Add<TKey, TValue>(this Dictionary<TKey, TValue> dict, KeyValuePair<TKey, TValue> pair)
        {
            dict.Add(pair.Key, pair.Value);
        }
        /// <summary>
        /// Removes the list of keys out of the dictionary.
        /// </summary>
        /// <typeparam name="TKey">Key type of the dictionary and IEnumerable.</typeparam>
        /// <typeparam name="TValue">Unused.</typeparam>
        /// <param name="dict">Dictionary to be removed of the keys.</param>
        /// <param name="keys">List of the key to be removed.</param>
        public static void Remove<TKey, TValue>(this Dictionary<TKey, TValue> dict, IEnumerable<TKey> keys)
        {
            if (keys is List<TKey>)
            {
                List<TKey> _keys = (List<TKey>)keys;
                for (int i = 0; i < _keys.Count; i++)
                    dict.Remove(_keys[i]);
                return;
            }
            foreach (TKey key in keys)
                dict.Remove(key);
        }
    }
}