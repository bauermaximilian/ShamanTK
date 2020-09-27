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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;

namespace ShamanTK.Graphics
{
    /// <summary>
    /// Represents a sprite font, which utilizes a texture of supported glyphs 
    /// to draw text to a <see cref="Canvas"/>.
    /// </summary>
    public class SpriteFont : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether all unmanaged resources used by 
        /// the current <see cref="SpriteFont"/> instance have been disposed 
        /// completely (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsDisposed => 
            GlyphTexture.IsDisposed && GlyphMesh.IsDisposed;

        /// <summary>
        /// Gets a value indicating whether the texture buffer, which is used 
        /// by the current <see cref="SpriteFont"/> instance, will be disposed 
        /// with this instance (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool DisposeBuffer { get; }

        /// <summary>
        /// Gets the original size (glyph height) of the sprite font in pixels.
        /// </summary>
        public float TypeSizePixels { get; }

        /// <summary>
        /// Gets the <see cref="TextureBuffer"/> instance which contains all
        /// characters available in the current <see cref="SpriteFont"/>
        /// instance.
        /// </summary>
        internal TextureBuffer GlyphTexture { get; }

        /// <summary>
        /// Gets the <see cref="MeshBuffer"/> instance, onto which each
        /// <see cref="SpriteText.Glyph"/> is drawn onto when rendering a 
        /// full <see cref="SpriteText"/> instance.
        /// </summary>
        internal MeshBuffer GlyphMesh { get; }

        private readonly IReadOnlyDictionary<char, GlyphMapping> glyphMappings;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteFont"/> class.
        /// </summary>
        /// <param name="glyphMesh">
        /// The buffer of the mesh onto which a single glyph of the
        /// <paramref name="glyphMapTexture"/> will be drawn on.
        /// Using a <see cref="MeshBuffer"/> instance with the data from 
        /// <see cref="IO.MeshData.CreatePlane"/> is recommended.
        /// </param>
        /// <param name="glyphMapTexture">
        /// The buffer of the texture with all available glyphs.
        /// </param>
        /// <param name="glyphTextureMappings">
        /// The mappings of the individual characters (as key) and their
        /// section in the <paramref name="glyphMapTexture"/> (as value).
        /// </param>
        /// <param name="typeSizePixels">
        /// The size (line height) of the sprite font in pixels.
        /// </param>
        /// <param name="disposeBuffer">
        /// <c>true</c> to dispose the 
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="glyphMesh"/>,
        /// <paramref name="glyphMapTexture"/> or
        /// <paramref name="glyphTextureMappings"/> are null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="typeSizePixels"/> is less than
        /// <see cref="float.Epsilon"/>.
        /// </exception>
        public SpriteFont(MeshBuffer glyphMesh, 
            TextureBuffer glyphMapTexture,
            IDictionary<char, GlyphMapping> glyphTextureMappings, 
            float typeSizePixels,
            bool disposeBuffer)
        {
            GlyphMesh = glyphMesh ??
                throw new ArgumentNullException(nameof(glyphMesh));
            GlyphTexture = glyphMapTexture ??
                throw new ArgumentNullException(nameof(glyphMapTexture));
            if (glyphTextureMappings == null)
                throw new ArgumentNullException(nameof(glyphTextureMappings));

            if (typeSizePixels < float.Epsilon)
                throw new ArgumentOutOfRangeException(nameof(typeSizePixels));
            TypeSizePixels = typeSizePixels;

            Dictionary<char, GlyphMapping> mappings = 
                new Dictionary<char, GlyphMapping>();

            foreach (KeyValuePair<char, GlyphMapping> glyphMapping in
                glyphTextureMappings)
                mappings.Add(glyphMapping.Key, glyphMapping.Value);

            glyphMappings = new ReadOnlyDictionary<char, GlyphMapping>(
                mappings);

            DisposeBuffer = disposeBuffer;
        }

        /// <summary>
        /// Creates a new <see cref="SpriteText"/> instance which can be drawn
        /// to a <see cref="Canvas"/> with the 
        /// <see cref="Draw(Canvas, SpriteText)"/> method of the
        /// current <see cref="SpriteFont"/> instance.
        /// </summary>
        /// <param name="position">The base position of the text.</param>
        /// <param name="text">The actual text string.</param>
        /// <param name="format">The format of the text.</param>
        /// <returns>
        /// A new instance of the <see cref="SpriteText"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="text"/> or
        /// <paramref name="format"/> are null.
        /// </exception>
        /// <remarks>
        /// It is recommended to only create a new <see cref="SpriteText"/>
        /// when the text has actually changed. For further optimisation,
        /// a <see cref="SpriteText"/> can be drawn to a 
        /// <see cref="RenderTextureBuffer"/> once, and that buffer can be
        /// applied to a simple plane mesh and rendered over and over again.
        /// </remarks>
        public SpriteText CreateText(Vector3 position, string text, 
            SpriteTextFormat format)
        {
            SpriteText.Glyph[] typesetContent = CreateTypesetContent(
                position, text, format, out Vector2 size);

            float areaMinimumX, areaMinimumY;

            if (format.HorizontalAlignment == HorizontalAlignment.Left)
                areaMinimumX = position.X;
            else if (format.HorizontalAlignment == HorizontalAlignment.Center)
                areaMinimumX = position.X - size.X / 2;
            else areaMinimumX = position.X - size.X;

            if (format.VerticalAlignment == VerticalAlignment.Top)
                areaMinimumY = position.Y;
            else if (format.VerticalAlignment == VerticalAlignment.Middle)
                areaMinimumY = position.Y - size.Y / 2;
            else areaMinimumY = position.Y - size.Y;

            return new SpriteText(typesetContent, this,
                new Vector3(areaMinimumX, areaMinimumY, position.Z), size);
        }

