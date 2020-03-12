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
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Eterra.Common
{
    /// <summary>
    /// Represents an point in three-dimensional space with special 
    /// attributes related to meshes. Instances of this class are immutable.
    /// </summary>
    /// <remarks>
    /// The order of the elements of a <see cref="Vertex"/> is 
    /// PPPNNNTTDDDDIIII, where 'P' defines the position elements, 'N' the 
    /// normals, 'T' the texture coordinates, 'D' the deformer IDs and 
    /// 'I' the defomer weights. The same applies to the memory layout of
    /// the byte representation of that struct.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Vertex
    {
        /// <summary>
        /// Defines the character which is used to separate the components
        /// in the string representation of instances of this class.
        /// </summary>
        public const char ComponentSeparator = ';';

        /// <summary>
        /// Defines the amount of components the string representation of
        /// the vertex must have to be valid.
        /// </summary>
        private const int ComponentCount = 16;

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
        /// Gets the deformer attachments of this vertex.
        /// </summary>
        public DeformerAttachments DeformerAttachments { get; }

        /// <summary>
        /// Creates a new vertex.
        /// </summary>
        /// <param name="position">The vertex position.</param>
        public Vertex(Vector3 position)
            : this(position, new Vector3(0, 1, 0), new Vector2(0, 0),
                  new DeformerAttachments())
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
                  new DeformerAttachments()) { }

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
                  new DeformerAttachments()) { }

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
                  new DeformerAttachments()) { }

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
        /// <param name="deformerAttachments">
        /// An enumeration of deformer attachments.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="deformerAttachments"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="deformerAttachments"/> contains
        /// more than four elements or when a deformer index is referenced
        /// more than once.
        /// </exception>
        public Vertex(Vector3 position, Vector3 normal,
            Vector2 textureCoordinate,
            DeformerAttachments deformerAttachments)
        {
            Position = position;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
            DeformerAttachments = deformerAttachments;
        }

        /// <summary>
        /// Generates a vertex string, which consists of all 16 possible 
        /// vertex parameters in one string - separated by the character
        /// defined in <see cref="ComponentSeparator"/>. The invariant culture
        /// is used for converting the numbers to strings.
        /// </summary>
        /// <returns>A new string.</returns>
        public override string ToString()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            using (StringWriter vw = new StringWriter())
            {
                vw.Write(Position.X.ToString(c));
                vw.Write(ComponentSeparator);
                vw.Write(Position.Y.ToString(c));
                vw.Write(ComponentSeparator);
                vw.Write(Position.Z.ToString(c));
                vw.Write(ComponentSeparator);

                vw.Write(Normal.X.ToString(c));
                vw.Write(ComponentSeparator);
                vw.Write(Normal.Y.ToString(c));
                vw.Write(ComponentSeparator);
                vw.Write(Normal.Z.ToString(c));
                vw.Write(ComponentSeparator);

                vw.Write(TextureCoordinate.X.ToString(c));
                vw.Write(ComponentSeparator);
                vw.Write(TextureCoordinate.Y.ToString(c));
                vw.Write(ComponentSeparator);

                vw.Write(DeformerAttachments.AttachmentOneIndex);
                vw.Write(ComponentSeparator);
                vw.Write(DeformerAttachments.AttachmentOneWeight);
                vw.Write(ComponentSeparator);
                vw.Write(DeformerAttachments.AttachmentTwoIndex);
                vw.Write(ComponentSeparator);
                vw.Write(DeformerAttachments.AttachmentTwoWeight);
                vw.Write(ComponentSeparator);
                vw.Write(DeformerAttachments.AttachmentThreeIndex);
                vw.Write(ComponentSeparator);
                vw.Write(DeformerAttachments.AttachmentThreeWeight);
                vw.Write(ComponentSeparator);
                vw.Write(DeformerAttachments.AttachmentFourIndex);
                vw.Write(ComponentSeparator);
                vw.Write(DeformerAttachments.AttachmentFourWeight);

                return vw.ToString();
            };
        }

        /// <summary>
        /// Parses a vertex from a string in the format generated by
        /// <see cref="ToString"/>. The invariant culture is used to parse
        /// the contained numbers.
        /// </summary>
        /// <param name="str">The vertex string.</param>
        /// <returns>A new vertex.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="str"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="str"/> has an 
        /// invalid format.
        /// </exception>
        public static Vertex Parse(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));

            string[] elements = str.Split(ComponentSeparator);
            if (elements.Length != ComponentCount)
                throw new ArgumentException("The specified string had an " +
                    "invalid amount of components!");
            else
            {
                CultureInfo c = CultureInfo.InvariantCulture;
                try
                {
                    Vector3 position = new Vector3(
                        float.Parse(elements[0], c),
                        float.Parse(elements[1], c), 
                        float.Parse(elements[2], c));
                    Vector3 normal = new Vector3(
                        float.Parse(elements[3], c),
                        float.Parse(elements[4], c), 
                        float.Parse(elements[5], c));
                    Vector2 texture = new Vector2(
                        float.Parse(elements[6], c),
                        float.Parse(elements[7], c));
                    DeformerAttachments attachments = new DeformerAttachments(
                         byte.Parse(elements[8]),
                         byte.Parse(elements[9]),
                         byte.Parse(elements[10]),
                         byte.Parse(elements[11]),
                         byte.Parse(elements[12]),
                         byte.Parse(elements[13]),
                         byte.Parse(elements[14]),
                         byte.Parse(elements[15])
                    );

                    return new Vertex(position, normal, texture, attachments);
                }
                catch
                {
                    throw new ArgumentException("The specified string had " +
                        "invalid component values!");
                }
            }
        }
    }
}
