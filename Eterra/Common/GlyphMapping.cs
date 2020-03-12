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

using System.Numerics;
using System.Runtime.InteropServices;

namespace Eterra.Common
{
    /// <summary>
    /// Represents a single glyph in a texture consisting of letters to be used
    /// as a sprite font.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct GlyphMapping
    {
        /// <summary>
        /// Gets the source rectangle where the glyph is located. All values
        /// are relative (between 0.0 and 1.0) to the image.
        /// </summary>
        public Rectangle ClippingRectangle { get; }

        /// <summary>
        /// Gets the width of the <see cref="ClippingRectangle"/>.
        /// </summary>
        public float ClippingWidth => ClippingRectangle.Width;

        /// <summary>
        /// Gets the height of the <see cref="ClippingRectangle"/>.
        /// </summary>
        public float ClippingHeight => ClippingRectangle.Height;

        /// <summary>
        /// Gets the left-side bearing of the glyph, which defines how 
        /// much of the left side of the <see cref="ClippingRectangle"/>
        /// should be overlapped with a preceding character.
        /// </summary>
        public float BearingLeft { get; }

        /// <summary>
        /// Gets the right-side bearing of the glyph, which deifnes how
        /// much of the right side of the <see cref="ClippingRectangle"/>
        /// should be overlapped with a succeding character.
        /// </summary>
        public float BearingRight => ClippingWidth - BearingLeft - GlyphWidth;

        /// <summary>
        /// Gets the actual width of the glyph inside the specified
        /// <see cref="ClippingRectangle"/>.
        /// </summary>
        public float GlyphWidth { get; }

        /// <summary>
        /// Initializes a new <see cref="GlyphMapping"/> instance.
        /// </summary>
        /// <param name="position">
        /// The position of the rectangle which defines the section in the
        /// texture which contains the glyph.
        /// </param>
        /// <param name="size">
        /// The size of the rectangle which defines the section in the texture
        /// which contains the glyph.
        /// </param>
        /// <param name="bearingLeft">
        /// The left-side bearing of the glyph.
        /// </param>
        /// <param name="glyphWidth">
        /// The actual width of the glyph.
        /// </param>
        public GlyphMapping(Vector2 position, Vector2 size,
            float bearingLeft, float glyphWidth)
            : this(new Rectangle(position, size), bearingLeft, glyphWidth)
        { }

        /// <summary>
        /// Initializes a new <see cref="GlyphMapping"/> instance.
        /// </summary>
        /// <param name="rectangle">
        /// The rectangle which relatively defines the section in the texture
        /// which contains the glyph.
        /// </param>
        /// <param name="bearingLeft">
        /// The left-side bearing of the glyph.
        /// </param>
        /// <param name="glyphWidth">
        /// The actual width of the glyph.
        /// </param>
        public GlyphMapping(Rectangle rectangle, float bearingLeft,
            float glyphWidth)
        {
            ClippingRectangle = rectangle;
            BearingLeft = bearingLeft;
            GlyphWidth = glyphWidth;
        }

        /// <summary>
        /// Converts the current <see cref="GlyphMapping"/> instance to a
        /// instance where all values are relative to 
        /// <see cref="Rectangle.Height"/>.
        /// </summary>
        /// <param name="targetHeight">
        /// The new height, to which all other values should be made 
        /// relative to.
        /// </param>
        /// <returns>A new <see cref="GlyphMapping"/> instance.</returns>
        internal GlyphMapping ToRelativeScaled(float targetHeight = 1)
        {
            float aspect = ClippingRectangle.Width / ClippingRectangle.Height;
            Rectangle relativeRectangle = new Rectangle(Vector2.Zero, 
                new Vector2(aspect, 1) 
                * targetHeight);
            return new GlyphMapping(relativeRectangle,
                targetHeight * (BearingLeft / ClippingRectangle.Height),
                targetHeight * (GlyphWidth / ClippingRectangle.Height));
        }
    }
}
