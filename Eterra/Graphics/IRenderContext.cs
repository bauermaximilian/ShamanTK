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
using System;
using System.Numerics;

namespace Eterra.Graphics
{
    /// <summary>
    /// Defines various mixing modes, which define how colors, textures or
    /// vertex colors will be mixed with each other.
    /// </summary>
    public enum MixingMode
    {
        /// <summary>
        /// The current color source will be ignored.
        /// </summary>
        None,
        /// <summary>
        /// The current source is added with the other.
        /// </summary>
        Add,
        /// <summary>
        /// The current source is multiplied with the other.
        /// </summary>
        Multiply,
        /// <summary>
        /// The current source is used to illuminate the other.
        /// </summary>
        Light
    }

    /// <summary>
    /// Provides functionality to perform drawing calls to a render target.
    /// The basic functionality defined by this interface is supported by every
    /// graphics unit implementation and serves as base for every derived
    /// canvas (interface) type.
    /// </summary>
    public interface IRenderContext : IDisposable
    {
        /// <summary>
        /// Gets a value which indicates whether the current 
        /// <see cref="IRenderContext"/> instance is disposed and can no longer be
        /// used for drawing (<c>true</c>) or if the instance is ready to be
        /// used (<c>false</c>).
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Gets or sets the <see cref="MeshBuffer"/> instance, which provides
        /// the mesh to be drawn.
        /// </summary>
        MeshBuffer Mesh { get; set; }

        /// <summary>
        /// Gets or sets the model transformation matrix, which moves, scales
        /// and/or rotates the drawn mesh.
        /// </summary>
        Matrix4x4 Location { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Deformer"/> collection, which is used
        /// to deform the <see cref="Mesh"/> or null, if the mesh shouldn't 
        /// be deformed. See the documentation of <see cref="Deformer"/> for 
        /// more information.
        /// </summary>
        Deformer Deformation { get; set; }

        /// <summary>
        /// Gets or sets the base model color. The default value is
        /// <see cref="Color.Black"/>.
        /// </summary>
        Color Color { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TextureBuffer"/> instance of the main
        /// texture, which - together with the <see cref="Color"/> - defines 
        /// the surface color of the model.
        /// </summary>
        TextureBuffer Texture { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="MixingMode"/>, which is used 
        /// to mix the <see cref="Color"/> with the <see cref="Texture"/>. 
        /// The default value is <see cref="MixingMode.Normal"/>.
        /// </summary>
        MixingMode TextureMixingMode { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="Rectangle"/>, which specifies 
        /// the section of the <see cref="Textures"/> which is used as
        /// UV coordinates. <see cref="Rectangle.One"/> can be used to
        /// display the whole texture.
        /// </summary>
        Rectangle TextureClipping { get; set; }

        /// <summary>
        /// Gets or sets the opacity, with which the <see cref="IRenderContext.Mesh"/> 
        /// is drawn. Valid values range between 0.0 and 1.0, invalid values
        /// are clamped automatically. The default value is 1.0.
        /// </summary>
        float Opacity { get; set; }

        /// <summary>
        /// Performs a drawing call the current render target, using the 
        /// current parameters of this <see cref="IRenderContext"/> instance and the 
        /// <see cref="RenderParameters"/>, which were used to create this
        /// <see cref="IRenderContext"/> instance.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the drawing call was executed successfully,
        /// <c>false</c> if one of the required parameters was invalid.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when this <see cref="IRenderContext"/> was disposed and
        /// can no longer be used for drawing operations.
        /// </exception>
        bool Draw();
    }
}
