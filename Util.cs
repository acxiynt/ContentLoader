using System.Collections.Generic;
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
        /// Checks if the list of mod have the selected mod.
        /// </summary>
        /// <param name="list">The list of mod to be checked</param>
        /// <param name="info">The info to be found</param>
        /// <returns>Search result, as bool.</returns>
        public static bool Contains(this List<Mod> list, ModInfo info)
        {
            foreach (Mod mod in list)
                if (mod.Info == info)
                    return true;
            return false;
        }
    }
}