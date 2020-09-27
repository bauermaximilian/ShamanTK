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
using System.Numerics;

namespace ShamanTK.Graphics
{
    /// <summary>
    /// Defines various modes which define how two layers of colors, textures 
    /// or alike can me blend which eatch other.
    /// </summary>
    public enum BlendingMode
    {
        /// <summary>
        /// The layer will be ignored and discarded.
        /// </summary>
        None,
        /// <summary>
        /// The layer will be added with the layer below.
        /// </summary>
        Add,
        /// <summary>
        /// The layer will be multiplied with the layer below.
        /// </summary>
        Multiply
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
        /// <see cref="IRenderContext"/> instance is disposed and can no longer 
        /// be used for drawing (<c>true</c>) or if the instance is ready to be
        /// used (<c>false</c>).
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Gets or sets the <see cref="MeshBuffer"/> instance, which provides
        /// the mesh to be drawn, or null use a <see cref="IO.MeshData.Quad"/>.
        /// The default value is null.
        /// </summary>
        MeshBuffer Mesh { get; set; }

        /// <summary>
        /// Gets or sets the model transformation matrix, which moves, scales
        /// and/or rotates the drawn <see cref="Mesh"/>.
        /// The default value is <see cref="Matrix4x4.Identity"/>.
        /// </summary>
        Matrix4x4 Transformation { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Deformer"/> collection, which is used
        /// to deform the <see cref="Mesh"/> or null, if the mesh shouldn't 
        /// be deformed. See the documentation of <see cref="Deformer"/> for 
        /// more information.
        /// The default value is null.
        /// </summary>
        Deformer Deformation { get; set; }

        /// <summary>
        /// Gets or sets the base color, which is blended with the base
        /// vertex color of the current <see cref="Mesh"/> 
        /// (<see cref="Color.Transparent"/> by default).
        /// The default value is <see cref="Color.Transparent"/>.
        /// </summary>
        Color Color { get; set; }

        /// <summary>
        /// Gets or sets the blending mode used to combine the 
        /// <see cref="Color"/> with the base vertex color of the current
        /// <see cref="Mesh"/>.
        /// The default value is <see cref="BlendingMode.Add"/>.
        /// </summary>
        BlendingMode ColorBlending { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TextureBuffer"/> instance of the main
        /// texture, which is blended with the underlying color layers to 
        /// generate the surface color of the <see cref="Mesh"/>,
        /// or null to only use the blended color layers.
        /// The default value is null.
        /// </summary>
        TextureBuffer Texture { get; set; }

        /// <summary>
        /// Gets or sets the blending mode used to combine the 
        /// <see cref="Texture"/> with the <see cref="Color"/>.
        /// The default value is <see cref="BlendingMode.Add"/>.
        /// </summary>
        BlendingMode TextureBlending { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="Rectangle"/>, which specifies 
        /// the section of the <see cref="Textures"/> which is used as
        /// UV coordinates. The default value (which displays the whole
        /// texture) is <see cref="Rectangle.One"/>.
        /// </summary>
        Rectangle TextureClipping { get; set; }

        /// <summary>
        /// Gets or sets the opacity, with which the <see cref="Mesh"/> 
        /// is drawn. Valid values range between 0.0 and 1.0, invalid values
        /// are clamped automatically. The default value is 1.0.
        /// </summary>
        float Opacity { get; set; }

        /// <summary>
        /// Gets or sets the fog properties, which alters the colors/opacity of 
        /// fragments (pixels) exceeding a specific distance from the 
        /// <see cref="Camera"/>.
        /// The default value is <see cref="Fog.Disabled"/>.
        /// </summary>
        Fog Fog { get; set; }

        /// <summary>
        /// Performs a drawing call the current render target, using the 
        /// current parameters of this <see cref="IRenderContext"/> instance 
        /// and the <see cref="RenderParameters"/>, which were used to create 
        /// this <see cref="IRenderContext"/> instance.
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