        /// <summary>
        /// Checks whether the current <see cref="SpriteFont"/> instance
        /// can render a specific character.
        /// </summary>
        /// <param name="c">The character to be checked.</param>
        /// <returns>
        /// <c>true</c> if the character can be rendered,
        /// <c>false</c> if not - in that case, the character will be
        /// omitted in a <see cref="SpriteText"/>.
        /// </returns>
        public bool Supports(char c)
        {
            return glyphMappings.ContainsKey(c);
        }

        private SpriteText.Glyph[] CreateTypesetContent(
            Vector3 position, string text, SpriteTextFormat format,
            out Vector2 size)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            string[] textRows =
                text != null ? text.Split('\n') : new string[0];

            //The amount of characters minus the new line characters.
            int typesetLength = text.Length - (textRows.Length - 1);
            int typesetCaret = 0;

            SpriteText.Glyph[] typesetContent =
                new SpriteText.Glyph[typesetLength];

            float caretX = 0, caretY = 0;

            //Set the Y caret to a value which automatically aligns the 
            //created glyphs to the base position, using the expected height
            //of the complete text (which is the absolute row height/size
            //multiplied with the amount of rows).
            float totalHeight = format.LineSpacingFactor * format.TypeSize 
                * textRows.Length;
            if (format.VerticalAlignment == VerticalAlignment.Middle)
                caretY = totalHeight / 2;
            else if (format.VerticalAlignment == VerticalAlignment.Top)
                caretY = totalHeight;

            //This value will be the highest caretX value afterwards.
            float totalWidth = 0;

            foreach (string row in textRows)
            {
                caretX = 0;
                int rowStartIndex = typesetCaret;
                int rowGlyphCount = 0;

                //Get the glyph mapping or continue if the glyph is undefined.
                for (int i = 0; i < row.Length; i++)
                {
                    if (!glyphMappings.TryGetValue(row[i],
                        out GlyphMapping mapping))
                        continue;
                    GlyphMapping absoluteMapping = mapping.ToRelativeScaled(
                        format.TypeSize);

                    float shiftedZ = position.Z + (typesetCaret % 2) 
                        * format.DepthSpacing;

                    //Generate the drawing call with the default alignment,
                    //using the properties from the glyph and the current
                    //caret X position, which is shifted to the left using the
                    //absolute bearing of the glyph.
                    Matrix4x4 glyphTransformation = 
                        MathHelper.CreateTransformation(
                        position.X + caretX - absoluteMapping.BearingLeft,
                        position.Y + caretY - absoluteMapping.ClippingHeight,
                        shiftedZ, absoluteMapping.ClippingWidth,
                        absoluteMapping.ClippingHeight, 1);

                    SpriteText.Glyph drawingCall =
                        new SpriteText.Glyph(glyphTransformation,
                        mapping.ClippingRectangle);

                    //Advance the caret.
                    caretX += absoluteMapping.GlyphWidth;

                    int adaptedCaret = format.InvertDrawingOrder ?
                        (typesetLength - 1 - typesetCaret) : typesetCaret;
                    typesetContent[adaptedCaret] = drawingCall;
                    typesetCaret++;
                    
                    rowGlyphCount++;
                }

                totalWidth = Math.Max(totalWidth, caretX);

                //After the glyph positions were calculated, apply the
                //horizontal alignment (if not default) in another step.
                float horizontalAlignmentOffset = 0;
                if (format.HorizontalAlignment == 
                    HorizontalAlignment.Center)
                    horizontalAlignmentOffset = -caretX / 2;
                else if (format.HorizontalAlignment == 
                    HorizontalAlignment.Right)
                    horizontalAlignmentOffset = -caretX;

                if (horizontalAlignmentOffset != 0)
                {
                    for (int i = rowStartIndex; i < rowStartIndex
                        + rowGlyphCount; i++)
                    {
                        typesetContent[i] = typesetContent[i].Moved(
                            new Vector3(horizontalAlignmentOffset, 0, 0));
                    }
                }

                //Update the Y caret for the next row.
                caretY -= format.LineSpacingFactor * format.TypeSize;
            }

            size = new Vector2(totalWidth, totalHeight);

            return typesetContent;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!GlyphMesh.IsDisposed) GlyphMesh.Dispose();
            if (!GlyphTexture.IsDisposed && DisposeBuffer) GlyphTexture.Dispose();
        }
    }
}
