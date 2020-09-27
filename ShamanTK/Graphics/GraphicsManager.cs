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
using System;
using System.Numerics;

namespace ShamanTK.Graphics
{
    /// <summary>
    /// Provides access to the available drawing functions and properties of 
    /// the graphics window.
    /// </summary>
    public class GraphicsManager
    {
        /// <summary>
        /// Gets or sets the <see cref="Engine.Graphics.Size"/> of the 
        /// current graphics window or screen in pixels. Setting this value
        /// only has an effect when the current <see cref="Mode"/> is
        /// <see cref="WindowMode.NormalBorderless"/>,
        /// <see cref="WindowMode.NormalFixed"/> or
        /// <see cref="WindowMode.NormalScalable"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Is thrown when the resolution is invalid for the current graphics
        /// unit.
        /// </exception>
        public Size Size
        {
            get => Graphics.Size;
            set => Graphics.Size = value;
        }

        /// <summary>
        /// Gets or sets the title of the application.
        /// </summary>
        /// <remarks>
        /// Null values are invalid and are automatically replaced with
        /// empty strings.
        /// </remarks>
        public string Title
        {
            get => Graphics.Title;
            set => Graphics.Title = value ?? "";
        }

        /// <summary>
        /// Gets or sets the <see cref="WindowMode"/> of the current 
        /// graphics unit.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Is thrown when <c>value</c> is invalid.
        /// </exception>
        /// <remarks>
        /// Changing this value can trigger the <see cref="Resized"/> event,
        /// for example when the mode is changed between fullscreen and a 
        /// windowed mode with different resolutions.
        /// </remarks>
        public WindowMode Mode
        {
            get => Graphics.Mode;
            set => Graphics.Mode = value;
        }

        /// <summary>
        /// Occurs when the size of the graphics changed.
        /// </summary>
        public event EventHandler Resized;

