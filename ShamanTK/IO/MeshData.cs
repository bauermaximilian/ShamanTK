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

using ShamanTK.Common;
using ShamanTK.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace ShamanTK.IO
{
    /// <summary>
    /// Provides the base class from which meshes are derived.
    /// </summary>
    public abstract class MeshData : DisposableBase
    {
        #region Internally used Mesh implementation.
        internal class MemoryMesh : MeshData
        {
            private Vertex[] vertexData;

            private Face[] faceData;

            public MemoryMesh(Vertex[] vertexData, Face[] faceData,
                VertexPropertyDataFormat vertexPropertyDataFormat,
                Skeleton skeleton) 
                : base(vertexData?.Length ?? 1, faceData?.Length ?? 1,
                      vertexPropertyDataFormat, skeleton)
            {
                this.vertexData = vertexData
                    ?? throw new ArgumentNullException(nameof(vertexData));
                this.faceData = faceData
                    ?? throw new ArgumentNullException(nameof(faceData));
            }

            public override Vertex[] GetVertices(int index, int length)
            {
                ThrowIfDisposed();

                ValidateVertexRange(index, length);

                Vertex[] output = new Vertex[length];
                Array.ConstrainedCopy(vertexData, index, output, 0, length);
                return output;
            }

            public override Face[] GetFaces(int index, int length)
            {
                ThrowIfDisposed();

                ValidateFaceRange(index, length);

                Face[] output = new Face[length];
                Array.ConstrainedCopy(faceData, index, output, 0, length);
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
        /// Gets a single-quad plane on the XY-axis with a width and height of 
        /// 1, its pivot in the bottom left corner of the plane and 
        /// -<see cref="Vector3.UnitZ"/> as normal.
        /// </summary>
        public static MeshData Quad { get; } = CreatePlane(Vector3.Zero,
            Vector3.UnitY, Vector3.UnitX);

        /// <summary>
        /// Gets a single-quad plane on the XY-axis with a width and height of
        /// 1, it's pivot in the center of the left side of the plane and 
        /// -<see cref="Vector3.UnitZ"/> as normal.
        /// </summary>
        public static MeshData LineQuad { get; } = CreatePlane(
            new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0),
            new Vector3(1, -0.5f, 0));

        /// <summary>
        /// Gets a three-dimensional cube with a width, height and depth of 1,
        /// its pivot in the center of the box and an UV layout with that any 
        /// assigned texture will be displayed completely on each side of 
        /// the cube.
        /// </summary>
        public static MeshData Box { get; } = CreateBox(Vector3.One, true);

        /// <summary>
        /// Gets a three-dimensional cube with a width, height and depth of 1,
        /// its pivot in the center of the box, an "t"-shaped UV layout 
        /// (tilted 90° clockwise, 4:3-format) and inverted normals.
        /// the cube.
        /// </summary>
        public static MeshData Skybox { get; } = 
            CreateBox(Vector3.One, true, true, true);

        /// <summary>
        /// Gets a three-dimensional cube with a width, height and depth of 1,
        /// its pivot in the left, bottom, front edge and an UV layout with 
        /// that any assigned texture will be displayed completely on each 
        /// side of the cube.
        /// </summary>
        public static MeshData Block { get; } = CreateBox(Vector3.One, false);

        /// <summary>
        /// Gets the amount of vertices the current <see cref="MeshData"/> has.
        /// </summary>
        public int VertexCount { get; }

        /// <summary>
        /// Gets the amount of faces the current <see cref="MeshData"/> has.
        /// </summary>
        public int FaceCount { get; }

        /// <summary>
        /// Gets the <see cref="Common.Skeleton"/> of the mesh or null, if 
        /// either the current <see cref="MeshData"/> instance doesn't contain
        /// any deformer attachments (as specified by 
        /// <see cref="VertexPropertyDataFormat"/>) or when the 
        /// <see cref="Deformer"/>s are created manually.
        /// </summary>
        public Skeleton Skeleton { get; }

        /// <summary>
        /// Gets a value indicating whether the current <see cref="MeshData"/>
        /// instance contains a <see cref="Common.Skeleton"/> that can be used 
        /// to put any existing <see cref="DeformerAttachments"/> in the
        /// <see cref="VertexPropertyData"/> of the vertices in a hierarchical 
        /// relationship (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool HasSkeleton => Skeleton != null;

        /// <summary>
        /// Gets the current <see cref="Common.VertexPropertyDataFormat"/>,
        /// which defines how the <see cref="VertexPropertyData"/> of the
        /// contained <see cref="Vertex"/> data is interpreted.
        /// </summary>
        public VertexPropertyDataFormat VertexPropertyDataFormat { get; }

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
        /// <param name="vertexPropertyDataFormat">
        /// Specifies the format of the <see cref="VertexPropertyData"/> of
        /// the <see cref="Vertex"/> instances in the new 
        /// <see cref="MeshData"/> instance. 
        /// Can be <see cref="VertexPropertyDataFormat.None"/> if the
        /// <see cref="VertexPropertyData"/> doesn't contain any meaningful
        /// data.
        /// </param>
        /// <param name="skeleton">
        /// A <see cref="Common.Skeleton"/> instance. Can be null.
        /// See <see cref="Skeleton"/> and <see cref="HasSkeleton"/> for more 
        /// information.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="vertexCount"/> is less than 1,
        /// when <paramref name="faceCount"/> is less than 1, or when
        /// <paramref name="vertexPropertyDataFormat"/> is invalid.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when no value for <paramref name="skeleton"/> is 
        /// provided, but the <paramref name="vertexPropertyDataFormat"/> is
        /// <see cref="VertexPropertyDataFormat.DeformerAttachments"/>.
        /// </exception>
        protected MeshData(int vertexCount, int faceCount,
            VertexPropertyDataFormat vertexPropertyDataFormat,
            Skeleton skeleton)
        {
            if (vertexCount < 1)
                throw new ArgumentException("The amount of vertices in a " +
                    "mesh must not be less than 1.");
            if (faceCount < 1)
                throw new ArgumentException("The amount of faces in a " +
                    "mesh must not be less than 1.");
            if (!Enum.IsDefined(typeof(VertexPropertyDataFormat),
                vertexPropertyDataFormat))
                throw new ArgumentException("The specified vertex property " +
                    "data format is invalid.");

            VertexCount = vertexCount;
            FaceCount = faceCount;

            if (skeleton != null)
            {
                if (!skeleton.IsReadOnly)
                    throw new ArgumentException("The specified skeleton " +
                        "isn't read-only and can't be used like that.");
                else Skeleton = skeleton;
            }
            else
            {
                Skeleton = null;
                if (vertexPropertyDataFormat ==
                    VertexPropertyDataFormat.DeformerAttachments)
                    throw new NotSupportedException("A skeleton must be " +
                        "provided for the vertex property data format '" +
                        nameof(VertexPropertyDataFormat.DeformerAttachments) +
                        "'.");
            }

            VertexPropertyDataFormat = vertexPropertyDataFormat;
        }

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
        public abstract Vertex[] GetVertices(int index, int length);

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
        public abstract Face[] GetFaces(int index, int length);

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
        /// Creates a new <see cref="MeshData"/> instance from existing 
        /// mesh data.
        /// </summary>
        /// <param name="vertices">The vertices of the mesh.</param>
        /// <param name="faces">The faces of the mesh.</param>
        /// <returns>
        /// A new instance of the <see cref="MeshData"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="vertices"/> or
        /// <paramref name="faces"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="vertices"/> or
        /// <paramref name="faces"/> are empty.
        /// </exception>
        public static MeshData Create(Vertex[] vertices, Face[] faces)
        {
            try
            {
                return new MemoryMesh(vertices, faces,
                    VertexPropertyDataFormat.None, null);
            }
            catch (ArgumentNullException) { throw; }
            catch (ArgumentException) { throw; }
        }

        /// <summary>
        /// Creates a new <see cref="MeshData"/> instance from existing 
        /// mesh data.
        /// </summary>
        /// <param name="vertices">The vertices of the mesh.</param>
        /// <param name="faces">The faces of the mesh.</param>
        /// <param name="skeleton">
        /// The skeleton of the mesh, which - in combination with the
        /// <see cref="VertexPropertyData"/> of the vertices, which will be
        /// interpreted as <see cref="DeformerAttachments"/> - can be used to
        /// deform the mesh when rendering.
        /// See <see cref="Skeleton"/> for more information.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshData"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="vertices"/>,
        /// <paramref name="faces"/> or <paramref name="skeleton"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="vertices"/> or
        /// <paramref name="faces"/> are empty, or when 
        /// <see cref="Node{T}.IsReadOnly"/> of <paramref name="skeleton"/> is
        /// <c>false</c>.
        /// </exception>
        public static MeshData Create(Vertex[] vertices, Face[] faces, 
            Skeleton skeleton)
        {
            try
            {
                return new MemoryMesh(vertices, faces,
                    VertexPropertyDataFormat.DeformerAttachments, skeleton);
            }
            catch (ArgumentNullException) { throw; }
            catch (ArgumentException) { throw; }
        }

        /// <summary>
        /// Creates a new <see cref="MeshData"/> instance from existing 
        /// mesh data.
        /// </summary>
        /// <param name="vertices">The vertices of the mesh.</param>
        /// <param name="faces">The faces of the mesh.</param>
        /// <param name="vertexPropertyDataFormat">
        /// The format of the <see cref="VertexPropertyData"/> of the
        /// specified <paramref name="vertices"/>. 
        /// To create a <see cref="MeshData"/> instance that supports 
        /// deforming, use the method 
        /// <see cref="Create(Vertex[], Face[], Skeleton)"/> instead, or an
        /// <see cref="NotSupportedException"/> will be thrown.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshData"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="vertices"/> or
        /// <paramref name="faces"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="vertices"/> or
        /// <paramref name="faces"/> are empty, or when
        /// <paramref name="vertexColorPrimary"/> or 
        /// <paramref name="vertexColorSecondary"/> are invalid.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <paramref name="vertexPropertyDataFormat"/> is
        /// <see cref="VertexPropertyDataFormat.DeformerAttachments"/>.
        /// This is not supported by this method - the overload
        /// <see cref="Create(Vertex[], Face[], Skeleton)"/> should be used 
        /// for this instead.
        /// </exception>
        public static MeshData Create(Vertex[] vertices, Face[] faces,
            VertexPropertyDataFormat vertexPropertyDataFormat)
        {
            if (vertexPropertyDataFormat ==
                VertexPropertyDataFormat.DeformerAttachments)
                throw new NotSupportedException("A mesh data instance " +
                    "with deformer attachments can not be created without " +
                    "a skeleton.");

            try
            {
                return new MemoryMesh(vertices, faces, 
                    vertexPropertyDataFormat, null);
            }
            catch (ArgumentNullException) { throw; }
            catch (ArgumentException) { throw; }
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
        /// Is thrown when <see cref="Stream.CanRead"/> of 
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
        internal static MeshData Load(Stream stream, bool expectFormatHeader)
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

            if (vertexCount < 1) throw new FormatException("The vertex " +
                "count was less than 1, which is too low.");
            if (faceCount < 1) throw new FormatException("The face " +
                "count was less than 1, which is too low.");

            Vertex[] vertices = new Vertex[vertexCount];
            Face[] faces = new Face[faceCount];

            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = stream.Read<Vertex>();

            for (int i = 0; i < faces.Length; i++)
                faces[i] = stream.Read<Face>();

            VertexPropertyDataFormat vertexPropertyDataFormat = 
                stream.ReadEnum<VertexPropertyDataFormat>();

            Skeleton skeleton = null;

            byte[] skeletonBuffer = stream.ReadBuffer();

            if (skeletonBuffer.Length > 0)
                skeleton = Skeleton.FromBuffer(skeletonBuffer).ToReadOnly(
                    false);

            if (skeleton == null && vertexPropertyDataFormat ==
                VertexPropertyDataFormat.DeformerAttachments)
                throw new FormatException("The vertex property format '" +
                    nameof(VertexPropertyDataFormat.DeformerAttachments) +
                    "' requires a skeleton to be valid.");

            return new MemoryMesh(vertices, faces, vertexPropertyDataFormat,
                skeleton);
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
        /// <exception cref="NotSupportedException">
        /// Is thrown when the <see cref="VertexPropertyDataFormat"/> is 
        /// specified as 
        /// <see cref="VertexPropertyDataFormat.DeformerAttachments"/>, but
        /// <see cref="HasSkeleton"/> is <c>false</c>.
        /// </exception>
        internal void Save(Stream stream, bool includeFormatHeader)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("The specified stream is not " +
                    "writable.");
            if (!HasSkeleton && VertexPropertyDataFormat ==
                VertexPropertyDataFormat.DeformerAttachments)
                throw new NotSupportedException("A mesh without a skeleton " +
                    "can't be exported to have the vertex property data " +
                    "format '" +
                    nameof(VertexPropertyDataFormat.DeformerAttachments) +
                    "'.");

            if (includeFormatHeader)
                NativeFormatHandler.WriteEntryHeader(ResourceType.Mesh,
                    stream);

            stream.WriteUnsignedInteger((uint)VertexCount);
            stream.WriteUnsignedInteger((uint)FaceCount);
            for (int i = 0; i < VertexCount; i++)
                stream.Write(GetVertices(i, 1)[0]);
            for (int i = 0; i < FaceCount; i++)
                stream.Write(GetFaces(i, 1)[0]);

            stream.WriteEnum(VertexPropertyDataFormat);

            byte[] skeletonBuffer;
            if (HasSkeleton) skeletonBuffer = Skeleton.ToBuffer();
            else skeletonBuffer = new byte[0];

            stream.WriteBuffer(skeletonBuffer, true);
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
        /// <param name="flipNormals">
        /// <c>false</c> to create a normal plane mesh with the normal of the
        /// plane faces being equal to the up vector created with the specified
        /// vectors, <c>true</c> to flip the vertex indicies in the faces 
        /// and invert the normal of the faces into the opposite direction.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshData"/> class.
        /// </returns>
        public static MeshData CreatePlane(Vector3 bottomLeft, Vector3 topLeft,
            Vector3 bottomRight, bool flipNormals = false)
        {
            List<Vertex> vertices = new List<Vertex>();
            List<Face> faces = new List<Face>();

            CreatePlane(bottomLeft, topLeft, bottomRight, out _,
                Vector2.Zero, Vector2.One, flipNormals, vertices, faces);

            return new MemoryMesh(vertices.ToArray(), faces.ToArray(),
                VertexPropertyDataFormat.None, null);
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
        /// <param name="centerOrigin">
        /// <c>true</c> to position the origin of the created mesh in its 
        /// center, <c>false</c> to set the origin of the box to its left,
        /// front, bottom edge.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshData"/> class.
        /// </returns>
        public static MeshData CreateBox(Vector3 dimensions, bool centerOrigin)
        {
            return CreateBox(dimensions, centerOrigin, false);
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
        /// <param name="centerOrigin">
        /// <c>true</c> to position the origin of the created mesh in its 
        /// center, <c>false</c> to set the origin of the box to its left,
        /// front, bottom edge.
        /// </param>
        /// <param name="seperateUvSides">
        /// <c>true</c> to create an UV map where each side of the box has
        /// its own segment in the UV map, which looks a bit like a lowercase 
        /// "t" rotated 90 degrees clockwise (see the remarks for more
        /// information), <c>false</c> to generate an UV map where each side
        /// of the box will have the same (full) texture.
        /// </param>
        /// <param name="flipNormals">
        /// <c>false</c> to create a normal cube with its normals pointing to
        /// the outside (default), <c>true</c> to invert the normals - the
        /// latter is especially required for skyboxes with enabled backface
        /// culling.
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
        ///     |T|
        /// |B|L|F|R|
        ///     |D|
        /// </c>
        /// </remarks>
        public static MeshData CreateBox(Vector3 dimensions, bool centerOrigin,
            bool seperateUvSides, bool flipNormals = false)
        {
            List<Vertex> vertices = new List<Vertex>();
            List<Face> faces = new List<Face>();

            float width = dimensions.X;
            float height = dimensions.Y;
            float depth = dimensions.Z;

            float right, left, top, bottom, back, front;
            if (centerOrigin)
            {
                right = width / 2;
                left = -right;
                top = height / 2;
                bottom = -top;
                back = depth / 2;
                front = -back;
            }
            else
            {
                right = width;
                left = 0;
                top = height;
                bottom = 0;
                back = depth;
                front = 0;
            }

            Vector2 seperatedUvSectionSize = new Vector2(1 / 4f, 1 / 3f);
            Vector2 singleUvSectionSize = Vector2.One;

            Vector3 leftBtmFront = new Vector3(left, bottom, front);
            Vector3 leftTopFront = new Vector3(left, top, front);
            Vector3 rightTopFront = new Vector3(right, top, front);
            Vector3 rightBtmFront = new Vector3(right, bottom, front);
            Vector3 leftBtmBack = new Vector3(left, bottom, back);
            Vector3 leftTopBack = new Vector3(left, top, back);
            Vector3 rightTopBack = new Vector3(right, top, back);
            Vector3 rightBtmBack = new Vector3(right, bottom, back);

            //The planes of the box are defined in the following order:
            //Back, left, front, right, top, bottom.
            CreatePlane(rightBtmBack, rightTopBack, leftBtmBack,
                seperateUvSides ? new Vector2(0 / 4f, 1 / 3f) : Vector2.Zero,
                seperateUvSides ? seperatedUvSectionSize : singleUvSectionSize,
                flipNormals, vertices, faces);
            CreatePlane(leftBtmBack, leftTopBack, leftBtmFront,
                seperateUvSides ? new Vector2(1 / 4f, 1 / 3f) : Vector2.Zero,
                seperateUvSides ? seperatedUvSectionSize : singleUvSectionSize,
                flipNormals, vertices, faces);
            CreatePlane(leftBtmFront, leftTopFront, rightBtmFront,
                seperateUvSides ? new Vector2(2 / 4f, 1 / 3f) : Vector2.Zero,
                seperateUvSides ? seperatedUvSectionSize : singleUvSectionSize,
                flipNormals, vertices, faces);
            CreatePlane(rightBtmFront, rightTopFront, rightBtmBack,
                seperateUvSides ? new Vector2(3 / 4f, 1 / 3f) : Vector2.Zero,
                seperateUvSides ? seperatedUvSectionSize : singleUvSectionSize,
                flipNormals, vertices, faces);
            CreatePlane(leftTopFront, leftTopBack, rightTopFront,
                seperateUvSides ? new Vector2(2 / 4f, 2 / 3f) : Vector2.Zero,
                seperateUvSides ? seperatedUvSectionSize : singleUvSectionSize,
                flipNormals, vertices, faces);
            CreatePlane(leftBtmBack, leftBtmFront, rightBtmBack,
                seperateUvSides ? new Vector2(2 / 4f, 0 / 3f) : Vector2.Zero,
                seperateUvSides ? seperatedUvSectionSize : singleUvSectionSize,
                flipNormals, vertices, faces);

            return new MemoryMesh(vertices.ToArray(), faces.ToArray(),
                VertexPropertyDataFormat.None, null);
        }

        private static void CreatePlane(Vector3 bottomLeftMesh,
             Vector3 topLeftMesh, Vector3 bottomRightMesh,
             Vector2 bottomLeftUV, Vector2 dimensionsUV, 
             bool flipNormals,
             List<Vertex> targetVerticesList, List<Face> targetFaceList)
        {
            CreatePlane(bottomLeftMesh, topLeftMesh, bottomRightMesh,
                out _, bottomLeftUV, dimensionsUV, flipNormals,
                targetVerticesList, targetFaceList);
        }

        private static void CreatePlane(Vector3 bottomLeftMesh,
            Vector3 topLeftMesh, Vector3 bottomRightMesh,
            out Vector3 topRightMesh, Vector2 bottomLeftUV,
            Vector2 dimensionsUV, bool flipNormals,
            List<Vertex> targetVerticesList,
            List<Face> targetFaceList)
        {
            if (targetVerticesList == null)
                throw new ArgumentNullException(nameof(targetVerticesList));
            if (targetFaceList == null)
                throw new ArgumentNullException(nameof(targetFaceList));

            uint firstVertexIndex = (uint)targetVerticesList.Count;

            if (flipNormals)
            {
                targetFaceList.Add(new Face(firstVertexIndex,
                    firstVertexIndex + 2, firstVertexIndex + 1));
                targetFaceList.Add(new Face(firstVertexIndex,
                    firstVertexIndex + 3, firstVertexIndex + 2));
            }
            else
            {
                targetFaceList.Add(new Face(firstVertexIndex,
                    firstVertexIndex + 1, firstVertexIndex + 2));
                targetFaceList.Add(new Face(firstVertexIndex,
                    firstVertexIndex + 2, firstVertexIndex + 3));
            }

            Vector3 normal = Vector3.Normalize(Vector3.Cross(topLeftMesh,
                bottomRightMesh));
            if (flipNormals) normal *= -1;

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
