/* 
 * Eterra Framework
 * A simple framework for creating multimedia applications.
 * Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;

namespace Eterra.Common
{
    /// <summary>
    /// Provides extension methods for <see cref="string"/> instances.
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// Defines the string which is appended when a string is truncated
        /// with <see cref="Truncate(string, int, bool)"/>.
        /// </summary>
        public const string TruncationHint = "...";

        /// <summary>
        /// Gets the minimum length of a string clamped with the 
        /// <see cref="Clamp(string, int)"/> method.
        /// </summary>
        public static int MinimumClampedLength { get; } 
            = TruncationHint.Length + 2;

        /// <summary>
        /// Gets the default length of a string which is clamped with the
        /// <see cref="Clamp(string)"/> method.
        /// </summary>
        public static int DefaultClampedLength { get; } = 15;

        /// <summary>
        /// Makes sure that a string does not exceed a certain length.
        /// If a string is longer, it's shortened to the specified amount
        /// of characters by removing the middle part and replacing it with
        /// the <see cref="TruncationHint"/>.
        /// </summary>
        /// <param name="str">The string to be shortened</param>
        /// <returns>A new <see cref="string"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="str"/> is null.
        /// </exception>
        public static string Clamp(this string str)
        {
            return Clamp(str, DefaultClampedLength);
        }

        /// <summary>
        /// Makes sure that a string does not exceed a certain length.
        /// If a string is longer, it's shortened to the specified amount
        /// of characters by removing the middle part and replacing it with
        /// the <see cref="TruncationHint"/>.
        /// </summary>
        /// <param name="str">The string to be shortened</param>
        /// <param name="maxLength">
        /// The desired maximum length of the resulting string.
        /// Must be greater than/equal to <see cref="MinimumClampedLength"/>.
        /// </param>
        /// <returns>A new <see cref="string"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="str"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="maxLength"/> is less than
        /// <see cref="MinimumClampedLength"/>.
        /// </exception>
        public static string Clamp(this string str, int maxLength)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));
            if (maxLength < TruncationHint.Length + 2)
                throw new ArgumentOutOfRangeException(nameof(maxLength));

            str = str.Trim();

            if (str.Length > maxLength)
            {
                int prefixLength = (str.Length - TruncationHint.Length) / 2;
                int suffixLength = str.Length - TruncationHint.Length
                    - prefixLength;
                int suffixIndex = str.Length - suffixLength;

                return str.Substring(0, prefixLength) + TruncationHint +
                    str.Substring(suffixIndex, suffixLength);
            }
            else return str;
        }

        /*
        /// <summary>
        /// Truncates a string to a maximum length.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <param name="maxLength">
        /// The maximum length (excluding the length of the optional truncation
        /// hint defined in <see cref="TruncationHint"/>).
        /// </param>
        /// <param name="appendTruncationHint">
        /// <c>true</c> to append the <see cref="TruncationHint"/> to the 
        /// truncated string if the string was truncated, <c>false</c> to just
        /// return the truncated string.
        /// </param>
        /// <returns>
        /// A new <see cref="string"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="str"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="maxLength"/> is 
        /// less than/equal to 0.
        /// </exception>
        public static string Truncate(this string str, int maxLength, 
            bool appendTruncationHint = true)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));
            if (maxLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength));

            str = str.Trim();
            int truncatedLength = Math.Min(str.Length, maxLength);
            string truncatedStr = str.Substring(0, truncatedLength);

            if (truncatedLength != str.Length && appendTruncationHint)
                truncatedStr += TruncationHint;

            return truncatedStr;
        }*/
    }
}
