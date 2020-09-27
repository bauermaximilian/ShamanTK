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

namespace ShamanTK.Graphics
{
    #region Delegates and enums used by IGraphics.
    /// <summary>
    /// Performs all operations to completely draw the content of
    /// a canvas as part of the applications' redraw cycle.
    /// </summary>
    /// <typeparam name="RenderContextT">
    /// The type derived from <see cref="IRenderContext"/> the provided
    /// canvas instance will have.
    /// </typeparam>
    /// <param name="context">
    /// The canvas instance which provides the drawing capabilities.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Is thrown when <paramref name="context"/> is null.
    /// </exception>
    /// <exception cref="Exception">
    /// Is thrown when the redraw fails.
    /// </exception>
    public delegate void RenderTask<RenderContextT>(RenderContextT context)
        where RenderContextT : IRenderContext;

    /// <summary>
    /// Provides a method that handles a recurring event in the
    /// <see cref="IGraphics"/> interface and derived classes.
    /// </summary>
    /// <param name="sender">
    /// The source <see cref="IGraphics"/> instance of the event.
    /// </param>
    /// <param name="delta">
    /// The time elapsed since the last event of the same type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Is thrown when <paramref name="sender"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Is thrown when <paramref name="delta"/> is negative.
    /// </exception>
    public delegate void GraphicsEventHandler(IGraphics sender, 
        TimeSpan delta);

    /// <summary>
    /// Defines the various modes of a <see cref="IGraphics"/> container.
    /// </summary>
    public enum WindowMode
    {
        /// <summary>
        /// The graphics are displayed in a non-scalable window.
        /// </summary>
        NormalFixed,
        /// <summary>
        /// The graphics are displayed in a scalable window.
        /// </summary>
        NormalScalable,
        /// <summary>
        /// The graphics are displayed in a borderless, non-scalable window.
        /// </summary>
        NormalBorderless,
        /// <summary>
        /// The graphics aren't displayed as the container is minimized 
        /// and/or in the background.
        /// </summary>
        Minimized,
        /// <summary>
        /// The graphics are displayed in a maximized, scalable window.
        /// </summary>
        Maximized,
        /// <summary>
        /// The graphics are displayed fullscreen (borderless, non-scalable).
        /// </summary>
        Fullscreen
    }

    /// <summary>
    /// Describes the various limits of an <see cref="IGraphics"/> 
    /// implementation running on a specific platform.
    /// </summary>
    public enum PlatformLimit
    {
        /// <summary>
        /// The maximum size (width or height) of a <see cref="IGraphics"/>
        /// instance in pixels.
        /// </summary>
        GraphicsSize,
        /// <summary>
        /// The maximum size (width or height) of textures supported by the
        /// current platform in pixels.
        /// </summary>
        TextureSize,
        /// <summary>
        /// The maximum size (width or height) of textures supported by the
        /// current platform in pixels.
        /// </summary>
        RenderBufferSize,
        /// <summary>
        /// The maximum amount of elements in a <see cref="Deformer"/>
        /// instance.
        /// </summary>
        DeformerSize,
        /// <summary>
        /// The maximum amount of lights that can be active at once.
        /// </summary>
        LightCount
    }
    #endregion

