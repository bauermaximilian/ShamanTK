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

namespace Eterra.Platforms.Windows.Graphics
{
    /// <summary>
    /// Provides capsuled access to the shaders, render targets and 
    /// methods to prepare and finalize a render context rendering.
    /// </summary>
    interface IGraphicsContext
    {
        /// <summary>
        /// Gets the render stage shader used by the current
        /// <see cref="IGraphicsContext"/> instance.
        /// </summary>
        ShaderRenderStage Shader { get; }

        /// <summary>
        /// Gets the current resolution of the associated 
        /// <see cref="IGraphics"/> instance.
        /// </summary>
        Size GraphicsResolution { get; }

        /// <summary>
        /// Prepares redrawing the current <see cref="IGraphicsContext"/>.
        /// </summary>
        void PrepareRedraw(RenderParameters parameters,
            RenderTextureBuffer finalRenderTarget, Size screenSize);

        /// <summary>
        /// Draws a mesh buffer with the parameters of the current
        /// <see cref="IGraphicsContext"/> (and the associated 
        /// <see cref="Shader"/>).
        /// </summary>
        void DrawMeshBuffer(MeshBuffer meshBuffer);

        /// <summary>
        /// Finalizes the redraw of the current <see cref="IGraphicsContext"/>.
        /// </summary>
        void FinalizeRedraw(RenderParameters parameters);
    }
}
