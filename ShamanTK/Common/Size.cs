/* 
 * ShamanTK
 * A toolkit for creating multimedia applications.
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

namespace ShamanTK.Common
{
    /// <summary>
    /// Represents the size of a graphics unit (like a window or screen).
    /// </summary>
    public struct Size : IEquatable<Size>
    {
        /// <summary>
        /// Gets an empty <see cref="Size"/> instance.
        /// </summary>
        public static Size Empty { get; } = new Size(0, 0);

        /// <summary>
        /// Gets a value indicating whether the current 
        /// <see cref="Size"/> instance is empty by having either 
        /// a <see cref="Width"/>.
        /// </summary>
        public bool IsEmpty => Width <= 0 || Height <= 0;

        /// <summary>
        /// Gets the aspect ratio between the <see cref="Width"/> and the
        /// <see cref="Height"/>.
        /// </summary>
        public float Ratio => (float)Width / Height;

        /// <summary>
        /// Gets the area of the current <see cref="Size"/> instance.
        /// </summary>
        public int Area => checked(Width * Height);

        /// <summary>
        /// Gets the width of the current <see cref="Size"/> instance.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the height of the current <see cref="Size"/> instance.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Initializes a new <see cref="Size"/> instance.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="width"/> or 
        /// <paramref name="height"/> are less than 0.
        /// </exception>
        public Size (int width, int height)
        {
            if (width < 0) 
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
        }

        /// <summary>
        /// Checks whether the current <see cref="Size"/> instance exceeds
        /// a certain limit.
        /// </summary>
        /// <param name="longestSideLimit">
        /// The maximum value for both <see cref="Width"/> and 
        /// <see cref="Height"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> when the either the current <see cref="Width"/> or
        /// <see cref="Height"/> are greater than the specified
        /// <paramref name="longestSideLimit"/>,
        /// <c>false</c> if both property values are equal to/less than the 
        /// specified <paramref name="longestSideLimit"/>.
        /// </returns>
        public bool Exceeds(int longestSideLimit)
        {
            return Width > longestSideLimit || Height > longestSideLimit;                
        }

        /// <summary>
        /// Checks whether the current <see cref="Size"/> instance exceeds
        /// a certain limit.
        /// </summary>
        /// <param name="limit">
        /// The maximum values for <see cref="Width"/> and 
        /// <see cref="Height"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> when either the current <see cref="Width"/> is 
        /// greater than the <see cref="Width"/> of <paramref name="limit"/> or
        /// when the current <see cref="Height"/> is greater than the
        /// <see cref="Height"/> of <paramref name="limit"/>,
        /// <c>false</c> when both the current <see cref="Width"/> and
        /// <see cref="Height"/> are equal to/less than their equivalents of
        /// the specified <paramref name="limit"/>.
        /// </returns>
        public bool Exceeds(Size limit)
        {
            return Width > limit.Width || Height > limit.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is Size size && Equals(size);
        }

        public bool Equals(Size other)
        {
            return Width == other.Width &&
                   Height == other.Height;
        }

        public override int GetHashCode()
        {
            var hashCode = 859600377;
            hashCode = hashCode * -1521134295 + Width.GetHashCode();
            hashCode = hashCode * -1521134295 + Height.GetHashCode();
            return hashCode;
        }

        public static Size operator * (Size size, int factor)
        {
            return new Size(size.Width * factor, size.Height * factor);
        }

        public static Size operator * (Size size, float factor)
        {
            return new Size((int)(size.Width * factor), 
                (int)(size.Height * factor));
        }

        public static bool operator ==(Size left, Size right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Size left, Size right)
        {
            return !(left == right);
        }
    }
}

