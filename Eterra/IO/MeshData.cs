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

using Eterra.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Eterra.IO
{
    /// <summary>
    /// Provides the base class from which meshes are derived.
    /// </summary>
    public abstract class MeshData : DisposableBase
    {
        #region Internally used Mesh implementation.
        internal class MemoryMesh : MeshData
        {
            private VertexCollection vertexData;

            private FaceCollection faceData;

            public MemoryMesh(VertexCollection vertexData, 
                FaceCollection faceData, Skeleton skeleton) 
                : base(vertexData?.Count ?? 3, faceData?.Count ?? 1, skeleton)
            {
                this.vertexData = vertexData 
                    ?? throw new ArgumentNullException(nameof(vertexData));
                this.faceData = faceData 
                    ?? throw new ArgumentNullException(nameof(faceData));
            }

            public override Vertex GetVertex(int index)
            {
                ThrowIfDisposed();

                if (index < 0 || index > VertexCount)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return vertexData[index];
            }

            public override Vertex[] GetVertices(int index, int length)
            {
                ThrowIfDisposed();

                ValidateVertexRange(index, length);

                Vertex[] output = new Vertex[length];
                Array.ConstrainedCopy(vertexData.BaseData, index, output, 0, 
                    length);
                return output;
            }

            public override Face GetFace(int index)
            {
                ThrowIfDisposed();

                if (index < 0 || index > FaceCount)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return faceData[index];
            }

            public override Face[] GetFaces(int index, int length)
            {
                ThrowIfDisposed();

                ValidateFaceRange(index, length);

                Face[] output = new Face[length];
                Array.ConstrainedCopy(faceData.BaseArray, index, output, 0, 
                    length);
                return output;
            }

            protected override void Dispose(bool disposing)
            {
                vertexData = null;
                faceData = null;
            }
        }
        #endregion

        /// <summary>
        /// Gets a single-quad plane on the XY-axis with a width and height of 
        /// 1, its pivot in the center of the plane and 
        /// -<see cref="Vector3.UnitZ"/> as normal.
        /// </summary>
        public static MeshData Plane { get; } = CreatePlane(Vector2.One);

        /// <summary>
        /// Gets a three-dimensional box with a width, height and depth of 1,
        /// its pivot in the center of the box and am UV layout with that any 
        /// assigned texture will be displayed completely on each side of 
        /// the box.
        /// </summary>
        public static MeshData Box { get; } = CreateBox(Vector3.One);

        /// <summary>
        /// Gets the amount of vertices the current <see cref="MeshData"/> has.
        /// </summary>
        public int VertexCount { get; }

        /// <summary>
        /// Gets the amount of faces the current <see cref="MeshData"/> has.
        /// </summary>
        public int FaceCount { get; }

        /// <summary>
        /// Gets the <see cref="Skeleton"/> of the mesh.
        /// </summary>
        public Skeleton Skeleton { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshData"/> class.
        /// </summary>
        /// <param name="vertexCount">
        /// The amount of vertices the current mesh holds. Must be greater
        /// than 2.
        /// </param>
        /// <param name="faceCount">
        /// The amount of faces the current mesh holds. Must be greater than 0.
        /// </param>
        /// <param name="skeleton">
        /// The skeleton of the mesh or <see cref="Skeleton.Empty"/> if the
        /// mesh shouldn't have any skeleton information. Must be read-only.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="skeleton"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="Skeleton.IsReadOnly"/> of 
        /// <paramref name="skeleton"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="vertexCount"/> is less than 3
        /// or when <paramref name="faceCount"/> is less than 1.
        /// </exception>
        protected MeshData (int vertexCount, int faceCount, Skeleton skeleton)
        {
            if (vertexCount < 3)
                throw new ArgumentOutOfRangeException(nameof(vertexCount));
            if (faceCount < 1)
                throw new ArgumentOutOfRangeException(nameof(faceCount));

            Skeleton = skeleton ??
                throw new ArgumentNullException(nameof(skeleton));
            if (!skeleton.IsReadOnly)
                throw new ArgumentException("The specified skeleton isn't " +
                    "read-only and can't be used.");

            VertexCount = vertexCount;
            FaceCount = faceCount;
        }

        /// <summary>
        /// Gets a single vertex from the <see cref="MeshData"/>.
        /// </summary>
        /// <param name="index">The index of the vertex.</param>
        /// <returns>A <see cref="Vertex"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="index"/> is less than 0
        /// or equal to/greater than <see cref="VertexCount"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the object data was disposed and can't be accessed
        /// anymore.
        /// </exception>
        public abstract Vertex GetVertex(int index);

        /// <summary>
        /// Gets multiple vertices in a specific range from 
        /// the <see cref="MeshData"/>.
        /// </summary>
        /// <remarks>
        /// It is recommended to use this method for retrieving the mesh data,
        /// which should be transferred to the GPU.
        /// When overriding the <see cref="MeshData"/> class, it is 
        /// recommended to override this method and optimize its performance,
        /// as the base implementation retrieves the image data vertex by 
        /// vertex using the <see cref="GetVertex(int)(int, int)"/> method.
        /// </remarks>
        /// <param name="index">
        /// The starting index of the range to be retrieved.
        /// </param>
        /// <param name="length">
        /// The length of the range to be retrieved.
        /// </param>
        /// <returns>A new array of <see cref="Vertex"/> instances.</returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <paramref name="index"/> is less than 0
        /// or equal to/greater than <see cref="VertexCount"/>, when
        /// <paramref name="length"/> is equal to/less than 0 or when
        /// the <paramref name="length"/> of a section starting at the
        /// specified <paramref name="index"/> would exceed the available
        /// data.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the object data was disposed and can't be accessed
        /// anymore.
        /// </exception>
        public virtual Vertex[] GetVertices(int index, int length)
        {
            ValidateVertexRange(index, length);

            Vertex[] vertices = new Vertex[length];

            try
            {
                for (int i = 0; i < length; i++)
                    vertices[i] = GetVertex(index + i);
            }
            catch (ObjectDisposedException) { throw; }

            return vertices;
        }

        /// <summary>
        /// Gets a single face from the <see cref="MeshData"/>.
        /// </summary>
        /// <param name="index">The index of the face.</param>
        /// <returns>A <see cref="Face"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="index"/> is less than 0
        /// or equal to/greater than <see cref="FaceCount"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the object data was disposed and can't be accessed
        /// anymore.
        /// </exception>
        public abstract Face GetFace(int index);

        /// <summary>
        /// Gets multiple faces in a specific range from 
        /// the <see cref="MeshData"/>.
        /// </summary>
        /// <remarks>
        /// It is recommended to use this method for retrieving the mesh data,
        /// which should be transferred to the GPU.
        /// When overriding the <see cref="MeshData"/> class, it is 
        /// recommended to override this method and optimize its performance,
        /// as the base implementation retrieves the image data face by face
        /// using the <see cref="GetFace(int, int)"/> method.
        /// </remarks>
        /// <param name="index">
        /// The starting index of the range to be retrieved.
        /// </param>
        /// <param name="length">
        /// The length of the range to be retrieved.
        /// </param>
        /// <returns>A new array of <see cref="Face"/> instances.</returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <paramref name="index"/> is less than 0
        /// or equal to/greater than <see cref="FaceCount"/>, when
        /// <paramref name="length"/> is equal to/less than 0 or when
        /// the <paramref name="length"/> of a section starting at the
        /// specified <paramref name="index"/> would exceed the available
        /// data.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the object data was disposed and can't be accessed
        /// anymore.
        /// </exception>
        public virtual Face[] GetFaces(int index, int length)
        {
            ValidateFaceRange(index, length);

            Face[] faces = new Face[length];
            try
            {
                for (int i = 0; i < length; i++)
                    faces[i] = GetFace(index + i);
            }
            catch (ObjectDisposedException) { throw; }

            return faces;
        }

        /// <summary>
        /// Validates a vertex range and throws an exception if the range 
        /// exceeds the available data. If the range is valid, calling this
        /// method will have no effect.
        /// </summary>
        /// <param name="index">
        /// The starting index of the range.
        /// </param>
        /// <param name="length">
        /// The length of the range.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <paramref name="index"/> is less than 0
        /// or equal to/greater than <see cref="VertexCount"/>, when
        /// <paramref name="length"/> is equal to/less than 0 or when
        /// the <paramref name="length"/> of a section starting at the
        /// specified <paramref name="index"/> would exceed the available
        /// data.
        /// </exception>
        protected void ValidateVertexRange(int index, int length)
        {
            if (index < 0 || index >= VertexCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (length <= 0 || (index + length) > VertexCount)
                throw new ArgumentOutOfRangeException(nameof(length));
        }

        /// <summary>
        /// Validates a face range and throws an exception if the range 
        /// exceeds the available data. If the range is valid, calling this
        /// method will have no effect.
        /// </summary>
        /// <param name="index">
        /// The starting index of the range.
        /// </param>
        /// <param name="length">
        /// The length of the range.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <paramref name="index"/> is less than 0
        /// or equal to/greater than <see cref="FaceCount"/>, when
        /// <paramref name="length"/> is equal to/less than 0 or when
        /// the <paramref name="length"/> of a section starting at the
        /// specified <paramref name="index"/> would exceed the available
        /// data.
        /// </exception>
        protected void ValidateFaceRange(int index, int length)
        {
            if (index < 0 || index >= FaceCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (length <= 0 || (index + length) > FaceCount)
                throw new ArgumentOutOfRangeException(nameof(length));
        }

        /// <summary>
        /// Initializes a new <see cref="MeshData"/> instance with existing 
        /// mesh data.
        /// </summary>
        /// <param name="vertexCount">
        /// The amount of vertices in the source (and the new) mesh.
        /// </param>
        /// <param name="faceCount">
        /// The amount of faces in the source (and the new) mesh.
        /// </param>
        /// <param name="vertexRetriever">
        /// A method which gets a specific <see cref="Vertex"/> of the source
        /// mesh.
        /// </param>
        /// <param name="faceRetriever">
        /// A method which gets a specific <see cref="Face"/> of the source
        /// mesh.
        /// </param>
        /// <param name="skeleton">
        /// The skeleton of the mesh or <see cref="Skeleton.Empty"/> if the
        /// mesh shouldn't have any skeleton information. Must be read-only.
        /// </param>
        /// <param name="bufferData">
        /// <c>true</c> to create a complete copy of the mesh data and store 
        /// it in the new <see cref="MeshData"/> instance (needs more 
        /// memory and longer to initialize, but has a predictable, constant 
        /// and higher GPU transfer speed and reliability), 
        /// <c>false</c> to create a wrapper around the specified delegates
        /// and redirect all mesh data requests of the new 
        /// <see cref="MeshData"/> (<see cref="GetFace(int)"/>, 
        /// <see cref="GetVertex(int)"/>...) to the specified 
        /// <paramref name="vertexRetriever"/>/<paramref name="faceRetriever"/> 
        /// (faster initialisation of the texture and less memory usage, but 
        /// less predictability on speed during GPU transfer).
        /// If the specified delegates access the data of an object 
        /// implementing <see cref="IDisposable"/>, it is highly recommended 
        /// to specifiy <c>true</c> as parameter value.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshData"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="vertexRetriever"/>,
        /// <paramref name="faceRetriever"/> or
        /// <paramref name="skeleton"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <paramref name="vertexCount"/> is less than 3
        /// or greater than <see cref="MaxVertices"/>, when 
        /// <paramref name="faceCount"/> is less than 1 or greater than
        /// <see cref="MaxFaces"/>, when the specified 
        /// <paramref name="vertexCount"/> or <paramref name="faceCount"/> 
        /// exceeded the dimensions accessible by the 
        /// <paramref name="vertexRetriever"/> or the 
        /// <paramref name="faceRetriever"/>, or when 
        /// <see cref="Skeleton.IsReadOnly"/> of <paramref name="skeleton"/> 
        /// is <c>false</c>.
        /// </exception>
        public static MeshData Create(VertexCollection vertices,
            FaceCollection faces, Skeleton skeleton)
        {
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));
            if (faces == null)
                throw new ArgumentNullException(nameof(faces));
            if (skeleton == null)
                throw new ArgumentNullException(nameof(skeleton));

            if (vertices.Count < 3)
                throw new ArgumentException("The vertex count was " +
                    "less than 3, which is too low.");
            if (faces.Count < 1)
                throw new ArgumentException("The face count was " +
                    "less than 1, which is too low.");

            return new MemoryMesh(vertices, faces, skeleton);
        }

        /// <summary>
        /// Loads a <see cref="MeshData"/> instance in the internal format 
        /// used by <see cref="NativeFormatHandler"/>.
        /// </summary>
        /// <param name="stream">
        /// The source stream.
        /// </param>
        /// <param name="expectFormatHeader">
        /// <c>true</c> if a header, as defined in 
        /// <see cref="NativeFormatHandler"/>, is expected to occur at the
        /// position of the <paramref name="stream"/>, <c>false</c> if the 
        /// current position of the <paramref name="stream"/> is directly at 
        /// the beginning of the resource data.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshData"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="StreamWrapper.CanRead"/> of 
        /// <paramref name="stream"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the data in the stream had an invalid format.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// resource could be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        internal static MeshData Load(Stream stream, 
            bool expectFormatHeader)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("The specified stream is not " +
                    "readable.");

            if (expectFormatHeader)
            {
                ResourceType resourceType =
                    NativeFormatHandler.ReadEntryHeader(stream);
                if (resourceType != ResourceType.Mesh)
                    throw new FormatException("The specified resource " +
                        "was no mesh resource.");
            }

            uint vertexCount = stream.ReadUnsignedInteger();
            uint faceCount = stream.ReadUnsignedInteger();

            if (vertexCount < 3) throw new FormatException("The vertex " +
                "count was less than 3, which is too low.");
            if (faceCount < 1) throw new FormatException("The face " +
                "count was less than 1, which is too low.");
            /*if (vertexCount > MaxVertices) throw new FormatException(
                "The vertex count was larger than " + MaxVertices +
                ", which is too high.");
            if (faceCount > MaxFaces) throw new FormatException(
                "The face count was larger than " + MaxFaces +
                ", which is too high.");*/

            Vertex[] vertexArray = new Vertex[vertexCount];
            Face[] faceArray = new Face[faceCount];

            for (int i = 0; i < vertexArray.Length; i++)
                vertexArray[i] = stream.Read<Vertex>();

            for (int i = 0; i < faceArray.Length; i++)
                faceArray[i] = stream.Read<Face>();

            uint skeletonBufferSize = stream.ReadUnsignedInteger();
            byte[] skeletonBuffer = stream.ReadBuffer(skeletonBufferSize);

            VertexCollection vertices = new VertexCollection(vertexArray, 
                false);
            FaceCollection faces = new FaceCollection(faceArray, false);
            Skeleton skeleton = Skeleton.FromBuffer(skeletonBuffer);

            return new MemoryMesh(vertices, faces, skeleton.ToReadOnly(false));
        }

        /// <summary>
        /// Saves the current <see cref="MeshData"/> instance in the internal 
        /// format used by <see cref="NativeFormatHandler"/>.
        /// </summary>
        /// <param name="stream">
        /// The target stream.
        /// </param>
        /// <param name="includeFormatHeader">
        /// <c>true</c> to include the format header (as specified in
        /// <see cref="NativeFormatHandler"/>), <c>false</c> to start right
        /// off with the resource data.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="StreamWrapper.CanWrite"/> of 
        /// <paramref name="stream"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        internal void Save(Stream stream, bool includeFormatHeader)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("The specified stream is not " +
                    "writable.");

            if (includeFormatHeader)
                NativeFormatHandler.WriteEntryHeader(ResourceType.Mesh,
                    stream);

            stream.WriteUnsignedInteger((uint)VertexCount);
            stream.WriteUnsignedInteger((uint)FaceCount);
            for (int i = 0; i < VertexCount; i++)
                stream.Write(GetVertex(i));
            for (int i = 0; i < FaceCount; i++)
                stream.Write(GetFace(i));

            stream.WriteBuffer(Skeleton.ToBuffer(), true);
        }

        /// <summary>
        /// Creates a single-quad plane on the XY-axis with a Z position of 0 
        /// and its pivot in the center of the plane.
        /// </summary>
        /// <param name="dimensions">
        /// The dimensions of the plane, where the width is defined by
        /// <see cref="Vector2.X"/> and the height is defined by
        /// <see cref="Vector2.Y"/>.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshData"/> class.
        /// </returns>
        /// <remarks>
        /// The bottom-left edge of the texture coordinates is the bottom-left
        /// edge of the plane, the top-right edge of the texture coordinate is
        /// the top-right edge of the plane.
        /// </remarks>
        public static MeshData CreatePlane(Vector2 dimensions)
        {
            return CreatePlane(dimensions, 0);
        }

        /// <summary>
        /// Creates a single-quad plane on the XY-axis with its pivot in the 
        /// center of the plane.
        /// </summary>
        /// <param name="dimensions">
        /// The dimensions of the plane, where the width is defined by
        /// <see cref="Vector2.X"/> and the height is defined by
        /// <see cref="Vector2.Y"/>.
        /// </param>
        /// <param name="z">
        /// The Z position of the plane.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshData"/> class.
        /// </returns>
        /// <remarks>
        /// The bottom-left edge of the texture coordinates is the bottom-left
        /// edge of the plane, the top-right edge of the texture coordinate is
        /// the top-right edge of the plane.
        /// </remarks>
        public static MeshData CreatePlane(Vector2 dimensions, float z)
        {
            return CreatePlane(new Vector3(-dimensions / 2, z),
                new Vector3(-dimensions.X / 2, dimensions.Y / 2, z),
                new Vector3(dimensions.X / 2, -dimensions.Y / 2, z));
        }

        /// <summary>
        /// Creates a single-quad plane.
        /// </summary>
        /// <param name="bottomLeft">
        /// The position of the bottom-left edge of the plane.
        /// </param>
        /// <param name="topLeft">
        /// The position of the top-left edge of the plane.
        /// </param>
        /// <param name="bottomRight">
        /// The position of the bottom-right edge of the plane.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshData"/> class.
        /// </returns>
        public static MeshData CreatePlane(Vector3 bottomLeft, Vector3 topLeft,
            Vector3 bottomRight)
        {
            List<Vertex> vertices = new List<Vertex>();
            List<Face> faces = new List<Face>();

            CreatePlane(bottomLeft, topLeft, bottomRight, out _,
                Vector2.Zero, Vector2.One, vertices, faces);

            return new MemoryMesh(new VertexCollection(vertices.ToArray(),
                false), new FaceCollection(faces.ToArray(), false),
                Skeleton.Empty);
        }

        /// <summary>
        /// Creates a three-dimensional box with its pivot in the center of the
        /// box and an UV layout with that any assigned texture will be 
        /// displayed completely on each side of the box.
        /// </summary>
        /// <param name="dimensions">
        /// The <see cref="Vector3"/> instance specifying the dimensions 
        /// of the box, where <see cref="Vector3.X"/> specifies the
        /// width, <see cref="Vector3.Y"/> specifies the height and
        /// <see cref="Vector3.Z"/> specifies the depth.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshData"/> class.
        /// </returns>
        public static MeshData CreateBox(Vector3 dimensions)
        {
            return CreateBox(dimensions, false);
        }

        /// <summary>
        /// Creates a three-dimensional box with its pivot in the center 
        /// of the box.
        /// </summary>
        /// <param name="dimensions">
        /// The <see cref="Vector3"/> instance specifying the dimensions 
        /// of the box, where <see cref="Vector3.X"/> specifies the
        /// width, <see cref="Vector3.Y"/> specifies the height and
        /// <see cref="Vector3.Z"/> specifies the depth.
        /// </param>
        /// <param name="seperateUvSides">
        /// <c>true</c> to create an UV map where each side of the box has
        /// its own segment in the UV map, which looks a bit like a lowercase 
        /// "t" rotated 90 degrees counter-clockwise (see the remarks for more
        /// information), <c>false</c> to generate an UV map where each side
        /// of the box will have the same (full) texture.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshData"/> class.
        /// </returns>
        /// <remarks>
        /// The created <see cref="MeshData"/> will have a UV layout. The type
        /// of the UV layout can be controlled with the 
        /// <paramref name="seperateUvSides"/> parameter.
        /// The following graphic should clarify how the "t"-shaped UV layout
        /// looks like, which is used when <paramref name="seperateUvSides"/>
        /// is <c>true</c>. In the graphic, where each "boxed glyph" represents
        /// a 0.25x~0.33 unit of the UV map. 'B' represents the back side, 'L' 
        /// represents the left side, 'F' represents the front side, 'R' 
        /// represents the right side, 'U' represents the top and 'D' 
        /// represents the bottom side:
        /// <c>
        ///   |T|
        /// |B|L|F|R|
        ///   |D|
        /// </c>
        /// </remarks>
        public static MeshData CreateBox(Vector3 dimensions, 
            bool seperateUvSides)
        {
            List<Vertex> vertices = new List<Vertex>();
            List<Face> faces = new List<Face>();

            float width = dimensions.X;
            float height = dimensions.Y;
            float depth = dimensions.Z;
            float right = width / 2;
            float left = -right;
            float top = height / 2;
            float bottom = -top;
            float back = depth / 2;
            float front = -back;

            Vector2 seperatedUvSectionSize = new Vector2(1 / 4f, 1 / 3f);
            Vector2 singleUvSectionSize = Vector2.One;

            Vector3 leftBottomFront = new Vector3(left, bottom, front);
            Vector3 leftTopFront = new Vector3(left, top, front);
            Vector3 rightTopFront = new Vector3(right, top, front);
            Vector3 rightBottomFront = new Vector3(right, bottom, front);
            Vector3 leftBottomBack = new Vector3(left, bottom, back);
            Vector3 leftTopBack = new Vector3(left, top, back);
            Vector3 rightTopBack = new Vector3(right, top, back);
            Vector3 rightBottomBack = new Vector3(right, bottom, back);

            //The planes of the box are defined in the following order:
            //Back, left, front, right, top, bottom.
            CreatePlane(rightBottomBack, rightTopBack, leftBottomBack,
                seperateUvSides ? new Vector2(0 / 4f, 1 / 3f) : Vector2.Zero,
                seperateUvSides ? seperatedUvSectionSize : singleUvSectionSize,
                vertices, faces);
            CreatePlane(leftBottomBack, leftTopBack, leftBottomFront,
                seperateUvSides ? new Vector2(1 / 4f, 1 / 3f) : Vector2.Zero,
                seperateUvSides ? seperatedUvSectionSize : singleUvSectionSize,
                vertices, faces);
            CreatePlane(leftBottomFront, leftTopFront, rightBottomFront,
                seperateUvSides ? new Vector2(2 / 4f, 1 / 3f) : Vector2.Zero,
                seperateUvSides ? seperatedUvSectionSize : singleUvSectionSize,
                vertices, faces);
            CreatePlane(rightBottomFront, rightTopFront, rightBottomBack,
                seperateUvSides ? new Vector2(3 / 4f, 1 / 3f) : Vector2.Zero,
                seperateUvSides ? seperatedUvSectionSize : singleUvSectionSize,
                vertices, faces);
            CreatePlane(leftTopFront, leftTopBack, rightTopFront,
                seperateUvSides ? new Vector2(2 / 4f, 2 / 3f) : Vector2.Zero,
                seperateUvSides ? seperatedUvSectionSize : singleUvSectionSize,
                vertices, faces);
            CreatePlane(leftBottomBack, leftBottomFront, rightBottomBack,
                seperateUvSides ? new Vector2(2 / 4f, 0 / 3f) : Vector2.Zero,
                seperateUvSides ? seperatedUvSectionSize : singleUvSectionSize,
                vertices, faces);

            return new MemoryMesh(new VertexCollection(vertices.ToArray(),
                false), new FaceCollection(faces.ToArray(), false),
                Skeleton.Empty);
        }

        private static void CreatePlane(Vector3 bottomLeftMesh,
             Vector3 topLeftMesh, Vector3 bottomRightMesh,
             Vector2 bottomLeftUV, Vector2 dimensionsUV, 
             List<Vertex> targetVerticesList, List<Face> targetFaceList)
        {
            CreatePlane(bottomLeftMesh, topLeftMesh, bottomRightMesh,
                out _, bottomLeftUV, dimensionsUV, targetVerticesList,
                targetFaceList);
        }

        private static void CreatePlane(Vector3 bottomLeftMesh,
            Vector3 topLeftMesh, Vector3 bottomRightMesh,
            out Vector3 topRightMesh, Vector2 bottomLeftUV,
            Vector2 dimensionsUV, List<Vertex> targetVerticesList,
            List<Face> targetFaceList)
        {
            if (targetVerticesList == null)
                throw new ArgumentNullException(nameof(targetVerticesList));
            if (targetFaceList == null)
                throw new ArgumentNullException(nameof(targetFaceList));

            uint firstVertexIndex = (uint)targetVerticesList.Count;
            targetFaceList.Add(new Face(firstVertexIndex, firstVertexIndex + 1,
                firstVertexIndex + 2));
            targetFaceList.Add(new Face(firstVertexIndex + 2,
                firstVertexIndex + 3, firstVertexIndex));

            Vector3 normal = Vector3.Normalize(Vector3.Cross(topLeftMesh,
                bottomRightMesh));

            Vector3 z = bottomRightMesh - topLeftMesh;
            float p1 = Vector3.Dot(z, bottomLeftMesh);
            float r = (p1 - Vector3.Dot(z, topLeftMesh)) / Vector3.Dot(z, z);
            Vector3 sp = topLeftMesh + r * z;
            topRightMesh = bottomLeftMesh + 2f * (sp - bottomLeftMesh);

            targetVerticesList.Add(new Vertex(bottomLeftMesh, normal,
                bottomLeftUV));
            targetVerticesList.Add(new Vertex(topLeftMesh, normal,
                bottomLeftUV + new Vector2(0, dimensionsUV.Y)));
            targetVerticesList.Add(new Vertex(topRightMesh, normal,
                bottomLeftUV + dimensionsUV));
            targetVerticesList.Add(new Vertex(bottomRightMesh, normal,
                bottomLeftUV + new Vector2(dimensionsUV.X, 0)));
        }
    }
}
