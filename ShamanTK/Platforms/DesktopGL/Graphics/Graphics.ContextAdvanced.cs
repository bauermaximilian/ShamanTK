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
using OpenTK.Graphics.OpenGL;
using System;
using OpenTK.Mathematics;

namespace ShamanTK.Platforms.DesktopGL.Graphics
{
    internal partial class Graphics
    {
        private class ContextAdvanced : IGraphicsContext
        {
            public ShaderRenderStage Shader =>
                parentGraphics.shaderRenderStage;

            private static readonly Color clearColor = Color.Transparent;
            private static readonly TimeSpan fullDay = TimeSpan.FromDays(1);

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

            public ContextAdvanced(Graphics parentGraphics)
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
                        GL.FrontFace(FrontFaceDirection.Cw);
                    else GL.FrontFace(FrontFaceDirection.Ccw);
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
                        -currentCamera.OrientationQuaternion.X,
                        -currentCamera.OrientationQuaternion.Y,
#if FLIP_Z
                        currentCamera.OrientationQuaternion.Z,
#else
                        -currentCamera.OrientationQuaternion.Z,
#endif
                        currentCamera.OrientationQuaternion.W)));
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

                TimeSpan elapsed = DateTime.Now - parentGraphics.StartTime;
                double elapsedSeconds =
                    elapsed.TotalSeconds % fullDay.TotalSeconds;

                shaderPostProcessing.Time.Set((float)elapsedSeconds);
                shaderPostProcessing.ScanlineEffectEnabled.Set(
                    parameters.Filters.ScanlineEffectEnabled);

                if (!parameters.BackfaceCullingEnabled)
                    GL.Enable(EnableCap.CullFace);
                else GL.Disable(EnableCap.CullFace);

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
    }
}
