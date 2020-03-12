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
using Eterra.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Numerics;

namespace Eterra.Platforms.Windows.Graphics
{
    /// <summary>
    /// Represents a container for <see cref="RenderTextureBuffer"/> instances 
    /// that are used as render target in a multi-stage rendering pipeline.
    /// </summary>
    class RenderTarget : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the <see cref="Texture"/> property 
        /// of the current <see cref="RenderTarget"/> references a valid,
        /// non-disposed <see cref="RenderTextureBuffer"/> instance
        /// (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsReady => Texture != null && !Texture.IsDisposed;

        /// <summary>
        /// Gets the <see cref="RenderTextureBuffer"/> instance, to which 
        /// the <see cref="Draw"/> call should be rendered into (with the
        /// offset defined in <see cref="CameraOffset"/>), or null (if 
        /// <see cref="IsReady"/> is <c>false</c>).
        /// </summary>
        public RenderTextureBuffer Texture { get; private set; }

        /// <summary>
        /// The position offset added to the <see cref="Camera.Position"/>
        /// of the <see cref="Camera"/> instance which was used to create
        /// the <see cref="RenderContext"/> which contains this 
        /// <see cref="RenderTarget"/> instance.
        /// This offset can, for example, define the offset between the
        /// two eyes (interpupilliary distance) for stereoscopic rendering,
        /// or - if no offset should be applied - just 
        /// <see cref="Vector3.Zero"/>.
        /// </summary>
        public Vector3 CameraOffset { get; private set; } = Vector3.Zero;

        /// <summary>
        /// Gets the transformation <see cref="Matrix4x4"/> instance which
        /// is applied to the plane, which has the <see cref="Texture"/>
        /// assigned to it and is rendered to the screen (or the
        /// <see cref="RenderParameters.CustomRenderTarget"/>) after the
        /// <see cref="IRenderContext"/> was disposed.
        /// </summary>
        public Matrix4x4 TargetPlaneTransformation { get; private set; } 
            = Matrix4x4.Identity;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class
        /// which is not yet ready to use and needs to be made ready with the
        /// <see cref="UpdateTarget(RenderTextureBuffer)"/> or the
        /// <see cref="Update(Vector3, Matrix4x4, int, int, float, 
        /// TextureFilter)"/> method.
        /// </summary>
        public RenderTarget() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class
        /// which is ready to use.
        /// </summary>
        /// <param name="width">
        /// The width of the <see cref="Texture"/> in the new 
        /// <see cref="RenderTarget"/> instance.
        /// </param>
        /// <param name="height">
        /// The height of the <see cref="Texture"/> in the new 
        /// <see cref="RenderTarget"/> instance.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="RenderTarget"/> class.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="width"/> or 
        /// <paramref name="height"/> are less than 1.
        /// </exception>
        public static RenderTarget Create(Size size)
        {
            RenderTarget renderTarget = new RenderTarget();
            renderTarget.Update(Vector3.Zero, Matrix4x4.Identity,
                size, 1, TextureFilter.Nearest);
            return renderTarget;
        }

