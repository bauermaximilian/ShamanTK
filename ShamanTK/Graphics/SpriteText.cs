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
    /// Represents a drawable text, which consists of sprites for each
    /// character.
    /// </summary>
    public class SpriteText
    {
        /// <summary>
        /// Provides the definition for a single glyph in three-dimensional
        /// space.
        /// </summary>
        public readonly struct Glyph
        {
            /// <summary>
            /// Gets a value which indicates whether the current 
            /// <see cref="Glyph"/> instance has an empty
            /// <see cref="TextureClipping"/> (<c>true</c>) or not
            /// (<c>false</c>).
            /// </summary>
            public bool IsEmpty => TextureClipping.IsEmpty;

            /// <summary>
            /// Gets the absolute transformation matrix, which will be applied
            /// to the plane mesh with the texture of the individual glyph.
            /// </summary>
            public Matrix4x4 GlyphTransformation { get; }

            /// <summary>
            /// Gets the texture clipping rectangle, which provides the
            /// location of the glyph inside the glyph texture map.
            /// </summary>
            public Rectangle TextureClipping { get; }

            /// <summary>
            /// Initializes a new <see cref="Glyph"/> instance.
            /// </summary>
            /// <param name="glyphTransformation">
            /// The absolute glyph plane mesh transformation.
            /// </param>
            /// <param name="textureClipping">
            /// The rectangle which contains the texture of the single glyph.
            /// </param>
            public Glyph(Matrix4x4 glyphTransformation,
                Rectangle textureClipping)
            {
                TextureClipping = textureClipping;
                GlyphTransformation = glyphTransformation;
            }

            /// <summary>
            /// Creates a new <see cref="Glyph"/> instance, using the
            /// current <see cref="TextureClipping"/> and a moved
            /// <see cref="GlyphTransformation"/>.
            /// </summary>
            /// <param name="offset">
            /// The direction and distance into which the 
            /// <see cref="GlyphTransformation"/> should be moved.
            /// </param>
            /// <returns>
            /// A new <see cref="Glyph"/> instance.
            /// </returns>
            public Glyph Moved(Vector3 offset)
            {
                return new Glyph(GlyphTransformation * 
                    Matrix4x4.CreateTranslation(offset), TextureClipping);
            }
        }

        /// <summary>
        /// Gets or sets the color of the glyphs in the current 
        /// <see cref="SpriteText"/>. The default value is 
        /// <see cref="Color.White"/>.
        /// </summary>
        /// <remarks>
        /// For this feature to work properly, the base texture of the
        /// associated <see cref="SpriteFont"/> needs to be transparent with
        /// the letters in pure <see cref="Color.White"/>.
        /// </remarks>
        public Color Color { get; set; } = Color.White;

        /// <summary>
        /// Gets or sets the opacity of the glyphs in the current
        /// <see cref="SpriteText"/>. Valid values are between 0.0 and 1.0,
        /// assigned values will be clamped automatically.
        /// The default value is 1.
        /// </summary>
        public float Opacity
        {
            get => opacity;
            set => opacity = Math.Max(0, Math.Min(1, value));
        }
        private float opacity = 1.0f;

        /// <summary>
        /// Gets the amount of glyphs in the current <see cref="SpriteText"/>.
        /// This value doesn't necessarily match with the amount of letters 
        /// in the original string, as this count also contains characters 
        /// which can't be drawn but still reserve a slot in the collection.
        /// </summary>
        public int GlyphCount => glyphs.Length;

        private readonly Glyph[] glyphs;

        /// <summary>
        /// Gets the <see cref="Graphics.SpriteFont"/> instance, which was
        /// used to create this <see cref="SpriteText"/> instance.
        /// </summary>
        public SpriteFont SpriteFont { get; }

        /// <summary>
        /// Gets the size of the text area.
        /// </summary>
        public Vector2 AreaSize { get; }

        /// <summary>
        /// Gets the position of the left bottom edge of the text area.
        /// </summary>
        public Vector3 AreaPositionMinimum { get; }

        /// <summary>
        /// Gets the position of the right top edge of the text area.
        /// </summary>
        public Vector3 AreaPositionMaximum { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteText"/> class.
        /// </summary>
        /// <param name="glyphs">
        /// The array of <see cref="Glyph"/> instances, which specifies
        /// the positions of the glyph both in 3D space and the 2D texture
        /// source from <paramref name="spriteFont"/>.
        /// </param>
        /// <param name="spriteFont">
        /// The <see cref="Graphics.SpriteFont"/> instance this new
        /// <see cref="SpriteText"/> instance should be linked to.
        /// </param>
        /// <param name="areaPositionMinimum">
        /// The position of the lower left edge of the text area.
        /// </param>
        /// <param name="size">
        /// The size of the text area.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="glyphs"/> or
        /// <paramref name="spriteFont"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="size"/> contains
        /// negative components.
        /// </exception>
        internal SpriteText(Glyph[] glyphs, SpriteFont spriteFont, 
            Vector3 areaPositionMinimum, Vector2 size)
        {
            this.glyphs = glyphs ??
                throw new ArgumentNullException(nameof(glyphs));
            SpriteFont = spriteFont ??
                throw new ArgumentNullException(nameof(spriteFont));

            if (size.X < 0 || size.Y < 0)
                throw new ArgumentException("The specified size is invalid.");
            else AreaSize = size;
            AreaPositionMinimum = areaPositionMinimum;
            AreaPositionMaximum = areaPositionMinimum +
                new Vector3(size.X, size.Y, 0);
        }

        /// <summary>
        /// Draws the current <see cref="SpriteText"/> instance to a
        /// <see cref="IRenderContext"/>.
        /// </summary>
        /// <param name="renderContext">The target render context.</param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="renderContext"/> is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the <see cref="SpriteFont"/> instance, which was
        /// used to generate this <see cref="SpriteText"/> instance, was
        /// disposed (and with it, the glyph texture map and the mesh to
        /// draw the single glyphs onto).
        /// </exception>
        public void Draw(IRenderContext renderContext)
        {
            if (renderContext == null)
                throw new ArgumentNullException(nameof(renderContext));

            renderContext.Mesh = SpriteFont.GlyphMesh;
            renderContext.Opacity = Opacity;
            renderContext.Color = Color;
            renderContext.TextureBlending = BlendingMode.Multiply;
            renderContext.Texture = SpriteFont.GlyphTexture;

            for (int i = 0; i < GlyphCount; i++)
            {
                Glyph glyph = glyphs[i];
                if (!glyph.IsEmpty)
                {
                    renderContext.Transformation = glyph.GlyphTransformation;
                    renderContext.TextureClipping = glyph.TextureClipping;
                    renderContext.Draw();
                }
            }
        }
    }
}
