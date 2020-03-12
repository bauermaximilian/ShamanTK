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

using Eterra.Graphics;
using Eterra.IO;
using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK;
using Eterra.Common;

namespace Eterra.Platforms.Windows.Graphics
{
    /// <summary>
    /// Provides access to a render texture buffer on the GPU,
    /// which can be used as render target and like a normal texture.
    /// </summary>
    class RenderTextureBuffer : Eterra.Graphics.RenderTextureBuffer
    {
        /// <summary>
        /// Gets the handle of the frame buffer.
        /// </summary>
        public int Handle => frameBufferHandle;

        /// <summary>
        /// Gets the handle of the texture which contains the rendered image.
        /// </summary>
        public int TextureHandle => frameBufferTextureHandle;

        private readonly int frameBufferHandle, frameBufferTextureHandle,
            renderBufferHandle;

        private Matrix4 cachedProjectionMatrix = Matrix4.Identity;

        private RenderTextureBuffer(Size size, TextureFilter textureFilter, 
            int frameBufferHandle, int frameBufferTextureHandle, 
            int renderBufferHandle) : base(size, textureFilter)
        {
            this.frameBufferHandle = frameBufferHandle;
            this.frameBufferTextureHandle = frameBufferTextureHandle;
            this.renderBufferHandle = renderBufferHandle;
        }

        /// <summary>
        /// Deletes the buffer.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (GraphicsContext.CurrentContext != null
                && !GraphicsContext.CurrentContext.IsDisposed)
            {
                GL.DeleteFramebuffer(frameBufferHandle);
                GL.DeleteTexture(frameBufferTextureHandle);
                GL.DeleteRenderbuffer(renderBufferHandle);
            }
        }

        /// <summary>
        /// Downloads a section of the current buffer from the GPU.
        /// </summary>
        /// <param name="section">
        /// The section of the buffer to be downloaded.
        /// </param>
        /// <returns>
        /// A new <see cref="TextureData"/> instance.
        /// </returns>
        public override TextureData Download(int x, int y, int width, 
            int height)
        {
            throw new NotImplementedException();
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
        /// of this <see cref="Eterra.Graphics.TextureBuffer"/> instance.
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new, unintialized <see cref="RenderTextureBuffer"/>.
        /// </summary>
        /// <param name="width">
        /// The width of the new <see cref="IRenderBuffer"/>.
        /// </param>
        /// <param name="height">
        /// The height of the new <see cref="IRenderBuffer"/>.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="RenderTextureBuffer"/> class.
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
        public static RenderTextureBuffer Create(Size size, 
            TextureFilter textureFilter)
        {
            if (size.IsEmpty)
                throw new ArgumentException("The specified size is empty.");

            //Convert the specified texture filter to its OpenGL equivalent
            TextureMagFilter magFilter;
            TextureMinFilter minFilter;
            if (textureFilter == TextureFilter.Nearest)
            {
                magFilter = TextureMagFilter.Nearest;
                minFilter = TextureMinFilter.Nearest;
            }
            else if (textureFilter == TextureFilter.Linear)
            {
                magFilter = TextureMagFilter.Linear;
                minFilter = TextureMinFilter.Linear;
            }
            else throw new ArgumentException("The specified texture " +
               "filter is invalid.");

            //Empty the error queue, initialize the buffers and ensure that
            //they were initialized without raising an OutOfMemory error.
            while (GL.GetError() != ErrorCode.NoError) ;

            //Generate the frame buffer first
            int frameBufferHandle = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer,
                frameBufferHandle);

            //Generate a texture, make sure no OutOfMemory ocurred and 
            //attach it to the framebuffer.
            int frameBufferTextureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, frameBufferTextureHandle);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                size.Width, size.Height, 0, PixelFormat.Rgba, 
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
                (int)TextureBuffer.DefaultTextureWrapMode);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureWrapT,
                (int)TextureBuffer.DefaultTextureWrapMode);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                frameBufferTextureHandle, 0);

            //Generate a render buffer, attach it to the frame buffer and
            //check if the process was completed successfully.
            int renderBufferHandle = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer,
                renderBufferHandle);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer,
                RenderbufferStorage.Depth24Stencil8, size.Width, size.Height);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthStencilAttachment,
                RenderbufferTarget.Renderbuffer, renderBufferHandle);

            FramebufferErrorCode framebufferErrorCode =
                GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            if (framebufferErrorCode !=
                FramebufferErrorCode.FramebufferComplete)
                throw new OutOfMemoryException("Framebuffer couldn't be " +
                    "created (" + framebufferErrorCode.ToString() + ").");

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            return new RenderTextureBuffer(size, textureFilter, 
                frameBufferHandle, frameBufferTextureHandle, 
                renderBufferHandle);
        }
    }
}