        /// <summary>
        /// Makes the current <see cref="RenderTarget"/> the current
        /// OpenGL framebuffer target.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsReady"/> is <c>false</c>.
        /// </exception>
        public void MakeCurrent()
        {
            if (IsReady)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer,
                    Texture.Handle);
                GL.Viewport(0, 0, Texture.Size.Width, Texture.Size.Height);
            }
            else throw new InvalidOperationException("The current render " +
                "target was disposed or has not been initialized yet.");
        }

        /// <summary>
        /// Updates the properties of the current <see cref="RenderTarget"/>
        /// instance and puts a new <see cref="Texture"/> in place, if
        /// necessary.
        /// </summary>
        /// <param name="cameraOffset">
        /// The camera offset, which should be added to the 
        /// <see cref="Camera.Position"/> when this <see cref="RenderTarget"/>
        /// instance is used.
        /// </param>
        /// <param name="targetPlaneTransformation">
        /// The transformation matrix (in a screen space as defined by
        /// <see cref="ProjectionMode.OrthographicAbsolute"/>), which 
        /// transforms the plane that will contain the (finished)
        /// <see cref="Texture"/>, which will be drawn to the main render
        /// target in the last step of <see cref="IGraphics.Redraw"/>.
        /// </param>
        /// <param name="screenSize">
        /// The size of the window/screen, which is multiplied by the other
        /// parameters to calculate the target width of the 
        /// <see cref="Texture"/>.
        /// </param>
        /// <param name="resolutionScaleFactor">
        /// The (proportional) resolution scale factor as a value between
        /// <see cref="float.Epsilon"/> and 1, which is multiplied with the
        /// <paramref name="screenWidth"/> and <paramref name="screenHeight"/> to downscale
        /// the resolution of the <see cref="Texture"/>.
        /// </param>
        /// <param name="textureFilter">
        /// The <see cref="TextureFilter"/>, which is used when the 
        /// <see cref="Texture"/> is drawn.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is throen when <paramref name="resolutionScaleFactor"/> is less 
        /// than <see cref="float.Epsilon"/> or greater than 1.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified 
        /// <paramref name="targetPlaneTransformation"/> is not a valid
        /// transformation matrix, from which a scale could be extracted,
        /// or when the resulting width or height (with the scaling parameters
        /// applied) would be less than 1.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// Is thrown when the <see cref="Texture"/> couldn't be created 
        /// because the GPU ran out of memory.
        /// </exception>
        public void Update(Vector3 cameraOffset, 
            Matrix4x4 targetPlaneTransformation, Size screenSize, 
            float resolutionScaleFactor, TextureFilter textureFilter)
        {
            if (screenSize.IsEmpty)
                throw new ArgumentException("The specified screen size " +
                    "is empty.");
            if (resolutionScaleFactor < float.Epsilon)
                throw new ArgumentOutOfRangeException(
                    nameof(resolutionScaleFactor));
            if (resolutionScaleFactor > 1)
                throw new ArgumentOutOfRangeException(
                    nameof(resolutionScaleFactor));

            if (!Matrix4x4.Decompose(targetPlaneTransformation,
                out Vector3 planeScale, out _, out _))
                throw new ArgumentException("The specified target plane " +
                    "transformation is no valid transformation matrix.");

            Size scaledSize = new Size(
                (int)Math.Abs(planeScale.X * resolutionScaleFactor * 
                    screenSize.Width),
                (int)Math.Abs(planeScale.Y * resolutionScaleFactor * 
                    screenSize.Height));

            if (scaledSize.IsEmpty) 
                throw new ArgumentException("The values of " +
                "the specified target plane transformation and the " +
                "resolution scale factor result in a texture width or " +
                "height less than 1 pixel, which is invalid.");

            CameraOffset = cameraOffset;
            TargetPlaneTransformation = targetPlaneTransformation;

            if (Texture == null || Texture.Size != scaledSize || 
                Texture.TextureFilter != textureFilter)
            {
                if (Texture != null) Texture.Dispose();

                Texture = RenderTextureBuffer.Create(scaledSize, 
                    textureFilter);
            }
        }

        /// <summary>
        /// Updates the properties of the current <see cref="RenderTarget"/>
        /// instance and puts a new <see cref="Texture"/> in place.
        /// </summary>
        /// <param name="texture">
        /// The <see cref="RenderTextureBuffer"/> instance to be used as
        /// <see cref="Texture"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="texture"/> is null.
        /// </exception>
        public void UpdateTarget(RenderTextureBuffer texture)
        {
            Texture = texture ?? 
                throw new ArgumentNullException(nameof(texture));
            CameraOffset = Vector3.Zero;
            TargetPlaneTransformation = Matrix4x4.Identity;
        }

        /// <summary>
        /// Removes the current <see cref="Texture"/> from this 
        /// <see cref="RenderTarget"/> so that <see cref="IsReady"/> is
        /// <c>false</c> again.
        /// </summary>
        /// <param name="disposeTexture">
        /// <c>true</c> to dispose the current <see cref="Texture"/> before
        /// setting the property to null, <c>false</c> to just set that 
        /// property to null.
        /// </param>
        public void ClearTarget(bool disposeTexture = false)
        {
            if (disposeTexture) Texture?.Dispose();
            Texture = null;
        }

        /// <summary>
        /// Disposes and removes the current <see cref="Texture"/> so that
        /// <see cref="IsReady"/> will be <c>false</c>. This instance can be
        /// "made ready" with the update methods again.
        /// </summary>
        public void Dispose()
        {
            Texture?.Dispose();
            Texture = null;
        }

        /// <summary>
        /// Resets the current render target to the default back buffer.
        /// </summary>
        /// <param name="viewportSize">
        /// The resolution of the window or screen.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <see cref="Size.IsEmpty"/> of
        /// <paramref name="viewportSize"/> is <c>true</c>.
        /// </exception>
        public static void ResetToDefault(Size viewportSize)
        {
            if (viewportSize.IsEmpty)
                throw new ArgumentOutOfRangeException(
                    nameof(viewportSize));

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, viewportSize.Width,
                viewportSize.Height);
        }
    }
}
