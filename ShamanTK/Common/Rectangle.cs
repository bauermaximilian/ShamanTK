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
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ShamanTK.Common
{
    /// <summary>
    /// A four-dimensional structure which defines a rectangle through a
    /// position and its size in two-dimensional space.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [InterpolatorProviderAttribute("Animator")]
    public readonly struct Rectangle
    {
        /// <summary>
        /// Gets the <see cref="IInterpolator{T}"/> for the current type.
        /// </summary>
        public static IInterpolator<Rectangle> Animator { get; }
            = new ValueAnimator();

        private class ValueAnimator : IInterpolator<Rectangle>
        {
            public ref readonly Rectangle Default => ref Zero;

            public Rectangle AnimateDynamicSmooth(in Rectangle x, 
                in Rectangle y, ref Rectangle currentAccerlation, 
                TimeSpan delta, float friction)
            {
                throw new NotSupportedException();
            }

            public Rectangle InterpolateLinear(in Rectangle x, in Rectangle y,
                float ratio)
            {
                return Interpolate(x, y, ratio);
            }

            public Rectangle InterpolateCubic(in Rectangle beforeX, 
                in Rectangle x, in Rectangle y, in Rectangle afterY, 
                float ratio)
            {
                return Interpolate(beforeX, x, y, afterY, ratio);
            }
        }

        /// <summary>
        /// Gets a rectangle with zero height and with at position 0/0.
        /// </summary>
        public static ref readonly Rectangle Zero => ref zero;
        private static readonly Rectangle zero = new Rectangle(0, 0, 0, 0);

        /// <summary>
        /// Gets a 1x1-sized rectangle at position 0/0.
        /// </summary>
        public static ref readonly Rectangle One => ref one;
        private static readonly Rectangle one = new Rectangle(0, 0, 1, 1);

        /// <summary>
        /// Defines the character which is used to separate the components
        /// in the string representation of instances of this struct.
        /// </summary>
        public const char ComponentSeparator = ';';

        /// <summary>
        /// Defines the amount of components the string representation of
        /// the vertex must have to be valid.
        /// </summary>
        private const int ComponentCount = 2;

        /// <summary>
        /// Gets the size of the current rectangle.
        /// </summary>
        public Vector2 Size => new Vector2(Width, Height);

        /// <summary>
        /// Gets the X position of the bottom left point 
        /// of the rectangle.
        /// </summary>
        public float X { get; }

        /// <summary>
        /// Gets the Y position of the bottom left point 
        /// of the rectangle.
        /// </summary>
        public float Y { get; }

        /// <summary>
        /// Gets the X position of the right border of the rectangle.
        /// </summary>
        public float Right => X + Width;

        /// <summary>
        /// Gets the X position of the left border of the rectangle.
        /// </summary>
        public float Left => X;

        /// <summary>
        /// Gets the width of the rectangle.
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Gets the height of the rectangle.
        /// </summary>
        public float Height { get; }

        /// <summary>
        /// Gets the Y position of the upper border of the rectangle.
        /// </summary>
        public float Top => Y + Height;

        /// <summary>
        /// Gets the Y position of the lower border of the rectangle.
        /// </summary>
        public float Bottom => Y;

        /// <summary>
        /// Gets the position of the top-left corner of the rectangle.
        /// </summary>
        public Vector2 TopLeft => new Vector2(X, Top);

        /// <summary>
        /// Gets the position of the top-left corner of the rectangle.
        /// </summary>
        public Vector2 TopRight => new Vector2(Right, Top);

        /// <summary>
        /// Gets the position of the bottom-right corner of the rectangle.
        /// </summary>
        public Vector2 BottomRight => new Vector2(Right, Y);

        /// <summary>
        /// Gets the position of the bottom-left corner of the rectancle,
        /// which is equivalent to the <see cref="X"/> and <see cref="Y"/>
        /// coordinates.
        /// </summary>
        public Vector2 BottomLeft => new Vector2(X, Y);

        /// <summary>
        /// Gets the position of the center of the rectangle.
        /// </summary>
        public Vector2 Center => new Vector2(X + Width / 2, Y + Height / 2);

        /// <summary>
        /// Gets the area the rectangle occupies.
        /// </summary>
        public float Area => Height * Width;

        /// <summary>
        /// Gets a value which indicates whether the current 
        /// <see cref="Rectangle"/> instance has an <see cref="Area"/> of 0
        /// (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsEmpty => Width == 0 || Height == 0;

        /// <summary>
        /// Creates a <see cref="Rectangle"/> with the bottom left coner at
        /// <see cref="Vector2.Zero"/>.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public Rectangle(float width, float height)
            : this()
        {
            X = 0;
            Y = 0;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Creates a <see cref="Rectangle"/>.
        /// </summary>
        /// <param name="position">
        /// The position of the bottom left corner.
        /// </param>
        /// <param name="size">
        /// The size as <see cref="Vector2"/>, where the X value defines the 
        /// <see cref="Width"/> and the Y value defines 
        /// the <see cref="Height"/>.
        /// </param>
        public Rectangle(Vector2 position, Vector2 size)
            : this()
        {
            X = position.X;
            Y = position.Y;
            Width = size.X;
            Height = size.Y;
        }

        /// <summary>
        /// Creates a <see cref="Rectangle"/>.
        /// </summary>
        /// <param name="x">The X position of the bottom-left corner.</param>
        /// <param name="y">The Y position of the bottom-left corner.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public Rectangle(float x, float y, float width, float height)
            : this()
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Creates a new <see cref="Rectangle"/> which is moved by
        /// a specified amount of units.
        /// </summary>
        /// <param name="x">The movement in the X direction.</param>
        /// <param name="y">The movement in the Y direction.</param>
        /// <returns>A new <see cref="Rectangle"/> instance.</returns>
        public Rectangle Move(float x, float y)
        {
            return new Rectangle(X + x, Y + y, Width, Height);
        }

        /// <summary>
        /// Checks if a rectangle is completely contained in the current
        /// rectangle.
        /// </summary>
        /// <param name="otherRectangle">
        /// The other rectangle.
        /// </param>
        /// <param name="ignorePosition">
        /// <c>true</c> to only check if the <paramref name="otherRectangle"/>
        /// fits into the current rectangle size-wise, <c>false</c> to
        /// check if the <paramref name="otherRectangle"/> is actually 
        /// located inside the current rectangle too.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <paramref name="otherRectangle"/> is contained
        /// in the current rectangle (size-wise or completely, depending on 
        /// <paramref name="ignorePosition"/>) or not <c>false</c>.
        /// </returns>
        public bool Contains(Rectangle otherRectangle,
            bool ignorePosition)
        {
            if (ignorePosition) return otherRectangle.Width <= Width &&
                otherRectangle.Height <= Height;
            else return
                otherRectangle.X >= X && otherRectangle.Right <= Right &&
                otherRectangle.Y >= Y && otherRectangle.Bottom <= Bottom;
        }

        /// <summary>
        /// Generates a <see cref="Rectangle"/> string, which consists of 
        /// the four vector components separated by the character defined 
        /// in <see cref="ComponentSeparator"/>. The invariant culture is 
        /// used to convert the numbers to strings.
        /// </summary>
        /// <returns>A new string.</returns>
        public override string ToString()
        {
            CultureInfo c = CultureInfo.InvariantCulture;

            using (StringWriter vw = new StringWriter())
            {
                vw.Write(X.ToString(c));
                vw.Write(ComponentSeparator);
                vw.Write(Y.ToString(c));
                vw.Write(ComponentSeparator);
                vw.Write(Width.ToString(c));
                vw.Write(ComponentSeparator);
                vw.Write(Height.ToString(c));
                return vw.ToString();
            }
        }

        /// <summary>
        /// Parses a <see cref="Rectangle"/> from a string in the format 
        /// generated by <see cref="ToString"/>. The invariant culture is 
        /// used for parsing the contained numbers.
        /// </summary>
        /// <param name="str">The rectangle string.</param>
        /// <returns>A new <see cref="Rectangle"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="str"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="str"/> has an invalid format.
        /// </exception>
        public static Rectangle Parse(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));

            CultureInfo c = CultureInfo.InvariantCulture;

            string[] elements = str.Split(ComponentSeparator);
            if (elements.Length != ComponentCount)
                throw new ArgumentException("The specified string had an " +
                    "invalid amount of components!");
            else
            {
                try
                {
                    return new Rectangle(float.Parse(elements[0], c),
                        float.Parse(elements[1], c),
                        float.Parse(elements[2], c),
                        float.Parse(elements[3], c));
                }
                catch
                {
                    throw new ArgumentException("The specified string had " +
                        "invalid component values!");
                }
            }
        }

        /// <summary>
        /// Calculates a smooth interpolation of two values.
        /// </summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="ratio">
        /// The mixing ratio of the two main values.
        /// </param>
        /// <returns>A new <see cref="Vector3"/> instance.</returns>
        public static Rectangle Interpolate(in Rectangle x,
                in Rectangle y, float ratio)
        {
            return new Rectangle(x.BottomLeft + ratio
                * (y.BottomLeft - x.BottomLeft),
                x.Size + ratio * (y.Size - x.Size));
        }

        /// <summary>
        /// Calculates a smooth interpolation of two values.
        /// </summary>
        /// <param name="beforeX">The value before the first value.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="afterY">The value after the second value.</param>
        /// <param name="ratio">
        /// The mixing ratio of the two main values.
        /// </param>
        /// <returns>A new <see cref="Vector3"/> instance.</returns>
        public static Rectangle Interpolate(
            in Rectangle beforeX, in Rectangle x, in Rectangle y,
            in Rectangle afterY, float ratio)
        {
            return new Rectangle(
                (((beforeX.BottomLeft + x.BottomLeft) - (afterY.BottomLeft
                + y.BottomLeft)) * (float)Math.Pow(ratio, 3) +
                ((beforeX.BottomLeft + x.BottomLeft * 2)
                - (afterY.BottomLeft + y.BottomLeft * 2))
                * (float)Math.Pow(ratio, 2) + x.BottomLeft),
                (((beforeX.Size + x.Size) - (afterY.Size
                + y.Size)) * (float)Math.Pow(ratio, 3) +
                ((beforeX.Size + x.Size * 2)
                - (afterY.Size + y.Size * 2))
                * (float)Math.Pow(ratio, 2) + x.Size));
        }
    }
}
