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

using System;

namespace Eterra.Graphics
{
    /// <summary>
    /// Defines the different ways a text can be aligned in relation to a
    /// <see cref="Vector3"/> position.
    /// </summary>
    public enum VerticalAlignment
    {
        /// <summary>
        /// The text will be aligned above the Y coordinate of the
        /// specified position.
        /// </summary>
        Top,
        /// <summary>
        /// The text will be aligned with the Y coordinate of the
        /// specified position as center.
        /// </summary>
        Middle,
        /// <summary>
        /// The text will be aligned below the Y coordinate of the
        /// specified position.
        /// </summary>
        Bottom
    }

    /// <summary>
    /// Defines the different ways a text can be aligned in relation to a
    /// <see cref="Vector3"/> position.
    /// </summary>
    public enum HorizontalAlignment
    {
        /// <summary>
        /// The text will be aligned left to the X coordinate of the
        /// specified position.
        /// </summary>
        Left,
        /// <summary>
        /// The text will be aligned central to the X coordinate of the
        /// specified position.
        /// </summary>
        Center,
        /// <summary>
        /// The text will be aligned right to the X coordinate of the
        /// specified position.
        /// </summary>
        Right
    }

    /// <summary>
    /// Provides parameters to define the appeareance and layout of a new
    /// <see cref="SpriteText"/>.
    /// </summary>
    public class SpriteTextFormat
    {
        /// <summary>
        /// Gets or sets the size/height of the glyphs in world units.
        /// Assigned values must be positive - negative values will be made 
        /// absolute automatically.
        /// The default value is 1.
        /// </summary>
        public float TypeSize
        {
            get => typeSize;
            set => typeSize = Math.Abs(value);
        }
        private float typeSize = 1;

        /// <summary>
        /// Gets or sets the factor which, in relation to the current 
        /// <see cref="TypeSize"/>, specifies the distance between individual
        /// rows in the generated <see cref="SpriteText"/>.
        /// The default value is 1.
        /// </summary>
        public float LineSpacingFactor { get; set; } = 1;

        /// <summary>
        /// Gets or sets the horizontal alignment of the text in relation to
        /// its root position. 
        /// The default value is <see cref="HorizontalAlignment.Right"/>.
        /// </summary>
        public HorizontalAlignment HorizontalAlignment { get; set; }
            = HorizontalAlignment.Left;

        /// <summary>
        /// Gets or sets the vertical alignment of the text in relation to
        /// its root position.
        /// The default value is <see cref="VerticalAlignment.Top"/>.
        /// </summary>
        public VerticalAlignment VerticalAlignment { get; set; }
            = VerticalAlignment.Top;

        /// <summary>
        /// Gets or sets the Z distance between consecutive glyphs, which
        /// is added to the Z position on every second drawn glyph to prevent
        /// clipping artifacts due to overlaying pixels.
        /// If this behaviour is not wanted, this property should be set to 0.
        /// Valid values are greater than/equal to 0 - assigned values will
        /// be automatically made absolute so that they're valid.
        /// The default value is 0.001.
        /// </summary>
        public float DepthSpacing
        {
            get => depthSpacing;
            set => depthSpacing = Math.Abs(value);
        }
        private float depthSpacing = 0.001f;

        /// <summary>
        /// Gets or sets a value indicating whether the glyphs should be drawn
        /// from the last to the first (<c>true</c>) or from the first to the
        /// last (<c>false</c>). The default value is <c>false</c>.
        /// </summary>
        public bool InvertDrawingOrder { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteText"/> class.
        /// </summary>
        public SpriteTextFormat() { }
    }
}
