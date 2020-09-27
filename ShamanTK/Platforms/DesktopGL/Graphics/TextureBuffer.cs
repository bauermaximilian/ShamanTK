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
using ShamanTK.IO;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;

namespace ShamanTK.Platforms.DesktopGL.Graphics
{
    /// <summary>
    /// Provides access to a texture buffer on the GPU.
    /// </summary>
    class TextureBuffer : ShamanTK.Graphics.TextureBuffer
    {
        /// <summary>
        /// Gets the default <see cref="TextureWrapMode"/>, which is used for
        /// every <see cref="ShamanTK.Graphics.TextureBuffer"/> in 
        /// <see cref="OpenTK.Graphics"/>.
        /// </summary>
        internal static TextureWrapMode DefaultTextureWrapMode { get; } 
            = TextureWrapMode.Repeat;

        /// <summary>
        /// Gets the handle of the texture buffer.
        /// </summary>
        public int Handle => IsDisposed ? 0 : handle;
        private readonly int handle;

        private TextureBuffer(int handle, Size size, TextureFilter filter) 
            : base(size, filter)
        {
            this.handle = handle;
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
        /// of this <see cref="ShamanTK.Graphics.TextureBuffer"/> instance.
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
        public override void Upload(TextureData source, int tx, int ty, 
            int width, int height)
        {
            VerifyUploadParameters(source, tx, ty, width, height);

            GL.BindTexture(TextureTarget.Texture2D, Handle);

            bool pointerUsed = false;
            if (source.SupportsPointers)
            {
                Type colorType = source.PixelDataPointer.ElementType;
                PixelFormat pixelFormat = PixelFormat.Red;

                if (colorType == typeof(Color))
                    pixelFormat = PixelFormat.Rgba;
                else if (colorType == typeof(Color.RGB))
                    pixelFormat = PixelFormat.Rgb;
                else if (colorType == typeof(Color.BGRA))
                    pixelFormat = PixelFormat.Bgra;
                else if (colorType == typeof(Color.BGR))
                    pixelFormat = PixelFormat.Bgr;

                if (pixelFormat != PixelFormat.Red)
                {
                    pointerUsed = true;

                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, tx,
                        ty, width, height, pixelFormat,
                        PixelType.UnsignedByte,
                        source.PixelDataPointer.GetElementAddress(
                            source.GetPixelIndex(tx, ty)));
                }
            }

            if (!pointerUsed)
            {
                Color[] pixels = source.GetPixels(tx, ty, width, height);

                GL.TexSubImage2D(TextureTarget.Texture2D, 0, tx,
                    ty, width, height, PixelFormat.Rgba,
                    PixelType.UnsignedByte, pixels);
            }

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Deletes the buffer.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            GL.DeleteTexture(Handle);
        }

        /// <summary>
        /// Creates a new, unintialized <see cref="TextureBuffer"/>.
        /// </summary>
        /// <param name="size">
        /// The size of the new <see cref="TextureBuffer"/>.
        /// </param>
        /// <param name="filter">
        /// The texture interpolation method, which should be used when 
        /// rendering a model with the texture buffer.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="TextureBuffer"/> class.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="Size.IsEmpty"/> of
        /// <paramref name="size"/> is <c>true</c> or when 
        /// <paramref name="filter"/> is invalid.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// Is thrown when there's not enough graphics memory left to create 
        /// a buffer of the specified size or when the specified buffer 
        /// exceeded platform-specific limits.
        /// </exception>
        public static TextureBuffer Create(Size size, TextureFilter filter)
        {
            if (size.IsEmpty)
                throw new ArgumentException("The specified size is empty.");

            //Convert the specified texture filter to its OpenGL equivalent
            TextureMagFilter magFilter;
            TextureMinFilter minFilter;
            if (filter == TextureFilter.Nearest)
            {
                magFilter = TextureMagFilter.Nearest;
                minFilter = TextureMinFilter.Nearest;
            }
            else if (filter == TextureFilter.Linear)
            {
                magFilter = TextureMagFilter.Linear;
                minFilter = TextureMinFilter.Linear;
            }
            else throw new ArgumentException("The specified texture " +
               "filter is invalid.");

            //Generate the texture on the GPU.
            int textureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureHandle);

            //Empty the error queue, initialize the texture and ensure that
            //the texture was initialized without raising an OutOfMemory error.
            while (GL.GetError() != ErrorCode.NoError) ;

            GL.TexImage2D(TextureTarget.Texture2D, 0,
                PixelInternalFormat.Rgba,
                size.Width, size.Height, 0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte, IntPtr.Zero);

            ErrorCode errorCode = GL.GetError();
            if (errorCode == ErrorCode.OutOfMemory)
                throw new OutOfMemoryException("There wasn't enough " +
                    "GPU memory left to store a texture of the specified " +
                    "dimensions.");
            else if (errorCode == ErrorCode.TextureTooLargeExt)
                throw new OutOfMemoryException("The dimensions of the " +
                    "specified texture exceeded the limits of the GPU.");

            //Set the parameters for the texture, as defined in the constructor
            //of the current generator.
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter, (int)magFilter);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureWrapS, 
                (int)DefaultTextureWrapMode);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureWrapT, 
                (int)DefaultTextureWrapMode);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            return new TextureBuffer(textureHandle, size, filter);
        }
    }
}
