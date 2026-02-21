/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/DictionaryExtensions.cs
 * Purpose: Library component: DictionaryExtensions.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Collections.Concurrent;

namespace UtiliLib
{
    /// <summary>
    /// Extension utilities for working with dictionaries
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Creates a deep clone of a ConcurrentDictionary.
        /// Both keys and values must implement <see cref="ICloneable"/>.
        /// </summary>
        /// <typeparam name="T">Type of dictionary keys, must implement ICloneable</typeparam>
        /// <typeparam name="U">Type of dictionary values, must implement ICloneable</typeparam>
        /// <param name="source">The ConcurrentDictionary to clone</param>
        /// <returns>A new ConcurrentDictionary with cloned keys and values</returns>
        public static ConcurrentDictionary<T, U> CloneDictionary<T, U>(this ConcurrentDictionary<T, U> source)
            where T : ICloneable
            where U : ICloneable
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var clone = new ConcurrentDictionary<T, U>();

            foreach (var kvp in source)
            {
                T keyClone = (T)kvp.Key.Clone();
                U valueClone = (U)kvp.Value.Clone();

                clone[keyClone] = valueClone;
            }

            return clone;
        }
    }
}