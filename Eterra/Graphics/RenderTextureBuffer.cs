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

using Eterra.Common;
using Eterra.IO;
using System;

namespace Eterra.Graphics
{
    /// <summary>
    /// Provides access to a render texture buffer on the GPU,
    /// which can be used as render target and like a normal texture.
    /// </summary>
    public abstract class RenderTextureBuffer : TextureBuffer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTextureBuffer"/>
        /// base class.
        /// </summary>
        /// <param name="size">The size of the render texture buffer.</param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="Size.IsEmpty"/> of 
        /// <paramref name="size"/> is <c>true</c> or when
        /// the specified <paramref name="textureFilter"/> is invalid.
        /// </exception>
        protected RenderTextureBuffer(Size size, TextureFilter textureFilter)
            : base(size, textureFilter) { }

        /// <summary>
        /// Downloads a section of the current buffer from the GPU.
        /// </summary>
        /// <param name="tx">
        /// The X position of the section to be downloaded
        /// (in texture space).
        /// </param>
        /// <param name="ty">
        /// The Y position of the section to be downloaded
        /// (in texture space).
        /// </param>
        /// <param name="width">
        /// The width of the section to be downloaded.
        /// </param>
        /// <param name="height">
        /// The height of the section to be downloaded.
        /// </param>
        /// <returns>
        /// A new <see cref="TextureData"/> instance.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when the area defined by the parameters is outside the
        /// image area.
        /// </exception>
        public abstract TextureData Download(int tx, int ty, int width, 
            int height);
    }
}