        /// <summary>
        /// Gets the base <see cref="IGraphics"/> instance.
        /// </summary>
        internal IGraphics Graphics { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsManager"/> 
        /// class.
        /// </summary>
        /// <param name="graphics">
        /// The <see cref="IGraphics"/> instance which should be wrapped by 
        /// this new <see cref="GraphicsManager"/> instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="graphics"/> is null.
        /// </exception>
        internal GraphicsManager(IGraphics graphics)
        {
            Graphics = graphics ??
                throw new ArgumentNullException(nameof(graphics));
            Graphics.Resized += OnResized;
        }

        private void OnResized(object source, EventArgs args)
        {
            try { Resized?.Invoke(this, EventArgs.Empty); }
            catch (Exception exc)
            {
                Log.Error(new ApplicationException("An event handler for " +
                    "the 'Resize' event failed unexpectedly.", exc));
            }
        }

        /// <summary>
        /// Performs a rendering operation to the screen or window managed by
        /// the current <see cref="GraphicsManager"/> instance.
        /// </summary>
        /// <typeparam name="ContextT">
        /// The <see cref="IRenderContext"/> interface type, which provides the
        /// required functionality for the render operation.
        /// </typeparam>
        /// <param name="renderParameters">
        /// The parameters, which are used to create a new instance of the
        /// specified <typeparamref name="ContextT"/>.
        /// </param>
        /// <param name="renderTask">
        /// The set of drawing actions, which will be performed to the created
        /// <see cref="IRenderContext"/> instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="renderParameters"/> or 
        /// <paramref name="renderTask"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when this method is called outside the 
        /// <see cref="ShamanApplicationBase.Redraw(TimeSpan)"/> method 
        /// of the containing <see cref="ShamanApplicationBase"/> 
        /// or when this method is called while another render operation
        /// is in progress (e.g. when this method is called from 
        /// inside another or the provided <paramref name="renderTask"/>).
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when the specified <typeparamref name="ContextT"/> is
        /// not supported by the implementation of this
        /// <see cref="IGraphics"/> instance.
        /// </exception>
        /// <exception cref="ApplicationException">
        /// Is thrown when the <paramref name="renderTask"/> fails.
        /// </exception>
        /// <remarks>
        /// See the <see cref="Render{CanvasT}(RenderParameters, 
        /// RenderTask{CanvasT}, RenderTextureBuffer)"/> method for a detailled
        /// description on how this method works.
        /// </remarks>
        public void Render<ContextT>(RenderParameters renderParameters,
            RenderTask<ContextT> renderTask)
            where ContextT : IRenderContext
        {
            if (renderParameters == null)
                throw new ArgumentNullException(nameof(renderParameters));
            if (renderTask == null)
                throw new ArgumentNullException(nameof(renderTask));

            try { Graphics.Render(renderParameters, renderTask); }
            catch (ApplicationException) { throw; }
            catch (Exception exc)
            {
                throw new ApplicationException("The canvas couldn't be " +
                    "drawn to the main target.", exc);
            }
        }

        /// <summary>
        /// Performs a rendering operation to a 
        /// <see cref="RenderTextureBuffer"/> created by the current
        /// <see cref="GraphicsManager"/> instance.
        /// </summary>
        /// <typeparam name="ContextT">
        /// The <see cref="IRenderContext"/> interface type, which provides the
        /// required functionality for the render operation.
        /// </typeparam>
        /// <param name="renderParameters">
        /// The parameters, which are used to create a new instance of the
        /// specified <typeparamref name="ContextT"/>.
        /// </param>
        /// <param name="renderTask">
        /// The set of drawing actions, which will be performed to the created
        /// <see cref="IRenderContext"/> instance.
        /// </param>
        /// <param name="renderTarget">
        /// The render texture buffer, which should be used as target for the
        /// drawing operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="renderParameters"/>, 
        /// <paramref name="renderTask"/> or <paramref name="renderTarget"/> 
        /// are null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when this method is called outside the 
        /// <see cref="ShamanApplicationBase.Redraw(TimeSpan)"/> method 
        /// of the containing <see cref="ShamanApplicationBase"/> 
        /// or when this method is called while another render operation
        /// is in progress (e.g. when this method is called from 
        /// inside another or the provided <paramref name="renderTask"/>).
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when the specified <typeparamref name="ContextT"/> is
        /// not supported by the implementation of this
        /// <see cref="IGraphics"/> instance.
        /// </exception>
        /// <exception cref="ApplicationException">
        /// Is thrown when the <paramref name="renderTask"/> fails.
        /// </exception>
        /// <remarks>
        /// Calling this method basically does three things for you -
        /// it initializes a render context (and clears the screen or specified
        /// render target), performs a set of drawing operations onto this 
        /// context and then finishes the rendering operation by doing some 
        /// post-processing and applying effects to the rendered image before 
        /// returning. The canvas is always initialized new for every rendering
        /// call using the specified <paramref name="renderParameters"/>. 
        /// Depending on your requirements, a different 
        /// <typeparamref name="ContextT"/> can be specified - 
        /// <see cref="IRenderContextPhong"/>, for example, supports phong 
        /// lighting calculations (while the basic <see cref="IRenderContext"/>
        /// doesn't). If the image should be rendered directly to the screen, 
        /// use the <see cref="Render{CanvasT}(RenderParameters, 
        /// RenderTask{CanvasT})"/> - for rendering to a texture,
        /// this method overload should be used.
        /// As noted in the <see cref="InvalidOperationException"/>, this 
        /// method must not be called from itself and it may only be called
        /// during the redraw cycle of the application (not update).
        /// If any exceptions occur within the <paramref name="renderTask"/>,
        /// they'll just be thrown by this method.
        /// </remarks>
        public void Render<ContextT>(RenderParameters renderParameters,
            RenderTask<ContextT> renderTask, RenderTextureBuffer renderTarget)
            where ContextT : IRenderContext
        {
            if (renderParameters == null)
                throw new ArgumentNullException(nameof(renderParameters));
            if (renderTask == null)
                throw new ArgumentNullException(nameof(renderTask));
            if (renderTarget == null)
                throw new ArgumentNullException(nameof(renderTarget));

            Graphics.Render(renderParameters, renderTask, renderTarget);
        }

        /// <summary>
        /// Creates a new <see cref="RenderTextureBuffer"/>.
        /// </summary>
        /// <param name="size">
        /// The size of the new <see cref="RenderTextureBuffer"/>.
        /// </param>
        /// <param name="textureFilter">
        /// The texture interpolation method, which should be used when 
        /// using that texture buffer to perform a drawing operation.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="RenderTextureBuffer"/> class.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="Size.IsEmpty"/> of
        /// <paramref name="size"/> is <c>true</c> or when 
        /// <paramref name="textureFilter"/> is invalid.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// Is thrown when there's not enough graphics memory left to create 
        /// a buffer of the specified size or when the specified buffer 
        /// exceeded platform-specific limits.
        /// </exception>
        /// <remarks>
        /// The content of the buffer is undefined and needs to be completely
        /// populated by using the returned <see cref="RenderTextureBuffer"/> 
        /// instance as render target in a 
        /// <see cref="Render{ContextT}(RenderParameters, RenderTask{ContextT},
        /// RenderTextureBuffer)"/> call before the buffer is used to 
        /// produce defined results.
        /// </remarks>
        public RenderTextureBuffer CreateRenderBuffer(Size size, 
            TextureFilter textureFilter)
        {
            return Graphics.CreateRenderBuffer(size, textureFilter);
        }

        public Vector2 PointToOrthographic(Vector2 mousePosition, 
            bool proportional)
        {
            float mouseX = mousePosition.X, mouseY = 1 - mousePosition.Y;
            if (proportional && Size.Ratio > 1)
                return new Vector2((mouseX * Size.Ratio) 
                    - ((Size.Ratio - 1) / 2f), mouseY);
            else if (proportional && Size.Ratio < 1)
                return new Vector2(mouseX, (mouseY * (1 / Size.Ratio)) 
                    - (((1 / Size.Ratio) - 1) / 2f));
            else return new Vector2(mouseX, mouseY);
        }
    }
}
