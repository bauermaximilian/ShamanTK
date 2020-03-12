/*
 * Eterra Framework Platforms
 * Eterra platform providers for various operating systems and devices.
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Eterra.Common;
using Eterra.IO;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;

namespace Eterra.Platforms.Windows.Graphics
{
    /// <summary>
    /// Provides access to a mesh buffer with vertices and faces on the GPU.
    /// </summary>
    class MeshBuffer : Eterra.Graphics.MeshBuffer
    {
        /// <summary>
        /// Gets the handle to the vertex array object, which defines this
        /// mesh.
        /// </summary>
        public int Handle => IsDisposed ? 0 : vertexArrayObjectHandle;

        /// <summary>
        /// Gets the shader this <see cref="MeshBuffer"/> was initialized for.
        /// </summary>
        internal Shader Shader { get; }

        private readonly int vertexBufferHandle, faceBufferHandle,
            vertexArrayObjectHandle;

        private MeshBuffer(Shader shader, int vertexBufferHandle, 
            int faceBufferHandle, int vertexArrayObjectHandle, int vertexCount,
            int faceCount) : base(vertexCount, faceCount)
        {
            Shader = shader ?? throw new ArgumentNullException(nameof(shader));
            this.vertexBufferHandle = vertexBufferHandle;
            this.faceBufferHandle = faceBufferHandle;
            this.vertexArrayObjectHandle = vertexArrayObjectHandle;
        }

        /// <summary>
        /// Writes vertex data to the current buffer.
        /// </summary>
        /// <param name="source">The mesh data.</param>
        /// <param name="offset">
        /// The start index, from which the element upload should be started.
        /// </param>
        /// <param name="count">
        /// The maximum amount of elements to be uploaded.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <see cref="MeshData"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="MeshData.VertexCount"/> of 
        /// <paramref name="source"/> doesn't match the 
        /// <see cref="VertexCount"/> of the current <see cref="MeshBuffer"/>
        /// instance.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="offset"/> is less than 0 or greater
        /// than/equal to <see cref="VertexCount"/> or
        /// <see cref="MeshData.VertexCount"/>, or when 
        /// <paramref name="count"/> is less than/equal to 0, 
        /// or when the sum of <paramref name="offset"/> and 
        /// <paramref name="count"/> is greater than <see cref="VertexCount"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="MeshBuffer"/> was disposed
        /// and can't be used anymore.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Is thrown when an error occurred during uploading the data.
        /// </exception>
        public override void UploadVertices(MeshData source, int offset, 
            int count)
        {
            VerifyVertexUploadParameters(source, offset, count);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);

            Vertex[] vertices = source.GetVertices(offset, count);

            GL.BufferSubData(BufferTarget.ArrayBuffer, 
                (IntPtr)(offset * Vertex.Size), count * Vertex.Size, vertices);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// Writes vertex data to the current buffer.
        /// </summary>
        /// <param name="source">The mesh data.</param>
        /// <param name="currentOffset">
        /// The current index offset, from which the element upload should be 
        /// started. This value is incremented by the amount of 
        /// uploaded elements.
        /// </param>
        /// <param name="countMax">
        /// The maximum amount of elements to be uploaded, if the end of the
        /// element collection isn't reached before.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <see cref="MeshData"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="MeshData.VertexCount"/> of 
        /// <paramref name="source"/> doesn't match the 
        /// <see cref="VertexCount"/> of the current <see cref="MeshBuffer"/>
        /// instance.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="currentOffset"/> is less than 0 or 
        /// greater than/equal to <see cref="VertexCount"/>, or when 
        /// <paramref name="count"/> is less than/equal to 0.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="MeshBuffer"/> was disposed
        /// and can't be used anymore.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Is thrown when an error occurred during uploading the data.
        /// </exception>
        public override void UploadFaces(MeshData source, int offset, 
            int count)
        {
            VerifyFaceUploadParameters(source, offset, count);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, faceBufferHandle);

            Face[] faces = source.GetFaces(offset, count);

            GL.BufferSubData(BufferTarget.ElementArrayBuffer,
                (IntPtr)(offset * Face.Size), count * Face.Size, faces);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        /// <summary>
        /// Deletes the buffers and the vertex array object.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (GraphicsContext.CurrentContext != null
                && !GraphicsContext.CurrentContext.IsDisposed)
            {
                GL.DeleteBuffer(vertexBufferHandle);
                GL.DeleteBuffer(faceBufferHandle);
                GL.DeleteVertexArray(vertexArrayObjectHandle);
            }
        }

        /// <summary>
        /// Creates a new, uninitialized <see cref="MeshBuffer"/>.
        /// </summary>
        /// <param name="vertexCount">
        /// The amount of vertices of the new <see cref="MeshBuffer"/>.
        /// </param>
        /// <param name="faceCount">
        /// The amount of faces of the new <see cref="MeshBuffer"/>.
        /// </param>
        /// <param name="shader">
        /// The <see cref="Shader"/> instance, which will use this mesh.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshBuffer"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="shader"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="vertexCount"/> is less than 3
        /// or when <paramref name="faceCount"/> is less than 1.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// Is thrown when there's not enough graphics memory left to create 
        /// a buffer of the specified size or when the specified buffer 
        /// exceeded platform-specific limits.
        /// </exception>
        public static MeshBuffer Create(int vertexCount, int faceCount,
            Shader shader)
        {
            if (shader == null)
                throw new ArgumentNullException(nameof(shader));

            //Create and bind a vertex array object, which stores the following
            //vertex/face buffer handles and vertex attributes to be used when 
            //the mesh is drawn later.
            int vertexArrayObjectHandle = GL.GenVertexArray();
            GL.BindVertexArray(vertexArrayObjectHandle);

            //Generate a vertex buffer on the GPU.
            int vertexBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);

            //Empty the error queue, initialize the buffer and ensure that
            //the buffer was initialized without raising an OutOfMemory error.
            while (GL.GetError() != ErrorCode.NoError) ;

            GL.BufferData(BufferTarget.ArrayBuffer, Vertex.Size * vertexCount,
                    IntPtr.Zero, BufferUsageHint.DynamicDraw);

            ErrorCode errorCode = GL.GetError();
            if (errorCode == ErrorCode.OutOfMemory)
                throw new OutOfMemoryException("There wasn't enough " +
                    "GPU memory left to store a mesh with the specified " +
                    "vertex count.");

            //Generate an index buffer on the GPU.
            int faceBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, faceBufferHandle);

            //Initialize the buffer and ensure that the buffer was initialized 
            //without raising an OutOfMemory error.
            GL.BufferData(BufferTarget.ElementArrayBuffer, Face.Size 
                * faceCount, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            //Configure the buffer attributes, which makes the buffer 
            //content "understandable" for the shader and pipes the data
            //into the various variables with the "in"-prefix.
            shader.InitializeVertexAttribPointers();

            //Finish up the initialisation of the new vertex array object by
            //unbinding the vertex array.
            GL.BindVertexArray(0);

            //Unbind the other buffers and return the new MeshBufferGL.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            return new MeshBuffer(shader, vertexBufferHandle, faceBufferHandle,
                vertexArrayObjectHandle, vertexCount, faceCount);
        }
    }
}
