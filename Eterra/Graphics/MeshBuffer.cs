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
using Eterra.IO;
using System;

namespace Eterra.Graphics
{
    /// <summary>
    /// Provides access to a mesh buffer with vertices and faces on the GPU.
    /// </summary>
    public abstract class MeshBuffer : DisposableBase
    {
        /// <summary>
        /// Gets the amount of vertices in the current buffer.
        /// </summary>
        public int VertexCount { get; }

        /// <summary>
        /// Gets the amount of faces in the current buffer.
        /// </summary>
        public int FaceCount { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshBuffer"/>
        /// base class.
        /// </summary>
        /// <param name="vertexCount">
        /// The amount of vertices stored in the <see cref="MeshBuffer"/>.
        /// </param>
        /// <param name="faceCount">
        /// The amount of faces stored in the <see cref="MeshBuffer"/>
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="vertexCount"/> is less than 3
        /// or when <paramref name="faceCount"/> is less than 1.
        /// </exception>
        protected MeshBuffer(int vertexCount, int faceCount)
            : base(Math.Max(1, vertexCount * Vertex.Size 
                + faceCount * Face.Size))
        {
            if (vertexCount < 3)
                throw new ArgumentOutOfRangeException("vertexCount");
            if (faceCount < 1)
                throw new ArgumentOutOfRangeException("faceCount");

            VertexCount = vertexCount;
            FaceCount = faceCount;
        }

        /// <summary>
        /// Verifies the parameters for the 
        /// <see cref="UploadVertices(MeshData, int, int)"/> method and
        /// throws an exception if one of the parameters violates the 
        /// requirements or the buffer is disposed. If all parameters are 
        /// valid and the buffer is not disposed, calling this method
        /// has no effect.
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
        protected void VerifyVertexUploadParameters(MeshData source, 
            int offset, int count)
        {
            ThrowIfDisposed();

            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count + offset > VertexCount)
                throw new ArgumentOutOfRangeException("The offset and " +
                    "count exceeded the amount of available vertices.");
            if (source.VertexCount != VertexCount)
                throw new ArgumentException("The vertex count of the " +
                    "specified mesh data source doesn't match the vertex " +
                    "count of the current buffer.");
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
        /// <remarks>
        /// To verify the parameters and throw the documented exceptions where
        /// necessary, the 
        /// <see cref="VerifyVertexUploadParameters(MeshData, int, int)"/> 
        /// method can be used.
        /// </remarks>
        public abstract void UploadVertices(MeshData source, int offset, 
            int count);

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
        public void UploadVertices(MeshData source, ref int currentOffset,
            int countMax)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (currentOffset < 0 || currentOffset >= VertexCount)
                throw new ArgumentOutOfRangeException(nameof(currentOffset));
            if (countMax <= 0)
                throw new ArgumentOutOfRangeException(nameof(countMax));

            int clampedCount = Math.Min(source.VertexCount -
                currentOffset, countMax);
            if (clampedCount == 0) return;

            UploadVertices(source, currentOffset, clampedCount);
            currentOffset += clampedCount;
        }

        /// <summary>
        /// Verifies the parameters for the 
        /// <see cref="UploadFaces(MeshData, int, int)"/> method and
        /// throws an exception if one of the parameters violates the 
        /// requirements or the buffer is disposed. If all parameters are 
        /// valid and the buffer is not disposed, calling this method
        /// has no effect.
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
        /// Is thrown when <see cref="MeshData.FaceCount"/> of 
        /// <paramref name="source"/> doesn't match the 
        /// <see cref="FaceCount"/> of the current <see cref="MeshBuffer"/>
        /// instance.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="offset"/> is less than 0 or greater
        /// than/equal to <see cref="FaceCount"/> or
        /// <see cref="MeshData.FaceCount"/>, or when 
        /// <paramref name="count"/> is less than/equal to 0, 
        /// or when the sum of <paramref name="offset"/> and 
        /// <paramref name="count"/> is greater than <see cref="FaceCount"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="MeshBuffer"/> was disposed
        /// and can't be used anymore.
        /// </exception>
        protected void VerifyFaceUploadParameters(MeshData source,
            int offset, int count)
        {
            ThrowIfDisposed();

            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count + offset > FaceCount)
                throw new ArgumentOutOfRangeException("The offset and " +
                    "count exceeded the amount of available faces.");
            if (source.FaceCount != FaceCount)
                throw new ArgumentException("The face count of the " +
                    "specified mesh data source doesn't match the face " +
                    "count of the current buffer.");
        }

        /// <summary>
        /// Writes face data to the current buffer.
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
        /// Is thrown when <see cref="MeshData.FaceCount"/> of 
        /// <paramref name="source"/> doesn't match the 
        /// <see cref="FaceCount"/> of the current <see cref="MeshBuffer"/>
        /// instance.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="offset"/> is less than 0 or greater
        /// than/equal to <see cref="FaceCount"/> or
        /// <see cref="MeshData.FaceCount"/> or when 
        /// <paramref name="count"/> is less than/equal to 0,
        /// or when the sum of <paramref name="offset"/> and 
        /// <paramref name="count"/> is greater than <see cref="FaceCount"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="MeshBuffer"/> was disposed
        /// and can't be used anymore.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Is thrown when an error occurred during uploading the data.
        /// </exception>
        /// <remarks>
        /// To verify the parameters and throw the documented exceptions where
        /// necessary, the 
        /// <see cref="VerifyFaceUploadParameters(MeshData, int, int)"/> 
        /// method can be used.
        /// </remarks>
        public abstract void UploadFaces(MeshData source, int offset, 
            int count);

        /// <summary>
        /// Writes face data to the current buffer.
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
        /// Is thrown when <see cref="MeshData.FaceCount"/> of 
        /// <paramref name="source"/> doesn't match the 
        /// <see cref="FaceCount"/> of the current <see cref="MeshBuffer"/>
        /// instance.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="currentOffset"/> is less than 0 or 
        /// greater than/equal to <see cref="FaceCount"/>, or when 
        /// <paramref name="countMax"/> is less than/equal to 0.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="MeshBuffer"/> was disposed
        /// and can't be used anymore.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Is thrown when an error occurred during uploading the data.
        /// </exception>
        public void UploadFaces(MeshData source, ref int currentOffset,
            int countMax)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (currentOffset < 0 || currentOffset >= FaceCount)
                throw new ArgumentOutOfRangeException(nameof(currentOffset));
            if (countMax <= 0)
                throw new ArgumentOutOfRangeException(nameof(countMax));

            int clampedCount = Math.Min(source.FaceCount -
                currentOffset, countMax);
            if (clampedCount == 0) return;

            UploadFaces(source, currentOffset, clampedCount);
            currentOffset += clampedCount;
        }
    }
}
