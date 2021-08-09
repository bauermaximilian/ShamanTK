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

using ShamanTK.Common;
using ShamanTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using OpenTK.Mathematics;

namespace ShamanTK.Platforms.DesktopGL.Graphics
{
    internal partial class Graphics
    {
        private class ContextSimple : IGraphicsContext
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

            public ContextSimple(Graphics parentGraphics)
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
