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
    /// Describes the available methods of texture interpolation.
    /// </summary>
    public enum TextureFilter
    {
        /// <summary>
        /// A nearest neighbour interpolation mode is used.
        /// Useful for pixel art.
        /// </summary>
        Nearest,
        /// <summary>
        /// A linear interpolation mode is used.
        /// </summary>
        Linear
    }

    /// <summary>
    /// Provides access to a texture buffer on the GPU.
    /// </summary>
    public abstract class TextureBuffer : DisposableBase
    {
        /// <summary>
        /// Gets the size of the texture in the current buffer.
        /// </summary>
        public Size Size { get; }

        /// <summary>
        /// Gets the texture filtering method, which is used for the current
        /// texture buffer.
        /// </summary>
        public TextureFilter TextureFilter { get; }

        /// <summary>
        /// Gets the amount of pixels in the current buffer.
        /// </summary>
        public int PixelCount => Size.Area;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureBuffer"/>
        /// base class.
        /// </summary>
        /// <param name="size">The size of the texture buffer.</param>
        /// <param name="textureFilter">
        /// The texture interpolation method, which should be used when 
        /// rendering a model with the texture buffer.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="Size.IsEmpty"/> of 
        /// <paramref name="size"/> is <c>true</c> or when
        /// the specified <paramref name="textureFilter"/> is invalid.
        /// </exception>
        protected TextureBuffer(Size size, TextureFilter textureFilter)
            : base (Math.Max(1, size.Area * Color.Size))
        {
            if (size.IsEmpty)
                throw new ArgumentException("The specified size is empty.");

            if (!Enum.IsDefined(typeof(TextureFilter), textureFilter))
                throw new ArgumentException("The specified texture " +
                    "filtering method is invalid.");

            Size = size;
            TextureFilter = textureFilter;
        }

        /// <summary>
        /// Verifies the parameters for the 
        /// <see cref="Upload(TextureData, int, int, int, int)"/> method and
        /// throws an exception if one of the parameters violates the 
        /// requirements or the buffer is disposed. If all parameters are 
        /// valid and the buffer is not disposed, calling this method
        /// has no effect.
        /// </summary>
        /// <param name="source">The texture data.</param>
        /// <param name="tx">
        /// The X position of the area origin in texture coordinate space
        /// to be uploaded from the <paramref name="source"/> to this buffer,
        /// where the origin is in the top left corner and the X axis 
        /// "points right".
        /// </param>
        /// <param name="ty">
        /// The Y position of the requested pixel in texture coordinate space
        /// to be uploaded from the <paramref name="source"/> to this buffer,
        /// where the origin is in the top left corner and the Y axis
        /// "points downwards".
        /// </param>
        /// <param name="width">
        /// The width of the area.
        /// </param>
        /// <param name="height">
        /// The height of the area.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="source"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <see cref="TextureData.Width"/> and
        /// <see cref="TextureData.Height"/> of <paramref name="source"/>
        /// don't match with the <see cref="Width"/> and <see cref="Height"/>
        /// of this <see cref="TextureBuffer"/> instance.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="tx"/> or <paramref name="ty"/> are
        /// less than 0, or when the sum of <paramref name="tx"/> and
        /// <paramref name="width"/> is greater than <see cref="Width"/>,
        /// or when the sum of <paramref name="ty"/> and 
        /// <paramref name="height"/> is greater than <see cref="Height"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="TextureBuffer"/> was disposed
        /// and can't be used anymore.
        /// </exception>
        protected void VerifyUploadParameters(TextureData source, int tx, 
            int ty, int width, int height)
        {
            ThrowIfDisposed();

            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (tx < 0) throw new ArgumentOutOfRangeException(nameof(tx));
            if (ty < 0) throw new ArgumentOutOfRangeException(nameof(ty));
            if (width > Size.Width)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height > Size.Height)
                throw new ArgumentOutOfRangeException(nameof(height));
            if (source.Size.Width != Size.Width)
                throw new ArgumentException("The width of the specified " +
                    "texture data source doesn't match the width of the " +
                    "current buffer.");
            if (source.Size.Height != Size.Height)
                throw new ArgumentException("The height of the specified " +
                    "texture data source doesn't match the height of the " +
                    "current buffer.");
        }

        /// <summary>
        /// Uploads texture data to the current buffer.
        /// </summary>
        /// <param name="source">The texture data.</param>
        /// <param name="tx">
        /// The X position of the area origin in texture coordinate space
        /// to be uploaded from the <paramref name="source"/> to this buffer,
        /// where the origin is in the top left corner and the X axis 
        /// "points right".
        /// </param>
        /// <param name="ty">
        /// The Y position of the requested pixel in texture coordinate space
        /// to be uploaded from the <paramref name="source"/> to this buffer,
        /// where the origin is in the top left corner and the Y axis
        /// "points downwards".
        /// </param>
        /// <param name="width">
        /// The width of the area.
        /// </param>
        /// <param name="height">
        /// The height of the area.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="source"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <see cref="TextureData.Width"/> and
        /// <see cref="TextureData.Height"/> of <paramref name="source"/>
        /// don't match with the <see cref="Width"/> and <see cref="Height"/>
        /// of this <see cref="TextureBuffer"/> instance.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="tx"/> or <paramref name="ty"/> are
        /// less than 0, or when the sum of <paramref name="tx"/> and
        /// <paramref name="width"/> is greater than <see cref="Width"/>,
        /// or when the sum of <paramref name="ty"/> and 
        /// <paramref name="height"/> is greater than <see cref="Height"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="TextureBuffer"/> was disposed
        /// and can't be used anymore.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Is thrown when an error occurred during uploading the data.
        /// </exception>
        /// <remarks>
        /// To verify the parameters and throw the documented exceptions where
        /// necessary, the <see cref="VerifyUploadParameters(TextureData, int, 
        /// int, int, int)"/> method can be used.
        /// </remarks>
        public abstract void Upload(TextureData source, int tx, int ty,
            int width, int height);

        /// <summary>
        /// Uploads texture data to the current buffer.
        /// </summary>
        /// <param name="source">The texture data.</param>
        /// <param name="row">
        /// The current Y position of the "pixel row" which is uploaded.
        /// Gets incremented by the amount of uploaded pixel rows.
        /// </param>
        /// <param name="rowCountMax">
        /// The amount of pixel rows to be uploaded, if the bottom of the
        /// texture isn't reached before (with a lower value).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="source"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <see cref="TextureData.Width"/> and
        /// <see cref="TextureData.Height"/> of <paramref name="source"/>
        /// don't match with the <see cref="Width"/> and <see cref="Height"/>
        /// of this <see cref="TextureBuffer"/> instance.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="row"/> is less than 0 or greater
        /// than/equal to <see cref="Height"/>, or when 
        /// <paramref name="rowCountMax"/> is less than/equal to 0.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="TextureBuffer"/> was disposed
        /// and can't be used anymore.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Is thrown when an error occurred during uploading the data.
        /// </exception>
        public void Upload(TextureData source, ref int row, int rowCountMax)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (row < 0 || row >= Size.Height)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (rowCountMax <= 0)
                throw new ArgumentOutOfRangeException(nameof(rowCountMax));

            int clampedRowCount = Math.Min(source.Size.Height - row, 
                rowCountMax);
            if (clampedRowCount == 0) return;

            Upload(source, 0, row, Size.Width, clampedRowCount);

            row += clampedRowCount;
        }
    }
}
