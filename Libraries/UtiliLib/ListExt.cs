/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/ListExt.cs
 * Purpose: Library component: ListExt.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

#nullable disable
namespace UtiliLib
{
    /// <summary>
    /// Provides extension methods for working with lists
    /// </summary>
    public static class ListExt
    {
        private static readonly Random Rng = new Random();

        /// <summary>
        /// Shuffles the elements of a list in place using the Fisher-Yates algorithm.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list</typeparam>
        /// <param name="list">The list to shuffle</param>
        public static void Shuffle<T>(this IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = Rng.Next(i + 1); // random index from 0 to i
                // Swap via deconstruction
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}