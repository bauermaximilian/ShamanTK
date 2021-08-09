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

#define FLIP_Z

using ShamanTK.IO;
using ShamanTK.Common;
using ShamanTK.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace ShamanTK.Platforms.DesktopGL.Graphics
{
    //TODO: Refactor Graphics, especially ContextSimple/ContextAdvanced.

    /// <summary>
    /// Implements the <see cref="IGraphics"/> interface using OpenGL.
    /// </summary>
    internal partial class Graphics : DisposableBase, IGraphics
    {
        /// <summary>
        /// Defines the maximum amount of deformers which are supported by
        /// this implementation, even if the hardware would support more.
        /// </summary>
        internal const int MaximumDeformers = 128;

        /// <summary>
        /// Defines the maximum amount of lights which are supported by this
        /// implementation, even if the hardware would support more.
        /// </summary>
        internal const int MaximumLights = 8;

        /// <summary>
        /// Gets or sets the <see cref="ShamanTK.Graphics.Size"/> of the 
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
            get => size;
            set
            {
                if (value.IsEmpty)
                    throw new ArgumentException("The specified size is " +
                        "empty.");

                if (Mode != WindowMode.Fullscreen &&
                    Mode != WindowMode.Maximized &&
                    Mode != WindowMode.Minimized)
                {
                    try
                    {
                        Window.Size = new OpenTK.Mathematics.Vector2i(value.Width,
                            value.Height);
                        size = value;
                    }
                    catch (Exception exc)
                    {
                        throw new ArgumentException("The specified " +
                            "resolution is invalid or unsupported.", exc);
                    }
                }
            }
        }
        private Size size;

        /// <summary>
        /// Gets or sets the <see cref="WindowMode"/> of the current graphics
        /// interface.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Is thrown when <c>value</c> is invalid.
        /// </exception>
        public WindowMode Mode
        {
            get
            {
                if (Window.WindowState == WindowState.Fullscreen)
                    return WindowMode.Fullscreen;
                else if (Window.WindowState == WindowState.Minimized)
                    return WindowMode.Minimized;
                else if (Window.WindowState == WindowState.Maximized)
                    return WindowMode.Maximized;
                else if (Window.WindowBorder == WindowBorder.Fixed)
                    return WindowMode.NormalFixed;
                else if (Window.WindowBorder == WindowBorder.Hidden)
                    return WindowMode.NormalBorderless;
                else return WindowMode.NormalScalable;
            }
            set
            {
                switch (value)
                {
                    case WindowMode.Fullscreen:
                        Window.WindowState = WindowState.Fullscreen;
                        break;
                    case WindowMode.Maximized:
                        Window.WindowState = WindowState.Maximized;
                        Window.WindowBorder = WindowBorder.Resizable;
                        break;
                    case WindowMode.NormalScalable:
                        Window.WindowState = WindowState.Normal;
                        Window.WindowBorder = WindowBorder.Resizable;
                        break;
                    case WindowMode.NormalFixed:
                        Window.WindowState = WindowState.Normal;
                        Window.WindowBorder = WindowBorder.Fixed;
                        break;
                    case WindowMode.NormalBorderless:
                        Window.WindowState = WindowState.Normal;
                        Window.WindowBorder = WindowBorder.Hidden;
                        break;
                    case WindowMode.Minimized:
                        Window.WindowState = WindowState.Minimized;
                        break;
                    default: throw new ArgumentException("The specified " +
                        "graphics mode is invalid.");
                }
            }
        }

        /// <summary>
        /// Gets the time when this instance was created.
        /// </summary>
        internal DateTime StartTime { get; }

        /// <summary>
        /// Gets a value indicating whether the current graphics interface
        /// is running and invoking the <see cref="Redraw"/>/
        /// <see cref="Update"/> events (<c>true</c>) or if its
        /// currently inactive (<c>false</c>).
        /// </summary>
        public bool IsRunning { get; private set; }

        private readonly object windowLock = new object();

        /// <summary>
        /// Gets or sets the title of the window or application the graphics
        /// are rendered into, or null for an empty application name.
        /// </summary>
        public string Title
        {
            get => Window.Title;
            set => Window.Title = value ?? "";
        }

        /// <summary>
        /// Gets or sets a value indicating whether vertical synchronization
        /// (VSync) should be enabled (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool VSyncEnabled
        {
            get => Window.VSync == VSyncMode.On;
            set => Window.VSync = value ? VSyncMode.On : VSyncMode.Off;
        }

        /// <summary>
        /// Occurs when the graphics interface redraws itself.
        /// All drawing operations will be done to a back buffer, which will
        /// be brought to screen after the event was processed completely.
        /// </summary>
        public event GraphicsEventHandler Redraw;

        /// <summary>
        /// Occurs when the graphics interface updates itself.
        /// All application logic updates should be done here. 
        /// </summary>
        public event GraphicsEventHandler Update;

        /// <summary>
        /// Occurs before <see cref="Update"/>.
        /// </summary>
        internal event EventHandler PreUpdate;

        /// <summary>
        /// Occurs after <see cref="Update"/>.
        /// </summary>
        internal event EventHandler PostUpdate;

        /// <summary>
        /// Occurs once after the graphics interface was started and 
        /// initialized, just before the <see cref="Update"/> and 
        /// <see cref="Redraw"/> event loop gets started.
        /// </summary>
        public event EventHandler Initialized;

        /// <summary>
        /// Occurs before the graphics interface is closed and disposed.
        /// </summary>
        public event EventHandler Closing;

        /// <summary>
        /// Occurs when the size of the graphics changed.
        /// </summary>
        public event EventHandler Resized;

        /// <summary>
        /// Gets the game window of the current <see cref="Graphics"/>.
        /// </summary>
        internal GameWindow Window { get; }

        private ContextSimple contextSimple;
        private ContextAdvanced contextAdvanced;

        //Both contexts above will use the following shader object, which
        //is initialized before.
        private ShaderRenderStage shaderRenderStage;

#if FLIP_Z
        private static readonly Matrix4 flipZ = Matrix4.CreateScale(1, 1, -1);
#endif

        /// <summary>
        /// A flag which is managed and updated by the 
        /// <see cref="OnRedraw(IGraphics, TimeSpan)"/> method and is used by
        /// the <see cref="RenderCanvas{CanvasT}(TimeSpan, RenderParameters, 
        /// RenderTask{CanvasT})"/> method to check if a drawing call is
        /// allowed or not.
        /// </summary>
        private bool isRedrawing = false;

        // For providing correct times to event-driven redraws/updates
        private DateTime lastRedraw = DateTime.Now;
        private DateTime lastUpdate = DateTime.Now;

        /// <summary>
        /// Initializes a new instance of the <see cref="Graphics"/> class.
        /// </summary>
        public Graphics(bool isEventDriven = false)
        {
            Window = new GameWindow(new GameWindowSettings()
            {
                RenderFrequency = ShamanApp.TargetRedrawsPerSecond,
                UpdateFrequency = ShamanApp.TargetUpdatesPerSecond
            }, new NativeWindowSettings()
            {
                Title = "ApplicationWindow",
                WindowBorder = WindowBorder.Resizable,
                NumberOfSamples = 4,
                IsEventDriven = isEventDriven
            });

            Window.UpdateFrame += OnUpdate;
            Window.RenderFrame += OnRedraw;
            Window.Resize += OnResize;
            Window.Closing += OnClosing;
            Window.Load += OnInitialized;

            StartTime = DateTime.Now;

            size = new Size(Window.Size.X, Window.Size.Y);
        }

        private void OnInitialized()
        {
            if (TryGetPlatformLimit(PlatformLimit.LightCount,
                out int supportedLightsCount) &&
                supportedLightsCount < MaximumLights)
                Log.Warning("The current platform only supports " + 
                    supportedLightsCount + " of the usually available " + 
                    MaximumLights + " maximum lights.");
            if (TryGetPlatformLimit(PlatformLimit.DeformerSize,
                out int supportedDeformersCount) &&
                supportedDeformersCount < MaximumDeformers)
                Log.Warning("The current platform only supports " +
                    supportedDeformersCount + " of the usually available " +
                    MaximumDeformers + " maximum deformers.");

            /*
            string extensions = GL.GetString(StringName.Extensions);
            if (!extensions.Contains("GL_ARB_framebuffer_object"))
                Log.Warning("The current platform might not support " +
                    "render texture buffers.");
            */

            GL.Enable(EnableCap.Multisample);

            try
            {
                try { shaderRenderStage = ShaderRenderStage.Create(this); }
                catch (Exception exc)
                {
                    throw new ApplicationException("The render stage shader " +
                        "couldn't be initialized.", exc);
                }

                contextSimple = new ContextSimple(this);
                contextAdvanced = new ContextAdvanced(this);
            }
            catch (Exception exc)
            {
                Log.Error(exc, "GLGRAPHICS");
                Close();
                return;
            }           

            Initialized?.Invoke(this, EventArgs.Empty);    
        }

        public bool TriggerRedraw()
        {
            if (!Window.IsEventDriven) return false;
            else
            {
                OnRedraw(new FrameEventArgs(
                    (DateTime.Now - lastRedraw).TotalSeconds));
                return true;
            }
        }

        public bool TriggerUpdate()
        {
            if (!Window.IsEventDriven) return false;
            else
            {
                OnRedraw(new FrameEventArgs(
                    (DateTime.Now - lastUpdate).TotalSeconds));
                return true;
            }
        }

        private void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Closing?.Invoke(this, EventArgs.Empty);
            IsRunning = false;
        }

        private void OnResize(ResizeEventArgs e)
        {
            //When the window is minimized, the window state doesn't instantly
            //change to WindowState.Minimized - but instead, the Width and
            //Height of the window are suddenly 0. So, this should only update
            //the Width and Height of the Graphics if the values from the
            //underlying window are greater than 0.
            if (Window.Size.X > 0 && Window.Size.Y > 0)
            {
                size = new Size(Window.Size.X, Window.Size.Y);

                Resized?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnRedraw(FrameEventArgs args)
        {
            isRedrawing = true;
            Redraw?.Invoke(this, TimeSpan.FromSeconds(args.Time));
            GL.Flush();
            isRedrawing = false;

            lastRedraw = DateTime.Now;
        }

        private void OnUpdate(FrameEventArgs args)
        {
            PreUpdate?.Invoke(this, EventArgs.Empty);
            Update?.Invoke(this, TimeSpan.FromSeconds(args.Time));
            PostUpdate?.Invoke(this, EventArgs.Empty);

            lastUpdate = DateTime.Now;
        }

        /// <summary>
        /// Starts running the graphics unit in the current thread.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsRunning"/> is <c>true</c>.
        /// </exception>
        public void Run()
        {
            if (IsRunning)
                throw new InvalidOperationException("The graphics unit is " +
                    "already running.");

            IsRunning = true;
            Window.Run();
        }

        /// <summary>
        /// Stops the graphics unit and disposes all resources.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsRunning"/> is <c>false</c>.
        /// </exception>
        public void Close()
        {
            if (!IsRunning)
                throw new InvalidOperationException("The graphics unit is " +
                    "not running.");

            lock (windowLock) Window.Close();
        }

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
        /// <remarks>
        /// This method uses the <see cref="GL.GetInteger(GetPName)"/> method
        /// to retrieve the limits for OpenGL on the current platform. 
        /// Unfortunately, on some platforms, these values are not provided
        /// by the driver - in these cases, the method will return false.
        /// </remarks>
        public bool TryGetPlatformLimit(PlatformLimit limit, out int value)
        {
            if (!Enum.IsDefined(typeof(PlatformLimit), limit))
                throw new ArgumentException("The limit is invalid.");

            int limitValue = limit switch
            {
                PlatformLimit.GraphicsSize => 
                    GL.GetInteger(GetPName.MaxViewportDims),
                PlatformLimit.TextureSize =>
                    GL.GetInteger(GetPName.MaxTextureSize),
                PlatformLimit.DeformerSize => Math.Min(MaximumDeformers,
                    (GL.GetInteger(GetPName.MaxVertexUniformVectors) / 4) -
                    ShaderRenderStage.BaseVertexShaderVectorCount),
                PlatformLimit.LightCount => Math.Min(MaximumLights,
                    (GL.GetInteger(GetPName.MaxFragmentUniformVectors) / 3) -
                    ShaderRenderStage.BaseFragmentShaderVectorCount),
                PlatformLimit.RenderBufferSize =>
                    GL.GetInteger(GetPName.MaxRenderbufferSize),                    
                _ => throw new ArgumentException("The limit is invalid.")
            };

            value = Math.Max(0, limitValue);
            return limitValue > 0;
        }

        /// <summary>
        /// Creates and renders new canvas.
        /// This method can only be called in a <see cref="Redraw"/> event 
        /// handler.
        /// The specified <paramref name="renderTarget"/> (or the 
        /// screen/window) is cleared with 
        /// <see cref="Common.Color.Transparent"/>before the canvas is 
        /// rendered.
        /// </summary>
        /// <typeparam name="CanvasT">
        /// The type of the canvas, which provides the required functionality
        /// for the operation.
        /// </typeparam>
        /// <param name="parameters">
        /// The parameters, which are used to create a new instance of the
        /// specified <typeparamref name="CanvasT"/>.
        /// </param>
        /// <param name="drawingTask">
        /// The set of drawing actions, which will be performed to the created
        /// <see cref="IRenderContext"/> instance.
        /// </param>
        /// <param name="renderTarget">
        /// The render texture buffer, which should be used as target for the
        /// drawing operation, or null if the drawing operation should be done
        /// directly to the window or screen (default).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="parameters"/> or 
        /// <paramref name="drawer"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="renderTarget"/> is
        /// disposed or wasn't created by this <see cref="IGraphics"/> 
        /// instance.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when this method is called outside the 
        /// <see cref="Redraw"/> event or when this method is called while
        /// another <see cref="RenderCanvas{CanvasT}(RenderParameters, 
        /// RenderTask{CanvasT}, RenderTextureBuffer)"/> method invocation
        /// is still running (e.g. when this method is called from inside
        /// <paramref name="drawer"/>).
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when the specified <typeparamref name="CanvasT"/> is
        /// not supported by the implementation of this
        /// <see cref="IGraphics"/> instance.
        /// </exception>
        /// <remarks>
        /// In this implementation, the drawing calls are not executed (and the
        /// canvas, context and framebuffer sizes are not updated) when the
        /// window is minimized.
        /// </remarks>
        public void Render<CanvasT>(RenderParameters parameters, 
            RenderTask<CanvasT> drawer,
            ShamanTK.Graphics.RenderTextureBuffer renderTarget = null)
            where CanvasT : IRenderContext
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (drawer == null)
                throw new ArgumentNullException(nameof(drawer));

            if (!isRedrawing)
                throw new InvalidOperationException("A canvas can only " +
                    "be drawn from inside the 'Redraw' event.");

            if (Window.WindowState != WindowState.Minimized)
            {
                if (renderTarget != null && renderTarget.IsDisposed)
                    throw new ArgumentException("The specified render " +
                        "target is disposed and can't be used.");

                RenderTextureBuffer typedRenderTarget = 
                    renderTarget as RenderTextureBuffer;

                if (renderTarget != null && typedRenderTarget == null)
                    throw new ArgumentException("The specified render " +
                        "target is invalid.");

                IGraphicsContext context;
                if (parameters.Stereoscopy.Enabled || 
                    parameters.Filters.Enabled) context = contextAdvanced;
                else context = contextSimple;

                context.PrepareRedraw(parameters, typedRenderTarget, 
                    context.GraphicsResolution);

                IRenderContext canvas;

                if (typeof(CanvasT) == typeof(IRenderContext) ||
                    typeof(CanvasT) == typeof(IRenderContextPhong))
                    canvas = new RenderContextPhong(context);
                else throw new NotSupportedException("The specified canvas " +
                    "type is not supported!");

                drawer((CanvasT)canvas);

                canvas.Dispose();
                context.FinalizeRedraw(parameters);
            }
        }

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
        public ShamanTK.Graphics.MeshBuffer CreateMeshBuffer(int vertexCount, 
            int faceCount, VertexPropertyDataFormat vertexPropertyDataFormat)
        {
            return MeshBuffer.Create(vertexCount, faceCount,
                vertexPropertyDataFormat, shaderRenderStage);
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
        public ShamanTK.Graphics.TextureBuffer CreateTextureBuffer(Size size, 
            TextureFilter filter)
        {
            return TextureBuffer.Create(size, filter);
        }

        /// <summary>
        /// Creates a new, unintialized <see cref="RenderTextureBuffer"/>.
        /// </summary>
        /// <param name="size">
        /// The size of the new <see cref="TextureBuffer"/>.
        /// </param>
        /// <param name="filter">
        /// The texture interpolation method, which should be used when 
        /// rendering a model with the texture buffer.
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
        public ShamanTK.Graphics.RenderTextureBuffer CreateRenderBuffer(
            Size size, TextureFilter filter)
        {
            return RenderTextureBuffer.Create(size, filter);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (IsRunning) Close();
        }

        /// <summary>
        /// Creates a projection <see cref="Matrix4"/> for a 
        /// <see cref="Camera"/>.
        /// </summary>
        /// <param name="camera">
        /// The <see cref="Camera"/> instance.
        /// </param>
        /// <param name="canvasSize">
        /// The size of the projection canvas in pixels.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="Size.IsEmpty"/> of
        /// <paramref name="canvasSize"/> is <c>true</c>.
        /// </exception>
        public static Matrix4 CreateProjectionMatrix(Camera camera, 
            Size canvasSize, bool flipY = false)
        {
            float aspectRatio = canvasSize.Ratio;

            Matrix4 matrix;

            switch (camera.ProjectionMode)
            {
                case ProjectionMode.Perspective:
                    matrix = Matrix4.CreatePerspectiveFieldOfView(
                        camera.PerspectiveFieldOfView.Radians, aspectRatio,
                        camera.ClippingRange.X, camera.ClippingRange.Y);
                    break;
                case ProjectionMode.OrthographicAbsolute:
                    matrix = Matrix4.CreateOrthographicOffCenter(0,
                        canvasSize.Width, 0, canvasSize.Height, 
                        camera.ClippingRange.X, camera.ClippingRange.Y);
                    break;
                case ProjectionMode.OrthographicRelative:
                    matrix = Matrix4.CreateOrthographicOffCenter(
                        0, 1, 0, 1, camera.ClippingRange.X,
                        camera.ClippingRange.Y);
                    break;
                case ProjectionMode.OrthgraphicRelativeProportional:
                    float leftRight = Math.Max((aspectRatio - 1) / 2, 0);
                    float topBottom = Math.Max((1 - aspectRatio) / 2, 0);

                    matrix = Matrix4.CreateOrthographicOffCenter(
                        -leftRight, 1 + leftRight, -topBottom,
                        1 + topBottom, camera.ClippingRange.X,
                        camera.ClippingRange.Y);
                    break;
                default:
                    throw new ArgumentException("The projection mode of the" +
                        "current camera was invalid.");
            }

#if FLIP_Z
            matrix = flipZ * matrix;
#endif

            if (flipY) matrix *= Matrix4.CreateScale(1, -1, 1);

            return matrix;
        }
    }
}
