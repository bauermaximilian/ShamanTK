/* 
 * Eterra Framework
 * A simple framework for creating multimedia applications.
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

namespace Eterra.Graphics
{
    /// <summary>
    /// Defines the various shading modes which can be used to render a
    /// mesh with a material.
    /// </summary>
    public enum ShadingMode
    {
        /// <summary>
        /// Defines a flat shading, where all lighting and shadow 
        /// calculations are skipped and the mesh is rendered only with its 
        /// <see cref="Canvas.Color"/> or, if available, the
        /// <see cref="TextureBufferCollection.MainChannel"/> as surface 
        /// texture.
        /// </summary>
        Flat,
        /// <summary>
        /// Defines a phong-based shading, which extends <see cref="Flat"/>
        /// by light, shadow and other features (like specular, emission and
        /// normal maps). 
        /// Either <see cref="Canvas.Color"/> or, if 
        /// available, the <see cref="TextureBufferCollection.MainChannel"/>
        /// is used as diffuse map. If available, the 
        /// <see cref="TextureBufferCollection.EffectChannel01"/> is used
        /// as specular map, the 
        /// <see cref="TextureBufferCollection.EffectChannel02"/> as
        /// normal map and the
        /// <see cref="TextureBufferCollection.EffectChannel03"/> as
        /// emission map.
        /// </summary>
        Phong,
        /// <summary>
        /// Defines PBR (physically-based) shading, which produces the most
        /// life-like results.
        /// Either <see cref="Canvas.Color"/> or, if 
        /// available, the <see cref="TextureBufferCollection.MainChannel"/>
        /// is used as albedo map. If available, the 
        /// <see cref="TextureBufferCollection.EffectChannel01"/> is used
        /// as metallic map, the 
        /// <see cref="TextureBufferCollection.EffectChannel02"/> as
        /// roughness map and the
        /// <see cref="TextureBufferCollection.EffectChannel03"/> as
        /// ambient occlusion map.
        /// </summary>
        PBR
    }
}
