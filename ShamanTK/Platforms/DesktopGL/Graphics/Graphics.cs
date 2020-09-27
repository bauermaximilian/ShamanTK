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
    /// <summary>
    /// Implements the <see cref="IGraphics"/> interface using OpenGL.
    /// </summary>
    internal class Graphics : DisposableBase, IGraphics
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

        //TODO: Refactor Graphics, especially ContextSimple/ContextAdvanced.
        private class GraphicsContextSimple : IGraphicsContext
        {
            public ShaderRenderStage Shader => 
                parentGraphics.shaderRenderStage;

            private static readonly Color clearColor = Color.Transparent;

            private readonly RenderTarget finalRenderTarget = 
                new RenderTarget();

            private readonly Graphics parentGraphics;

            public Size GraphicsResolution => parentGraphics.Size;

            private Camera currentCamera = null;

            private bool wireframeEnabled = false;

            private bool redrawInProgress = false;

            private readonly int supportedLightsCount;

            public GraphicsContextSimple(Graphics parentGraphics)
            {
                this.parentGraphics = parentGraphics ??
                    throw new ArgumentNullException(nameof(parentGraphics));

                if (!parentGraphics.TryGetPlatformLimit(
                    PlatformLimit.LightCount,
                    out supportedLightsCount))
                    supportedLightsCount = MaximumLights;

                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(DepthFunction.Less);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, 
                    BlendingFactor.OneMinusSrcAlpha);
                /*GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha,
                BlendingFactorDest.OneMinusSrcAlpha,
                    BlendingFactorSrc.One, BlendingFactorDest.Zero);*/
                GL.ClearColor(clearColor.R, clearColor.G, clearColor.B, 
                    clearColor.Alpha);
            }

            public void PrepareRedraw(RenderParameters parameters,
                RenderTextureBuffer finalRenderTarget, Size screenSize)
            {
                //Before anything else, make sure that this flag is set to
                //detect if no other canvas rendering is currently in progress.
                if (redrawInProgress)
                    throw new InvalidOperationException("The rendering " +
                        "process couldn't be initialized - another canvas " +
                        "is being rendered right now.");
                else redrawInProgress = true;

                //Applies all parameters (and the correct render target).
                //Mind that the render target which is made current by that
                //method actually stays current until the end of the redraw -
                //this is intentional and important.
                ApplyParameters(parameters, finalRenderTarget);

                //Upload camera view and projection matrices to the GPU,
                //using the width and height of the texture as viewport
                //dimensions for the projection matrix.
                SetCamera(parameters.Camera, System.Numerics.Vector3.Zero,
                    screenSize, finalRenderTarget != null);
            }

            private void ApplyParameters(RenderParameters parameters,
                RenderTextureBuffer finalRenderTarget)
            {
                if (parameters == null)
                    throw new ArgumentNullException(nameof(parameters));

                //The correct render target is set and made current. 
                //If double buffering is disabled, it's important that the only
                //"MakeCurrent" required to set the correct final render target
                //is done here - if double buffering is enabled, the render
                //targets will be reassigned later, so then it wouldn't really
                //matter (unless something goes wrong and it's not 
                //reassigned...).
                if (!IsNullOrDisposed(finalRenderTarget))
                {
                    this.finalRenderTarget.UpdateTarget(finalRenderTarget);
                    this.finalRenderTarget.MakeCurrent();
                }
                else
                {
                    this.finalRenderTarget.ClearTarget();
                    RenderTarget.ResetToDefault(GraphicsResolution);
                }

                //Depending on the now current render target, either the 
                //back buffer or the render texture is cleared here.
                ClearRenderTarget();

                //Make the main render stage shader current.
                Shader.Use();

                //Enable or disable backface culling (clockwise to match the
                //order of the faces, as specified in the "Face" class).
                if (parameters.BackfaceCullingEnabled)
                {
                    GL.Enable(EnableCap.CullFace);
                    GL.CullFace(CullFaceMode.Back);
                    //HACK: Without this, everything rendered to texture will
                    //have flipped faces.
                    if (finalRenderTarget != null)
                        GL.FrontFace(FrontFaceDirection.Ccw);
                    else GL.FrontFace(FrontFaceDirection.Cw);
                }
                else GL.Disable(EnableCap.CullFace);

                wireframeEnabled = 
                    parameters.WireframeRenderingEnabled;
            }

            public void DrawMeshBuffer(MeshBuffer meshBuffer)
            {
                if (meshBuffer == null)
                    throw new ArgumentNullException(nameof(meshBuffer));
                if (meshBuffer.IsDisposed)
                    throw new ArgumentException("The specified mesh " +
                        "buffer was disposed and can no longer be used.");

                Shader.VertexPropertyDataFormat.Set(
                    (int)meshBuffer.VertexPropertyDataFormat);

                GL.BindVertexArray(meshBuffer.Handle);

                PrimitiveType drawType = PrimitiveType.Triangles;

                if (wireframeEnabled) drawType = PrimitiveType.LineStrip;

                GL.DrawElements(drawType, meshBuffer.FaceCount * 3,
                        DrawElementsType.UnsignedInt, 0);

                GL.BindVertexArray(0);
            }

            private void SetCamera(Camera camera,
                System.Numerics.Vector3 offset, Size viewportSize, 
                bool flipY = false)
            {
                currentCamera = camera ??
                    throw new ArgumentNullException(nameof(camera));

                System.Numerics.Vector3 cameraPosition =
                    currentCamera.Position + offset;

                Shader.View.Set(Matrix4.CreateTranslation(
                        -cameraPosition.X, -cameraPosition.Y,
                        -cameraPosition.Z) *
                    Matrix4.CreateFromQuaternion(new Quaternion(
                        -currentCamera.Rotation.X,
                        -currentCamera.Rotation.Y,
#if FLIP_Z
                        currentCamera.Rotation.Z,
#else
                        -currentCamera.Rotation.Z,
#endif
                        currentCamera.Rotation.W)));
                Shader.ViewPosition.Set(cameraPosition);

                Shader.Projection.Set(CreateProjectionMatrix(camera, 
                    viewportSize, flipY));
            }

            public void ClearRenderTarget()
            {
                GL.Clear(ClearBufferMask.ColorBufferBit |
                    ClearBufferMask.DepthBufferBit |
                    ClearBufferMask.StencilBufferBit);
            }


            public void FinalizeRedraw(RenderParameters parameters)
            {
                //The SwapBuffer command should only be used when the main back
                //buffer of the window/screen is the render target.
                if (!finalRenderTarget.IsReady)
                    parentGraphics.Window.SwapBuffers();

                //Reset the flag to false to allow other render calls again.
                redrawInProgress = false;
            }
        }

        private class GraphicsContextAdvanced : IGraphicsContext
        {
            public ShaderRenderStage Shader =>
                parentGraphics.shaderRenderStage;

            private static readonly Color clearColor = Color.Transparent;

            private readonly ShaderEffectStage shaderPostProcessing;
            private readonly Camera postProcessingCamera = new Camera()
            {
                ProjectionMode = ProjectionMode.OrthographicRelative
            };

            private readonly MeshBuffer textureTargetPlane;

            private readonly RenderTarget 
                monoRenderTarget = new RenderTarget(),
                stereoLeftRenderTarget = new RenderTarget(),
                stereoRightRenderTarget = new RenderTarget();

            private RenderTarget[] currentRenderTargets = null;

            private readonly RenderTarget finalRenderTarget = 
                new RenderTarget();

            private readonly Graphics parentGraphics;

            public Size GraphicsResolution => parentGraphics.Size;

            private Camera currentCamera = null;

            private bool wireframeEnabled = false;

            private bool redrawInProgress = false;

            private readonly int supportedLightsCount;

            public GraphicsContextAdvanced(Graphics parentGraphics)
            {
                this.parentGraphics = parentGraphics ??
                    throw new ArgumentNullException(nameof(parentGraphics));

                if (!parentGraphics.TryGetPlatformLimit(
                    PlatformLimit.LightCount,
                    out supportedLightsCount))
                    supportedLightsCount = MaximumLights;

                try
                {
                shaderPostProcessing = ShaderEffectStage.Create();
                MeshData planeData = MeshData.CreatePlane(
                    new System.Numerics.Vector3(0, 0, 0.5f), 
                    new System.Numerics.Vector3(0, 1, 0.5f),
                    new System.Numerics.Vector3(1, 0, 0.5f));
                textureTargetPlane = MeshBuffer.Create(planeData.VertexCount,
                    planeData.FaceCount, planeData.VertexPropertyDataFormat,
                    shaderPostProcessing);
                textureTargetPlane.UploadVertices(planeData, 0,
                    planeData.VertexCount);
                textureTargetPlane.UploadFaces(planeData, 0,
                    planeData.FaceCount);
                }
                catch (Exception exc)
                {
                    throw new ApplicationException("The post processing " +
                        "stage shader couldn't be initialized.", exc);
                }

                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(DepthFunction.Less);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha,
                    BlendingFactor.OneMinusSrcAlpha);
                /*GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha,
                BlendingFactorDest.OneMinusSrcAlpha,
                    BlendingFactorSrc.One, BlendingFactorDest.Zero);*/
                GL.ClearColor(clearColor.R, clearColor.G, clearColor.B,
                    clearColor.Alpha);
            }

            public void PrepareRedraw(RenderParameters parameters, 
                RenderTextureBuffer finalRenderTarget, Size screenSize)
            {
                //Before anything else, make sure that this flag is set to
                //detect if no other canvas rendering is currently in progress.
                if (redrawInProgress)
                    throw new InvalidOperationException("The rendering " +
                        "process couldn't be initialized - another canvas " +
                        "is being rendered right now.");
                else redrawInProgress = true;

                //Copy the resolution scale factor and filter from the filter
                //parameters or use the default values if the filter parameters
                //are disabled.
                float scaleFactor = 1;
                TextureFilter scaleFilter = TextureFilter.Linear;

                if (parameters.Filters.Enabled)
                {
                    scaleFactor = parameters.Filters.ResolutionScaleFactor;
                    scaleFilter = parameters.Filters.ResolutionScaleFilter;
                }

                //Set the parameters for the render targets which 
                //should be used, make them current and clear them.
                if (parameters.Stereoscopy.Enabled)
                {
                    System.Numerics.Vector3 ipdOffset =
                        System.Numerics.Vector3.UnitX * 0.5f *
                        parameters.Stereoscopy.InterpupilliaryDistance;

                    stereoLeftRenderTarget.Update(-ipdOffset,
                        parameters.Stereoscopy.ViewportTransformationLeft,
                        screenSize, scaleFactor, scaleFilter);
                    stereoLeftRenderTarget.MakeCurrent();
                    ClearRenderTarget();

                    stereoRightRenderTarget.Update(ipdOffset,
                        parameters.Stereoscopy.ViewportTransformationRight,
                        screenSize, scaleFactor, scaleFilter);
                    stereoRightRenderTarget.MakeCurrent();
                    ClearRenderTarget();

                    currentRenderTargets = new RenderTarget[]
                        { stereoLeftRenderTarget, stereoRightRenderTarget };
                }
                else
                {
                    monoRenderTarget.Update(System.Numerics.Vector3.Zero,
                        System.Numerics.Matrix4x4.Identity, screenSize,
                        scaleFactor, scaleFilter);
                    monoRenderTarget.MakeCurrent();
                    ClearRenderTarget();

                    currentRenderTargets = new RenderTarget[] 
                        { monoRenderTarget };
                }

                //Upload camera view and projection matrices to the GPU,
                //using the width and height of the viewport for the 
                //projection matrix.
                SetCamera(parameters.Camera, System.Numerics.Vector3.Zero,
                    screenSize);

                //Applies remaining parameters.
                ApplyParameters(parameters, finalRenderTarget);
            }

            private void ApplyParameters(RenderParameters parameters,
                RenderTextureBuffer finalRenderTarget)
            {
                if (parameters == null)
                    throw new ArgumentNullException(nameof(parameters));

                //The correct render target is set and made current. 
                //If double buffering is disabled, it's important that the only
                //"MakeCurrent" required to set the correct final render target
                //is done here - if double buffering is enabled, the render
                //targets will be reassigned later, so then it wouldn't really
                //matter (unless something goes wrong and it's not 
                //reassigned...).
                if (!IsNullOrDisposed(finalRenderTarget))
                {
                    this.finalRenderTarget.UpdateTarget(finalRenderTarget);
                    this.finalRenderTarget.MakeCurrent();
                }
                else
                {
                    this.finalRenderTarget.ClearTarget();
                    RenderTarget.ResetToDefault(GraphicsResolution);
                }

                //Depending on the now current render target, either the 
                //back buffer or the render texture is cleared here.
                ClearRenderTarget();

                //Make the main render stage shader current.
                Shader.Use();

                //Enable or disable backface culling (clockwise to match the
                //order of the faces, as specified in the "Face" class).
                if (parameters.BackfaceCullingEnabled)
                {
                    GL.Enable(EnableCap.CullFace);
                    GL.CullFace(CullFaceMode.Back);
                    //HACK: Without this, everything rendered to texture will
                    //have flipped faces.
                    if (finalRenderTarget != null)
                        GL.FrontFace(FrontFaceDirection.Ccw);
                    else GL.FrontFace(FrontFaceDirection.Cw);
                }
                else GL.Disable(EnableCap.CullFace);

                wireframeEnabled =
                    parameters.WireframeRenderingEnabled;
            }

            public void DrawMeshBuffer(MeshBuffer meshBuffer)
            {
                DrawMeshBuffer(meshBuffer, false);
            }

            private void DrawMeshBuffer(MeshBuffer meshBuffer,
                bool ignoreCurrentRenderTargets)
            {
                if (meshBuffer == null)
                    throw new ArgumentNullException(nameof(meshBuffer));
                if (meshBuffer.IsDisposed)
                    throw new ArgumentException("The specified mesh " +
                        "buffer was disposed and can no longer be used.");

                GL.BindVertexArray(meshBuffer.Handle);

                PrimitiveType drawType = PrimitiveType.Triangles;

                if (!ignoreCurrentRenderTargets)
                {
                    //The vertex property data format only needs to be set in 
                    //the rendering stage/shader, not in the effect stage.
                    Shader.VertexPropertyDataFormat.Set(
                        (int)meshBuffer.VertexPropertyDataFormat);

                    if (wireframeEnabled) drawType = PrimitiveType.LineStrip;

                    foreach (RenderTarget renderTarget in currentRenderTargets)
                    {
                        renderTarget.MakeCurrent();
                        SetCamera(currentCamera, renderTarget.CameraOffset,
                            renderTarget.Texture.Size);
                        GL.DrawElements(drawType, meshBuffer.FaceCount * 3,
                            DrawElementsType.UnsignedInt, 0);
                    }
                }
                else
                {
                    GL.DrawElements(drawType, meshBuffer.FaceCount * 3,
                            DrawElementsType.UnsignedInt, 0);
                }

                GL.BindVertexArray(0);
            }

            private void SetCamera(Camera camera,
                System.Numerics.Vector3 offset, Size viewportSize,
                bool flipY = false)
            {
                currentCamera = camera ??
                    throw new ArgumentNullException(nameof(camera));

                System.Numerics.Vector3 cameraPosition =
                    currentCamera.Position + offset;

                Shader.View.Set(Matrix4.CreateTranslation(
                        -cameraPosition.X, -cameraPosition.Y,
                        -cameraPosition.Z) *
                    Matrix4.CreateFromQuaternion(new Quaternion(
                        -currentCamera.Rotation.X,
                        -currentCamera.Rotation.Y,
#if FLIP_Z
                        currentCamera.Rotation.Z,
#else
                        -currentCamera.Rotation.Z,
#endif
                        currentCamera.Rotation.W)));
                Shader.ViewPosition.Set(cameraPosition);

                Shader.Projection.Set(CreateProjectionMatrix(camera,
                    viewportSize, flipY));
            }

            public void ClearRenderTarget()
            {
                GL.Clear(ClearBufferMask.ColorBufferBit |
                    ClearBufferMask.DepthBufferBit |
                    ClearBufferMask.StencilBufferBit);
            }

            public void FinalizeRedraw(RenderParameters parameters)
            {
                //Use the post processing shader.
                shaderPostProcessing.Use();

                //Apply post-processing-specific effect parameters to the
                //effect shader (if the filters are enabled, that is).
                if (parameters.Filters.Enabled)
                {
                    shaderPostProcessing.ColorResolution.Set(
                        parameters.Filters.ColorShades);
                }
                else
                {
                    shaderPostProcessing.ColorResolution.Set(RenderParameters
                        .GraphicsFilterParameters.ColorShadesFull);
                }

                //Sets the correct "final" render target for the whole 
                //drawing operation (can either be a "user"-defined framebuffer 
                //or the backbuffer - this was specified in "ApplyParameters").
                if (finalRenderTarget.IsReady)
                {
                    finalRenderTarget.MakeCurrent();
                    shaderPostProcessing.Projection.Set(CreateProjectionMatrix(
                        postProcessingCamera, finalRenderTarget.Texture.Size));
                    shaderPostProcessing.FlipTextureY.Set(true);
                }
                else
                {
                    RenderTarget.ResetToDefault(GraphicsResolution);
                    shaderPostProcessing.Projection.Set(CreateProjectionMatrix(
                        postProcessingCamera, GraphicsResolution));
                    shaderPostProcessing.FlipTextureY.Set(false);
                }

                //Draw each of the render targets for post processing to 
                //the screen with the post processing shader using the
                //plane mesh buffered in the constructor.
                foreach (RenderTarget renderTarget in currentRenderTargets)
                {
                    if (!renderTarget.IsReady)
                        throw new ArgumentException("The specified " +
                            "array contained at least one render target " +
                            "without a render texture buffer.");

                    shaderPostProcessing.Texture.Set(renderTarget.Texture);
                    shaderPostProcessing.Model.Set(
                        renderTarget.TargetPlaneTransformation);
                    DrawMeshBuffer(textureTargetPlane, true);
                }

                //The SwapBuffer command should only be used when the main back
                //buffer of the window/screen is the render target.
                if (!finalRenderTarget.IsReady)
                    parentGraphics.Window.SwapBuffers();

                //Reset the flag to false to allow other render calls again.
                redrawInProgress = false;
            }
        }

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

        private GraphicsContextSimple contextSimple;
        private GraphicsContextAdvanced contextAdvanced;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="Graphics"/> class.
        /// </summary>
        public Graphics()
        {
            Window = new GameWindow(new GameWindowSettings()
            {
                RenderFrequency = ShamanApplicationBase.TargetRedrawsPerSecond,
                UpdateFrequency = ShamanApplicationBase.TargetUpdatesPerSecond
            }, new NativeWindowSettings()
            {
                Title = "ApplicationWindow",
                WindowBorder = WindowBorder.Resizable
            });

            Window.UpdateFrame += OnUpdate;
            Window.RenderFrame += OnRedraw;
            Window.Resize += OnResize;
            Window.Closing += OnClosing;
            Window.Load += OnInitialized;

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

            string extensions = GL.GetString(StringName.Extensions);
            if (!extensions.Contains("GL_ARB_framebuffer_object"))
                Log.Warning("The current platform might not support " +
                    "render texture buffers.");

            try
            {
                try { shaderRenderStage = ShaderRenderStage.Create(this); }
                catch (Exception exc)
                {
                    throw new ApplicationException("The render stage shader " +
                        "couldn't be initialized.", exc);
                }

                contextSimple = new GraphicsContextSimple(this);
                contextAdvanced = new GraphicsContextAdvanced(this);
            }
            catch (Exception exc)
            {
                Log.Error(exc, "GLGRAPHICS");
                Close();
                return;
            }           

            Initialized?.Invoke(this, EventArgs.Empty);    
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
            isRedrawing = false;
        }

        private void OnUpdate(FrameEventArgs args)
        {
            PreUpdate?.Invoke(this, EventArgs.Empty);
            Update?.Invoke(this, TimeSpan.FromSeconds(args.Time));
            PostUpdate?.Invoke(this, EventArgs.Empty);
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
