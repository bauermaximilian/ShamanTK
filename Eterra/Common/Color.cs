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
using System.Globalization;
using System.Runtime.InteropServices;

namespace Eterra.Common
{
    /// <summary>
    /// Defines a RGBA color with 8 bit per color component and an 
    /// alpha channel in the default byte order.
    /// Contains definitions for color structures with another component 
    /// layout or depth, which can be explicitely converted to (and from)
    /// this <see cref="Color"/> structure (except <see cref="Color.RGB32"/>).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Color : IEquatable<Color>
    {
        #region Color structure definitions with alternative layout/depth
        /// <summary>
        /// Defines a RGB color with 8 bit per color component.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct RGB
        {
            /// <summary>
            /// Gets the size of a <see cref="RGB"/> instance in bytes.
            /// </summary>
            public static int Size { get; } = Marshal.SizeOf(typeof(RGB));

            /// <summary>
            /// Gets the red color component.
            /// </summary>
            public byte R { get; }

            /// <summary>
            /// Gets the green color component.
            /// </summary>
            public byte G { get; }

            /// <summary>
            /// Gets the blue color component.
            /// </summary>
            public byte B { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Color.RGB"/> 
            /// struct.
            /// </summary>
            /// <param name="r">The red color component.</param>
            /// <param name="g">The green color component.</param>
            /// <param name="b">The blue color component.</param>
            public RGB(byte r, byte g, byte b)
            {
                R = r;
                G = g;
                B = b;
            }

            /// <summary>
            /// Explicitely converts a <see cref="Color"/> instance to a
            /// <see cref="Color.RGB"/> instance.
            /// </summary>
            /// <param name="color">
            /// The <see cref="Color"/> instance.
            /// </param>
            public static explicit operator RGB(Color color)
            {
                return new RGB(color.R, color.G, color.B);
            }
        }

        /// <summary>
        /// Defines a ARGB color with 8 bit per color component and an 
        /// alpha channel.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ARGB
        {
            /// <summary>
            /// Gets the size of a <see cref="ARGB"/> instance in bytes.
            /// </summary>
            public static int Size { get; } = Marshal.SizeOf(typeof(ARGB));

            /// <summary>
            /// Gets the alpha component (100% opacity is 255, 0% opacity is 0).
            /// </summary>
            public byte Alpha { get; }

            /// <summary>
            /// Gets the red color component.
            /// </summary>
            public byte R { get; }

            /// <summary>
            /// Gets the green color component.
            /// </summary>
            public byte G { get; }

            /// <summary>
            /// Gets the blue color component.
            /// </summary>
            public byte B { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Color.ARGB"/> 
            /// struct.
            /// </summary>
            /// <param name="alpha">
            /// The alpha color component (100% opacity is 255, 
            /// 0% opacity is 0).
            /// </param>
            /// <param name="r">The red color component.</param>
            /// <param name="g">The green color component.</param>
            /// <param name="b">The blue color component.</param>
            public ARGB(byte alpha, byte r, byte g, byte b)
            {
                Alpha = alpha;
                R = r;
                G = g;
                B = b;
            }

            /// <summary>
            /// Explicitely converts a <see cref="Color"/> instance to a
            /// <see cref="Color.ARGB"/> instance.
            /// </summary>
            /// <param name="color">
            /// The <see cref="Color"/> instance.
            /// </param>
            public static explicit operator ARGB(Color color)
            {
                return new ARGB(color.Alpha, color.R, color.G, color.B);
            }
        }

        /// <summary>
        /// Defines a BGR color with 8 bit per color component.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct BGR
        {
            /// <summary>
            /// Gets the size of a <see cref="BGR"/> instance in bytes.
            /// </summary>
            public static int Size { get; } = Marshal.SizeOf(typeof(BGR));

            /// <summary>
            /// Gets the blue color component.
            /// </summary>
            public byte B { get; }

            /// <summary>
            /// Gets the green color component.
            /// </summary>
            public byte G { get; }

            /// <summary>
            /// Gets the red color component.
            /// </summary>
            public byte R { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Color.BGR"/> 
            /// struct.
            /// </summary>
            /// <param name="r">The red color component.</param>
            /// <param name="g">The green color component.</param>
            /// <param name="b">The blue color component.</param>
            public BGR(byte b, byte g, byte r)
            {
                B = b;
                G = g;
                R = r;
            }

            /// <summary>
            /// Explicitely converts a <see cref="Color"/> instance to a
            /// <see cref="Color.BGR"/> instance.
            /// </summary>
            /// <param name="color">
            /// The <see cref="Color"/> instance.
            /// </param>
            public static explicit operator BGR(Color color)
            {
                return new BGR(color.B, color.G, color.R);
            }
        }

        /// <summary>
        /// Defines a BGRA color with 8 bit per color component and an 
        /// alpha channel.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct BGRA
        {
            /// <summary>
            /// Gets the size of a <see cref="BGRA"/> instance in bytes.
            /// </summary>
            public static int Size { get; } = Marshal.SizeOf(typeof(BGRA));

            /// <summary>
            /// Gets the blue color component.
            /// </summary>
            public byte B { get; }

            /// <summary>
            /// Gets the green color component.
            /// </summary>
            public byte G { get; }

            /// <summary>
            /// Gets the red color component.
            /// </summary>
            public byte R { get; }

            /// <summary>
            /// Gets the alpha component (100% opacity is 255, 
            /// 0% opacity is 0).
            /// </summary>
            public byte Alpha { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Color"/> struct.
            /// </summary>
            /// <param name="b">The blue color component.</param>
            /// <param name="g">The green color component.</param>
            /// <param name="r">The red color component.</param>
            /// <param name="alpha">
            /// The alpha color component (100% opacity is 255, 
            /// 0% opacity is 0).
            /// </param>
            public BGRA(byte b, byte g, byte r, byte alpha)
            {
                B = b;
                G = g;
                R = r;
                Alpha = alpha;
            }

            /// <summary>
            /// Explicitely converts a <see cref="Color"/> instance to a
            /// <see cref="Color.BGRA"/> instance.
            /// </summary>
            /// <param name="color">
            /// The <see cref="Color"/> instance.
            /// </param>
            public static explicit operator BGRA(Color color)
            {
                return new BGRA(color.B, color.G, color.R, color.Alpha);
            }
        }

        /// <summary>
        /// Defines a RGB color with 32 bit per color component.
        /// Explicit conversion from/to a <see cref="Color"/> is not supported.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct RGB32
        {
            /// <summary>
            /// Gets the size of a <see cref="RGB32"/> instance in bytes.
            /// </summary>
            public static int Size { get; } = Marshal.SizeOf(typeof(RGB32));

            /// <summary>
            /// Gets the red color component.
            /// </summary>
            public float R { get; }

            /// <summary>
            /// Gets the green color component.
            /// </summary>
            public float G { get; }

            /// <summary>
            /// Gets the blue color component.
            /// </summary>
            public float B { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Color.RGB32"/> 
            /// struct.
            /// </summary>
            /// <param name="r">The red color component.</param>
            /// <param name="g">The green color component.</param>
            /// <param name="b">The blue color component.</param>
            public RGB32(float r, float g, float b)
            {
                R = r;
                G = g;
                B = b;
            }
        }
        #endregion

        #region Static common color definitions.
        /// <summary>
        /// Gets the color red.
        /// </summary>
        public static Color Red => new Color(255, 0, 0);

        /// <summary>
        /// Gets the color cyan.
        /// </summary>
        public static Color Cyan => new Color(0, 255, 255);

        /// <summary>
        /// Gets the color blue.
        /// </summary>
        public static Color Blue => new Color(0, 0, 255);

        /// <summary>
        /// Gets the color dark blue.
        /// </summary>
        public static Color DarkBlue => new Color(0, 0, 160);

        /// <summary>
        /// Gets the color light blue.
        /// </summary>
        public static Color LightBlue => new Color(173, 216, 230);

        /// <summary>
        /// Gets the color purple.
        /// </summary>
        public static Color Purple => new Color(128, 0, 128);

        /// <summary>
        /// Gets the color yellow.
        /// </summary>
        public static Color Yellow => new Color(255, 255, 0);

        /// <summary>
        /// Gets the color magenta.
        /// </summary>
        public static Color Magenta => new Color(255, 0, 255);

        /// <summary>
        /// Gets the color white.
        /// </summary>
        public static Color White => new Color(255, 255, 255);

        /// <summary>
        /// Gets the color silver.
        /// </summary>
        public static Color Silver => new Color(192, 192, 192);

        /// <summary>
        /// Gets the color gunmetal, a dark gray.
        /// </summary>
        public static Color Gunmetal => new Color(44, 53, 57);

        /// <summary>
        /// Gets the color gray. Equivalent to <see cref="Grey"/>.
        /// </summary>
        public static Color Gray => new Color(128, 128, 128);

        /// <summary>
        /// Gets the color grey. Equivalent to <see cref="Gray"/>.
        /// </summary>
        public static Color Grey => Gray;

        /// <summary>
        /// Gets the color black.
        /// </summary>
        public static Color Black => new Color(0, 0, 0);

        /// <summary>
        /// Gets the color red.
        /// </summary>
        public static Color Orange => new Color(255, 165, 0);

        /// <summary>
        /// Gets the color brown.
        /// </summary>
        public static Color Brown => new Color(165, 42, 42);

        /// <summary>
        /// Gets the color maroon.
        /// </summary>
        public static Color Maroon => new Color(128, 0, 0);

        /// <summary>
        /// Gets the color green.
        /// </summary>
        public static Color Green => new Color(0, 128, 0);

        /// <summary>
        /// Gets the color olive.
        /// </summary>
        public static Color Olive => new Color(128, 128, 0);

        /// <summary>
        /// Gets a transparent color (with all components set to 0).
        /// </summary>
        public static Color Transparent => new Color(0, 0, 0, 0);
        #endregion

        /// <summary>
        /// Gets the size of a <see cref="Color"/> instance in bytes.
        /// </summary>
        public static int Size { get; } = Marshal.SizeOf(typeof(Color));

        /// <summary>
        /// Gets the red color component.
        /// </summary>
        public byte R { get; }

        /// <summary>
        /// Gets the green color component.
        /// </summary>
        public byte G { get; }

        /// <summary>
        /// Gets the blue color component.
        /// </summary>
        public byte B { get; }

        /// <summary>
        /// Gets the alpha component (100% opacity is 255, 0% opacity is 0).
        /// </summary>
        public byte Alpha { get; }

        /// <summary>
        /// Gets the relative luminance of the current <see cref="Color"/>
        /// (photometric/digital ITU BT.709), linearly dampened by the 
        /// <see cref="Alpha"/> value.
        /// </summary>
        public float Luminance => (0.2126f * R + 0.7152f * G + 0.0722f * B)
            * (Alpha / (float)byte.MaxValue);

        /// <summary>
        /// Gets the brightness of the current <see cref="Color"/> as average
        /// of each color component, dampened by the <see cref="Alpha"/> value.
        /// </summary>
        public float Brightness => ((R + G + B) / 3.0f) *
            (Alpha / (float)byte.MaxValue);

        /// <summary>
        /// Initializes a new instance of the <see cref="Color"/> struct
        /// with 100% alpha.
        /// </summary>
        /// <param name="r">The red color component.</param>
        /// <param name="g">The green color component.</param>
        /// <param name="b">The blue color component.</param>
        public Color(byte r, byte g, byte b)
            : this(r, g, b, 255) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color"/> struct.
        /// </summary>
        /// <param name="r">The red color component.</param>
        /// <param name="g">The green color component.</param>
        /// <param name="b">The blue color component.</param>
        /// <param name="alpha">
        /// The alpha color component (100% opacity is 255, 0% opacity is 0).
        /// </param>
        public Color(byte r, byte g, byte b, byte alpha)
        {
            R = r;
            G = g;
            B = b;
            Alpha = alpha;
        }

        /// <summary>
        /// Parses a color from a hex color code.
        /// </summary>
        /// <param name="hexString">
        /// The hex color code (e.g. #FF0000 for red).
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Color"/> struct.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="hexString"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="hexString"/> was no valid 
        /// hex color code.
        /// </exception>
        public static Color Parse(string hexString)
        {
            if (hexString == null)
                throw new ArgumentNullException(nameof(hexString));

            NumberStyles hexStyle = NumberStyles.AllowHexSpecifier;

            hexString = hexString.TrimStart('#').Trim();
            string redHex, greenHex, blueHex, alphaHex = "";

            if (hexString.Length == 3 || hexString.Length == 4)
            {
                redHex = hexString.Substring(0, 1); redHex += redHex;
                greenHex = hexString.Substring(1, 1); greenHex += greenHex;
                blueHex = hexString.Substring(2, 1); blueHex += blueHex;
                if (hexString.Length == 4)
                {
                    alphaHex = hexString.Substring(3, 1); alphaHex += alphaHex;
                }
            }
            else if (hexString.Length == 6 || hexString.Length == 8)
            {
                redHex = hexString.Substring(0, 2);
                greenHex = hexString.Substring(2, 2);
                blueHex = hexString.Substring(4, 2);
                if (hexString.Length == 8)
                    alphaHex = hexString.Substring(6, 2);
            }
            else throw new ArgumentException("The specified hex string " +
              "had an invalid length!");

            try
            {
                if (!string.IsNullOrWhiteSpace(alphaHex))
                    return new Color(byte.Parse(redHex, hexStyle), byte.Parse(
                    greenHex, hexStyle), byte.Parse(blueHex, hexStyle),
                    byte.Parse(alphaHex, hexStyle));
                else
                    return new Color(byte.Parse(redHex, hexStyle), byte.Parse(
                    greenHex, hexStyle), byte.Parse(blueHex, hexStyle));
            }
            catch
            {
                throw new ArgumentException("The specified hex string was " +
                    "invalid!");
            }
        }

        /// <summary>
        /// Creates two new <see cref="Color"/> instances with the data from 
        /// a <see cref="VertexPropertyData"/> instance by using the first
        /// 4 property bytes for the <paramref name="primaryColor"/> (RGBA)
        /// and the last 4 property bytes for the 
        /// <paramref name="secondaryColor"/> (RGBA).
        /// </summary>
        /// <param name="properties">
        /// The properties instance from the parent <see cref="Vertex"/>.
        /// </param>
        /// <param name="primaryColor">
        /// The primary color, using the data from the provided properties.
        /// </param>
        /// <param name="secondaryColor">
        /// The secondary color, using the data from the provided properties.
        /// </param>
        public void FromVertexProperties(VertexPropertyData properties, 
            out Color primaryColor, out Color secondaryColor)
        {
            primaryColor = new Color(properties.P1, properties.P2,
                properties.P3, properties.P4);
            secondaryColor = new Color(properties.P5, properties.P6,
                properties.P7, properties.P8);
        }

        /// <summary>
        /// Converts the current <see cref="Color"/> into a hex color code.
        /// </summary>
        /// <param name="includeAlphaComponent">
        /// <c>true</c> to include the <see cref="Alpha"/> component as fourth
        /// hex value in the color string, <c>false</c> otherwise.
        /// </param>
        /// <returns>A new <see cref="string"/> instance.</returns>
        public string ToString(bool includeAlphaComponent)
        {
            return '#' + R.ToString("X2") + G.ToString("X2")
                + B.ToString("X2") +
                (includeAlphaComponent ? Alpha.ToString("X2") : "");
        }

        /// <summary>
        /// Converts the current <see cref="Color"/> into a hex color code
        /// with the <see cref="Alpha"/> component.
        /// </summary>
        /// <returns>A new <see cref="string"/> instance.</returns>
        public override string ToString()
        {
            return ToString(true);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current 
        /// object.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with the current object.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified object is equal to the current object,
        /// <c>false</c> otherwise.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is Color && Equals((Color)obj);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current 
        /// object.
        /// </summary>
        /// <param name="other">
        /// The object to compare with the current object.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified object is equal to the current object,
        /// <c>false</c> otherwise.
        /// </returns>
        public bool Equals(Color other)
        {
            return R == other.R &&
                   G == other.G &&
                   B == other.B &&
                   Alpha == other.Alpha;
        }

        /// <summary>
        /// Calculates the hash of the current object instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            var hashCode = -1273254677;
            hashCode = hashCode * -1521134295 + R.GetHashCode();
            hashCode = hashCode * -1521134295 + G.GetHashCode();
            hashCode = hashCode * -1521134295 + B.GetHashCode();
            hashCode = hashCode * -1521134295 + Alpha.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Color color1, Color color2)
        {
            return color1.Equals(color2);
        }

        public static bool operator !=(Color color1, Color color2)
        {
            return !(color1 == color2);
        }

        public static Color operator *(Color color, double brightness)
        {
            const byte max = byte.MaxValue;
            return new Color(
                (byte)Math.Min(Math.Max((color.R * brightness), 0), max),
                (byte)Math.Min(Math.Max((color.G * brightness), 0), max),
                (byte)Math.Min(Math.Max((color.B * brightness), 0), max),
                color.Alpha);
        }

        public static Color operator *(double brightness, Color color)
        {
            const byte max = byte.MaxValue;
            return new Color(
                (byte)Math.Min(Math.Max((color.R * brightness), 0), max),
                (byte)Math.Min(Math.Max((color.G * brightness), 0), max),
                (byte)Math.Min(Math.Max((color.B * brightness), 0), max),
                color.Alpha);
        }

        public static Color operator *(Color color1, Color color2)
        {
            const byte max = byte.MaxValue;
            return new Color(
                (byte)Math.Min(Math.Max((color1.R * color2.R), 0), max),
                (byte)Math.Min(Math.Max((color1.G * color2.G), 0), max),
                (byte)Math.Min(Math.Max((color1.B * color2.B), 0), max),
                color1.Alpha);
        }

        public static Color operator +(Color color1, Color color2)
        {
            const byte max = byte.MaxValue;
            return new Color(
                (byte)Math.Min(Math.Max((color1.R + color2.R), 0), max),
                (byte)Math.Min(Math.Max((color1.G + color2.G), 0), max),
                (byte)Math.Min(Math.Max((color1.B + color2.B), 0), max),
                color1.Alpha);
        }

        public static Color operator -(Color color1, Color color2)
        {
            const byte max = byte.MaxValue;
            return new Color(
                (byte)Math.Min(Math.Max((color1.R - color2.R), 0), max),
                (byte)Math.Min(Math.Max((color1.G - color2.G), 0), max),
                (byte)Math.Min(Math.Max((color1.B - color2.B), 0), max),
                color1.Alpha);
        }

        /// <summary>
        /// Explicitely converts a <see cref="Color.RGB"/> instance to a
        /// <see cref="Color"/> instance with 100% alpha.
        /// </summary>
        /// <param name="color">
        /// The <see cref="Color"/> instance.
        /// </param>
        public static explicit operator Color(Color.RGB color)
        {
            return new Color(color.R, color.G, color.B);
        }

        /// <summary>
        /// Explicitely converts a <see cref="Color.ARGB"/> instance to a
        /// <see cref="Color"/> instance.
        /// </summary>
        /// <param name="color">
        /// The <see cref="Color"/> instance.
        /// </param>
        public static explicit operator Color(Color.ARGB color)
        {
            return new Color(color.R, color.G, color.B, color.Alpha);
        }

        /// <summary>
        /// Explicitely converts a <see cref="Color.BGR"/> instance to a
        /// <see cref="Color"/> instance with 100% alpha.
        /// </summary>
        /// <param name="color">
        /// The <see cref="Color"/> instance.
        /// </param>
        public static explicit operator Color(Color.BGR color)
        {
            return new Color(color.R, color.G, color.B);
        }

        /// <summary>
        /// Explicitely converts a <see cref="Color.BGRA"/> instance to a
        /// <see cref="Color"/> instance.
        /// </summary>
        /// <param name="color">
        /// The <see cref="Color"/> instance.
        /// </param>
        public static explicit operator Color(Color.BGRA color)
        {
            return new Color(color.R, color.G, color.B, color.Alpha);
        }
    }
}
