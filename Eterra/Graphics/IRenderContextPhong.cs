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
    /// Provides functionality to perform drawing calls to a render target
    /// using the phong shading algorithm, where the 
    /// <see cref="IRenderContext.Texture"/> specifies the diffuse map.
    /// </summary>
    public interface IRenderContextPhong : IRenderContext
    {
        /// <summary>
        /// Gets or sets the <see cref="TextureBuffer"/> instance of the
        /// specular texture channel.
        /// </summary>
        TextureBuffer SpecularMap { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TextureBuffer"/> instance of the
        /// normal texture channel.
        /// </summary>
        TextureBuffer NormalMap { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TextureBuffer"/> instance of the
        /// emission texture channel.
        /// </summary>
        TextureBuffer EmissionMap { get; set; }
    }
}
