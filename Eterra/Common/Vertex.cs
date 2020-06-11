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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Eterra.Common
{
    /// <summary>
    /// Represents an point in three-dimensional space with special 
    /// attributes related to meshes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Vertex
    {
        /// <summary>
        /// Defines the default format string that is used to convert 
        /// <see cref="Vertex"/> instances to a <see cref="string"/> or parse
        /// a <see cref="string"/> into one or more <see cref="Vertex"/>
        /// instances.
        /// </summary>
        /// <remarks>
        /// The tokens in this string (e.g. "nx", "x") stand for individual
        /// components of a <see cref="Vertex"/> instance and can be rearranged
        /// or omitted in a format string to produce a different string
        /// representation or parse a different string representation of a
        /// <see cref="Vertex"/> back into a valid instance.
        /// Every omitted token causes its associated value to default to 0.
        /// The "x", "y" and "z" components define the vertex position,
        /// the "nx", "ny", and "nz" components define the normal vector,
        /// the "tx" and "ty" define the texture coordinate - all in
        /// culture-invariant floats.
        /// The "p1" to "p8" tokens define the property byte values in
        /// (unsigned) bytes.
        /// Mind that the format string may only consist of these tokens in 
        /// that string, separated by whitespaces - additional tokens or 
        /// different separators or characters are not supported.
        /// </remarks>
        public const string DefaultVertexStringFormat =
            "x y z nx ny nz tx ty p1 p2 p3 p4 p5 p6 p7 p8";

        /// <summary>
        /// Gets the size of a <see cref="Vertex"/> instance in bytes.
        /// </summary>
        public static int Size { get; } = Marshal.SizeOf(typeof(Vertex));

        /// <summary>
        /// Gets the position.
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// Gets the normal vector.
        /// </summary>
        public Vector3 Normal { get; }

        /// <summary>
        /// Gets the texture coordinate.
        /// </summary>
        public Vector2 TextureCoordinate { get; }

        /// <summary>
        /// Gets the property data, which meaning is defined in the parent
        /// collection of this <see cref="Vertex"/> instance.
        /// </summary>
        public VertexPropertyData Properties { get; }

        /// <summary>
        /// Creates a new vertex.
        /// </summary>
        /// <param name="position">The vertex position.</param>
        public Vertex(Vector3 position)
            : this(position, new Vector3(0, 1, 0), new Vector2(0, 0),
                  new VertexPropertyData())
        { }

        /// <summary>
        /// Creates a new vertex.
        /// </summary>
        /// <param name="x">The X component of the vertex position.</param>
        /// <param name="y">The Y component of the vertex position.</param>
        /// <param name="z">The Z component of the vertex position.</param>
        public Vertex(float x, float y, float z)
            : this(new Vector3(x,y,z)) { }

        /// <summary>
        /// Creates a new vertex.
        /// </summary>
        /// <param name="position">The vertex position.</param>
        /// <param name="normal">The normal direction.</param>
        public Vertex(Vector3 position, Vector3 normal)
            : this(position, normal, new Vector2(0, 0),
                  new VertexPropertyData()) { }

        /// <summary>
        /// Creates a new vertex.
        /// </summary>
        /// <param name="x">The X component of the vertex position.</param>
        /// <param name="y">The Y component of the vertex position.</param>
        /// <param name="z">The Z component of the vertex position.</param>
        /// <param name="nx">The X component of the normal vector.</param>
        /// <param name="ny">The Y component of the normal vector.</param>
        /// <param name="nz">The Z component of the normal vector.</param>
        public Vertex(float x, float y, float z, float nx, float ny, float nz)
            : this(new Vector3(x, y, z), new Vector3(nx, ny, nz)) { }

        /// <summary>
        /// Creates a new vertex.
        /// </summary>
        /// <param name="position">The vertex position.</param>
        /// <param name="textureCoordinate">The texture coordinate.</param>
        public Vertex(Vector3 position, Vector2 textureCoordinate)
            : this(position, new Vector3(0, 1, 0), textureCoordinate,
                  new VertexPropertyData()) { }

        /// <summary>
        /// Creates a new vertex.
        /// </summary>
        /// <param name="x">The X component of the vertex position.</param>
        /// <param name="y">The Y component of the vertex position.</param>
        /// <param name="z">The Z component of the vertex position.</param>
        /// <param name="tx">The X component of the texture coordinate.</param>
        /// <param name="ty">The Y component of the texture coordinate.</param>
        public Vertex(float x, float y, float z, float tx, float ty)
            : this(new Vector3(x, y, z), new Vector2(tx, ty)) { }

        /// <summary>
        /// Creates a new vertex.
        /// </summary>
        /// <param name="position">The vertex position.</param>
        /// <param name="normal">The normal vector.</param>
        /// <param name="textureCoordinate">The texture coordinate.</param>
        public Vertex(Vector3 position, Vector3 normal, 
            Vector2 textureCoordinate)
            : this(position, normal, textureCoordinate, 
                  new VertexPropertyData()) { }

        /// <summary>
        /// Creates a new vertex.
        /// </summary>
        /// <param name="x">The X component of the vertex position.</param>
        /// <param name="y">The Y component of the vertex position.</param>
        /// <param name="z">The Z component of the vertex position.</param>
        /// <param name="nx">The X component of the normal vector.</param>
        /// <param name="ny">The Y component of the normal vector.</param>
        /// <param name="nz">The Z component of the normal vector.</param>
        /// <param name="tx">The X component of the texture coordinate.</param>
        /// <param name="ty">The Y component of the texture coordinate.</param>
        public Vertex(float x, float y, float z, float nx, float ny, float nz,
            float tx, float ty)
            : this(new Vector3(x, y, z), new Vector3(nx, ny, nz),
                  new Vector2(tx, ty)) { }

        /// <summary>
        /// Creates a new vertex.
        /// </summary>
        /// <param name="position">The vertex position.</param>
        /// <param name="normal">The normal vector.</param>
        /// <param name="textureCoordinate">The texture coordinate.</param>
        /// <param name="properties">
        /// The property data of the current vertex.
        /// </param>
        public Vertex(Vector3 position, Vector3 normal,
            Vector2 textureCoordinate,
            VertexPropertyData properties)
        {
            Position = position;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
            Properties = properties;
        }

        /// <summary>
        /// Converts the current <see cref="Vertex"/> instance to a string 
        /// representation using the default format defined by 
        /// <see cref="DefaultVertexStringFormat"/>.
        /// </summary>
        /// <returns>A new <see cref="string"/>.</returns>
        public override string ToString()
        {
            return ToString(DefaultVertexStringFormat);
        }

        /// <summary>
        /// Converts the current <see cref="Vertex"/> instance to a string 
        /// representation.
        /// </summary>
        /// <param name="format">
        /// The format string that defines which <see cref="Vertex"/> 
        /// components are written to the string in which order.
        /// See the documentation of <see cref="DefaultVertexStringFormat"/>
        /// for more details.
        /// </param>
        /// <returns>A new <see cref="string"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="format"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="format"/> is invalid.
        /// </exception>
        public string ToString(string format)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            format = format.Trim().ToLowerInvariant();
            if (format.Length == 0)
                throw new ArgumentException("The format string mustn't be " +
                    "empty or whitespaces only.");

            string[] formatSegments = format.ToLowerInvariant().Split(' ');

            CultureInfo c = CultureInfo.InvariantCulture;

            using (StringWriter writer = new StringWriter())
            {
                void WriteFloat(float f) => writer.Write(f.ToString(c));
                void WriteByte(byte b) => writer.Write(b.ToString(c));

                for (int i = 0; i < formatSegments.Length; i++)
                {
                    if (i > 0) writer.Write(' ');

                    switch (formatSegments[i])
                    {
                        case "x": WriteFloat(Position.X); break;
                        case "y": WriteFloat(Position.Y); break;
                        case "z": WriteFloat(Position.Z); break;
                        case "nx": WriteFloat(Normal.X); break;
                        case "ny": WriteFloat(Normal.Y); break;
                        case "nz": WriteFloat(Normal.Z); break;
                        case "tx": WriteFloat(TextureCoordinate.X); break;
                        case "ty": WriteFloat(TextureCoordinate.Y); break;
                        case "p1": WriteByte(Properties.P1); break;
                        case "p2": WriteByte(Properties.P2); break;
                        case "p3": WriteByte(Properties.P3); break;
                        case "p4": WriteByte(Properties.P4); break;
                        case "p5": WriteByte(Properties.P5); break;
                        case "p6": WriteByte(Properties.P6); break;
                        case "p7": WriteByte(Properties.P7); break;
                        case "p8": WriteByte(Properties.P8); break;
                        default: 
                            throw new ArgumentException("Format string " +
                                "invalid.");
                    }
                }

                return writer.ToString();
            }
        }

        /// <summary>
        /// Parses a <see cref="string"/> into a collection of 
        /// <see cref="Vertex"/> instances, expecting the vertices to be in the
        /// default format defined by <see cref="DefaultVertexStringFormat"/>.
        /// </summary>
        /// <param name="verticesString">
        /// The string representation of a collection of vertices.
        /// </param>
        /// <returns>A new array of <see cref="Vertex"/> instances.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="verticesString"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the specified <paramref name="verticesString"/> is
        /// invalid.
        /// </exception>
        public static Vertex[] Parse(string verticesString)
        {
            return Parse(verticesString, DefaultVertexStringFormat);
        }

        /// <summary>
        /// Parses a <see cref="string"/> into a collection of 
        /// <see cref="Vertex"/> instances.
        /// </summary>
        /// <param name="verticesString">
        /// The string representation of a collection of vertices.
        /// </param>
        /// <param name="format">
        /// The format string that defines which <see cref="Vertex"/> 
        /// components are parsed from the string in which order.
        /// See the documentation of <see cref="DefaultVertexStringFormat"/>
        /// for more details.
        /// </param>
        /// <returns>A new array of <see cref="Vertex"/> instances.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="verticesString"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="format"/> is invalid.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the specified <paramref name="verticesString"/> is
        /// invalid.
        /// </exception>
        public static Vertex[] Parse(string verticesString, string format)
        {
            if (verticesString == null)
                throw new ArgumentNullException(nameof(verticesString));
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            format = format.Trim().ToLowerInvariant();
            if (format.Length == 0)
                throw new ArgumentException("The format string mustn't be " +
                    "empty or whitespaces only.");

            List<Vertex> vertices = new List<Vertex>();

            string[] formatSegments = format.Split(' ');
            int currentFormatSegment = 0;

            float x = 0, y = 0, z = 0, nx = 0, ny = 0, nz = 0, tx = 0, ty = 0;
            byte p1 = 0, p2 = 0, p3 = 0, p4 = 0,
                p5 = 0, p6 = 0, p7 = 0, p8 = 0;

            void AddNewVertexAndReset()
            {
                Vector3 positon = new Vector3(x, y, z);
                Vector3 normal = new Vector3(nx, ny, nz);
                Vector2 textureCoordinate = new Vector2(tx, ty);
                VertexPropertyData properties = new VertexPropertyData(
                    p1, p2, p3, p4, p5, p6, p7, p8);

                vertices.Add(new Vertex(positon, normal, textureCoordinate,
                    properties));

                x = y = z = nx = ny = nz = tx = ty = 0;
                p1 = p2 = p3 = p4 = p5 = p6 = p7 = p8 = 0;

                currentFormatSegment = 0;
            }

            NumberStyles ns = NumberStyles.Float;
            CultureInfo c = CultureInfo.InvariantCulture;

            Regex segmentRegex = new Regex("[.\\S]+");

            Match match = segmentRegex.Match(verticesString);
            while (match.Success)
            {
                int parserIndex = match.Index;

                switch (formatSegments[currentFormatSegment++])
                {
                    case "x":
                        if (float.TryParse(match.Value, ns, c, out x)) break;
                        else throw new FormatException("Invalid Position.X " +
                            "value at " + parserIndex + ".");
                    case "y":
                        if (float.TryParse(match.Value, ns, c, out y)) break;
                        else throw new FormatException("Invalid Position.Y " +
                            "value at " + parserIndex + ".");
                    case "z":
                        if (float.TryParse(match.Value, ns, c, out z)) break;
                        else throw new FormatException("Invalid Position.Z " +
                            "value at " + parserIndex + ".");
                    case "nx":
                        if (float.TryParse(match.Value, ns, c, out nx)) break;
                        else throw new FormatException("Invalid Normal.X " +
                            "value at " + parserIndex + ".");
                    case "ny":
                        if (float.TryParse(match.Value, ns, c, out ny)) break;
                        else throw new FormatException("Invalid Normal.Y " +
                            "value at " + parserIndex + ".");
                    case "nz":
                        if (float.TryParse(match.Value, ns, c, out nz)) break;
                        else throw new FormatException("Invalid Normal.Z " +
                            "value at " + parserIndex + ".");
                    case "tx":
                        if (float.TryParse(match.Value, ns, c, out tx)) break;
                        else throw new FormatException("Invalid Texture" +
                            "Coordinate.X value at " + parserIndex + ".");
                    case "ty":
                        if (float.TryParse(match.Value, ns, c, out ty)) break;
                        else throw new FormatException("Invalid Texture" +
                            "Coordinate.Y value at " + parserIndex + ".");
                    case "p1":
                        if (byte.TryParse(match.Value, out p1)) break;
                        else throw new FormatException("Invalid " +
                            "Properties.P1 value at " + parserIndex + ".");
                    case "p2":
                        if (byte.TryParse(match.Value, out p2)) break;
                        else throw new FormatException("Invalid " +
                            "Properties.P2 value at " + parserIndex + ".");
                    case "p3":
                        if (byte.TryParse(match.Value, out p3)) break;
                        else throw new FormatException("Invalid " +
                            "Properties.P3 value at " + parserIndex + ".");
                    case "p4":
                        if (byte.TryParse(match.Value, out p4)) break;
                        else throw new FormatException("Invalid " +
                            "Properties.P4 value at " + parserIndex + ".");
                    case "p5":
                        if (byte.TryParse(match.Value, out p5)) break;
                        else throw new FormatException("Invalid " +
                            "Properties.P5 value at " + parserIndex + ".");
                    case "p6":
                        if (byte.TryParse(match.Value, out p6)) break;
                        else throw new FormatException("Invalid " +
                            "Properties.P6 value at " + parserIndex + ".");
                    case "p7":
                        if (byte.TryParse(match.Value, out p7)) break;
                        else throw new FormatException("Invalid " +
                            "Properties.P7 value at " + parserIndex + ".");
                    case "p8":
                        if (byte.TryParse(match.Value, out p8)) break;
                        else throw new FormatException("Invalid " +
                            "Properties.P8 value at " + parserIndex + ".");
                    default:
                        throw new ArgumentException("Format string invalid.");
                }

                if (currentFormatSegment >= formatSegments.Length)
                    AddNewVertexAndReset();

                match = match.NextMatch();
            }

            if (currentFormatSegment > 0) AddNewVertexAndReset();

            return vertices.ToArray();
        }
    }
}