    /// <summary>
    /// Represents the main unit of the platform, which hosts the
    /// application and is responsible for updating and drawing the
    /// application using a specified set of drawing parameters.
    /// </summary>
    public interface IGraphics : IDisposable
    {
        /// <summary>
        /// Gets or sets the dimensions of the current graphics window or 
        /// screen in pixels. Setting this value only has an effect when the 
        /// current <see cref="Mode"/> is
        /// <see cref="WindowMode.NormalBorderless"/>,
        /// <see cref="WindowMode.NormalFixed"/> or
        /// <see cref="WindowMode.NormalScalable"/> and is ignored otherwise.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Is thrown when the resolution is invalid in the current context.
        /// </exception>
        Size Size { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WindowMode"/> of the current 
        /// graphics unit instance.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Is thrown when <c>value</c> is invalid.
        /// </exception>
        /// <remarks>
        /// Changing this value can trigger the <see cref="Resized"/> event,
        /// for example when the mode is changed between fullscreen and a 
        /// windowed mode with different resolutions.
        /// </remarks>
        WindowMode Mode { get; set; }

        /// <summary>
        /// Gets a value indicating whether the current graphics interface
        /// is running and invoking the <see cref="Redraw"/>/
        /// <see cref="Update"/> events (<c>true</c>) or if its
        /// currently inactive (<c>false</c>).
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets or sets a value indicating whether vertical synchronization
        /// (VSync) should be enabled (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        bool VSyncEnabled { get; set; }

        /// <summary>
        /// Gets or sets the title of the window or application the graphics
        /// are rendered into.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Occurs when the graphics interface redraws itself.
        /// All drawing operations will be done to a back buffer, which will
        /// be brought to screen after the event was processed completely.
        /// </summary>
        event GraphicsEventHandler Redraw;

        /// <summary>
        /// Occurs when the graphics interface updates itself.
        /// All application logic updates should be done here. 
        /// </summary>
        event GraphicsEventHandler Update;

        /// <summary>
        /// Occurs once after the graphics interface was started and 
        /// initialized, just before the <see cref="Update"/> and 
        /// <see cref="Redraw"/> event loop gets started.
        /// </summary>
        event EventHandler Initialized;

        /// <summary>
        /// Occurs when the graphics interface is closed and disposed.
        /// </summary>
        event EventHandler Closing;

        /// <summary>
        /// Occurs after the size of the window or screen changed.
        /// </summary>
        /// <remarks>
        /// When the window is resized by the user, this event only occurs
        /// once after the user finished resizing the window by releasing the 
        /// edge of the window.
        /// </remarks>
        event EventHandler Resized;

        /// <summary>
        /// Starts running the graphics unit in the current thread.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsRunning"/> is <c>true</c>.
        /// </exception>
        void Run();

        /// <summary>
        /// Stops the graphics unit and disposes all resources.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsRunning"/> is <c>false</c>.
        /// </exception>
        void Close();

        /// <summary>
        /// Retrieves the current limit for various values that can vary
        /// on different platforms or implementations of a 
        /// <see cref="IGraphics"/> unit.
        /// </summary>
        /// <param name="limit">
        /// The limit to be queried.
        /// </param>
        /// <param name="value">
        /// The value of the limit, if the current platform and implementation
        /// supports querying that limit, or 0.
        /// </param>
        /// <returns>
        /// <c>true</c> if the value of the requested platform limit was
        /// successfully retrieved into the <paramref name="value"/> parameter,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="limit"/> is invalid.
        /// </exception>
        bool TryGetPlatformLimit(PlatformLimit limit, out int value);

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
        /// drawing operation or null to use the current window/screen as
        /// render target.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="renderParameters"/>, 
        /// <paramref name="renderTask"/> or <paramref name="renderTarget"/> 
        /// are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="renderTarget"/> is
        /// disposed or wasn't created by this <see cref="IGraphics"/> 
        /// instance.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when this method is called outside the 
        /// <see cref="Redraw"/> event handler or when this method is called
        /// while another render operation is in progress (e.g. when this 
        /// method is called from inside another or the provided 
        /// <paramref name="renderTask"/>).
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
        /// This method should initialize the render context provided to
        /// <paramref name="renderTask"/> so that all parameters are either set
        /// to null or their default values and clear the associated render
        /// target to <see cref="Color.Transparent"/>. After the 
        /// <paramref name="renderTask"/> was executed, the
        /// the render context provided to the <paramref name="renderTask"/>
        /// should be disposed and prevent any further usage.
        /// </remarks>
        void Render<ContextT>(RenderParameters renderParameters,
            RenderTask<ContextT> renderTask, 
            RenderTextureBuffer renderTarget = null) 
            where ContextT : IRenderContext;

        /// <summary>
        /// Creates a new <see cref="MeshBuffer"/> instance.
        /// </summary>
        /// <param name="vertexCount">
        /// The amount of vertices of the new <see cref="MeshBuffer"/>.
        /// </param>
        /// <param name="faceCount">
        /// The amount of faces of the new <see cref="MeshBuffer"/>.
        /// </param>
        /// <param name="vertexPropertyDataFormat">
        /// The format of the <see cref="VertexPropertyData"/> in every
        /// <see cref="Vertex"/> uploaded to the new <see cref="MeshBuffer"/>.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MeshBuffer"/> class.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="vertexCount"/> is less than 1
        /// or when <paramref name="faceCount"/> is less than 1.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="vertexPropertyDataFormat"/> is
        /// invalid.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// Is thrown when there's not enough graphics memory left to create 
        /// a buffer of the specified size or when the specified buffer 
        /// exceeded platform-specific limits.
        /// </exception>
        /// <remarks>
        /// The content of the buffer is undefined and needs to be completely
        /// populated using the methods of the returned 
        /// <see cref="MeshBuffer"/> instance before the buffer is used to 
        /// produce defined results.
        /// </remarks>
        MeshBuffer CreateMeshBuffer(int vertexCount, int faceCount,
            VertexPropertyDataFormat vertexPropertyDataFormat);

        /// <summary>
        /// Creates a new <see cref="TextureBuffer"/> instance.
        /// </summary>
        /// <param name="size">
        /// The size of the new <see cref="TextureBuffer"/>.
        /// </param>
        /// <param name="filter">
        /// The texture interpolation method, which should be used when 
        /// using that texture buffer to perform a drawing operation.
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
        /// <remarks>
        /// The content of the buffer is undefined and needs to be completely
        /// populated using the methods of the returned 
        /// <see cref="TextureBuffer"/> instance before the buffer is used to 
        /// produce defined results.
        /// </remarks>
        TextureBuffer CreateTextureBuffer(Size size, TextureFilter filter);

        /// <summary>
        /// Creates a new <see cref="RenderTextureBuffer"/>.
        /// </summary>
        /// <param name="size">
        /// The size of the new <see cref="RenderTextureBuffer"/>.
        /// </param>
        /// <param name="filter">
        /// The texture interpolation method, which should be used when 
        /// using that texture buffer to perform a drawing operation.
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
        /// <remarks>
        /// The content of the buffer is undefined and needs to be completely
        /// populated by using the returned <see cref="RenderTextureBuffer"/> 
        /// instance as render target in a 
        /// <see cref="Render{ContextT}(RenderParameters, RenderTask{ContextT},
        /// RenderTextureBuffer)"/> call before the buffer is used to 
        /// produce defined results.
        /// </remarks>
        RenderTextureBuffer CreateRenderBuffer(Size size, 
            TextureFilter filter);
    }
}
