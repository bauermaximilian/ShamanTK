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
using System.IO;

namespace ShamanTK.IO
{
    /// <summary>
    /// Provides the parameters which define the data of a sprite font.
    /// </summary>
    public class SpriteFontData : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="TextureData"/> instance of the sprite font 
        /// texture map, which contains all available characters.
        /// </summary>
        public TextureData Texture { get; }

        /// <summary>
        /// Gets the <see cref="ReadOnlyDictionary{TKey, TValue}"/> instance
        /// which contains mappings between all available characters and its 
        /// rectangular source section in the <see cref="Texture"/>.
        /// </summary>
        public ReadOnlyDictionary<char, GlyphMapping> CharacterMap { get; }

        /// <summary>
        /// Gets the size (height) of the font in pixels.
        /// </summary>
        public float SizePx { get; }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="SpriteFontData"/> class.
        /// </summary>
        /// <param name="texture">
        /// The texture map which contains all available characters.
        /// </param>
        /// <param name="characterMap">
        /// The mappings of all available characters and their location in
        /// the specified <paramref name="texture"/>.
        /// </param>
        /// <param name="sizePx">
        /// The size (height) of the font in pixels.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="texture"/> or 
        /// <paramref name="characterMap"/> are null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="sizePx"/> is less than
        /// <see cref="float.Epsilon"/>.
        /// </exception>
        public SpriteFontData(TextureData texture, 
            IDictionary<char,GlyphMapping> characterMap,
            float sizePx)
        {
            Texture = texture ??
                throw new ArgumentNullException(nameof(texture));

            if (characterMap == null)
                throw new ArgumentNullException(nameof(characterMap));

            Dictionary<char, GlyphMapping> characterMapInternal =
                new Dictionary<char, GlyphMapping>();
            foreach (KeyValuePair<char, GlyphMapping> mapping in characterMap)
                characterMapInternal[mapping.Key] = mapping.Value;
            CharacterMap = new ReadOnlyDictionary<char, GlyphMapping>(
                characterMapInternal);

            if (sizePx < float.Epsilon)
                throw new ArgumentOutOfRangeException(nameof(sizePx));
            SizePx = sizePx;
        }

        /// <summary>
        /// Saves the current <see cref="Timeline"/> instance in the internal 
        /// format used by <see cref="NativeFormatHandler"/>.
        /// </summary>
        /// <param name="stream">
        /// The target stream.
        /// </param>
        /// <param name="includeFormatHeader">
        /// <c>true</c> to include the format header (as specified in
        /// <see cref="NativeFormatHandler"/>), <c>false</c> to start right
        /// off with the resource data.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="StreamWrapper.CanWrite"/> of 
        /// <paramref name="stream"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <see cref="BaseStream"/> of 
        /// <paramref name="streamWrapper"/> was disposed.
        /// </exception>
        internal void Save(Stream stream, bool includeFormatHeader)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("The specified stream is not " +
                    "writable.");

            if (includeFormatHeader)
                NativeFormatHandler.WriteEntryHeader(ResourceType.SpriteFont,
                    stream);

            stream.WriteFloat(SizePx);
            stream.WriteDictionary(CharacterMap, c => BitConverter.GetBytes(c),
                delegate (GlyphMapping g)
                {
                    byte[] buffer = new byte[sizeof(float) * 6];
                    BitConverter.GetBytes(g.ClippingRectangle.X).CopyTo(
                        buffer, sizeof(float) * 0);
                    BitConverter.GetBytes(g.ClippingRectangle.Y).CopyTo(
                        buffer, sizeof(float) * 1);
                    BitConverter.GetBytes(g.ClippingRectangle.Width).CopyTo(
                        buffer, sizeof(float) * 2);
                    BitConverter.GetBytes(g.ClippingRectangle.Height).CopyTo(
                        buffer, sizeof(float) * 3);
                    BitConverter.GetBytes(g.BearingLeft).CopyTo(
                        buffer, sizeof(float) * 4);
                    BitConverter.GetBytes(g.GlyphWidth).CopyTo(
                        buffer, sizeof(float) * 5);
                    return buffer;
                });
            Texture.Save(stream, false);
        }

        /// <summary>
        /// Loads a <see cref="SpriteFontData"/> instance in the internal 
        /// format used by <see cref="NativeFormatHandler"/> from the current 
        /// position of a <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">
        /// The source stream.
        /// </param>
        /// <param name="expectFormatHeader">
        /// <c>true</c> if a header, as defined in 
        /// <see cref="NativeFormatHandler"/>, is expected to occur at the
        /// position of the <paramref name="stream"/>, <c>false</c> if the 
        /// current position of the <paramref name="stream"/> is directly at 
        /// the beginning of the resource data.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="SpriteFontData"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="Stream.CanRead"/> of 
        /// <paramref name="stream"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the data in the stream had an invalid format.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// resource could be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> is disposed and can't
        /// be used anymore.
        /// </exception>
        internal static SpriteFontData Load(Stream stream,
            bool expectFormatHeader)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("The specified stream is not " +
                    "readable.");

            if (expectFormatHeader)
            {
                ResourceType resourceType =
                    NativeFormatHandler.ReadEntryHeader(stream);
                if (resourceType != ResourceType.SpriteFont)
                    throw new FormatException("The specified resource " +
                        "was no sprite font resource.");
            }

            float sizePx = stream.ReadFloat();

            Dictionary<char, GlyphMapping> characterMap =
                stream.ReadDictionary(delegate(byte[] buffer)
                {
                    if (buffer.Length != sizeof(char))
                        throw new FormatException("The byte representation " +
                            "of a glyph character had an invalid size.");
                    return BitConverter.ToChar(buffer, 0);
                },
                delegate (byte[] buffer)
                {
                    if (buffer.Length != sizeof(float) * 6)
                        throw new FormatException("The byte representation " +
                            "of a glyph mapping had an invalid size.");
                    float x = BitConverter.ToSingle(buffer, sizeof(float) * 0);
                    float y = BitConverter.ToSingle(buffer, sizeof(float) * 1);
                    float w = BitConverter.ToSingle(buffer, sizeof(float) * 2);
                    float h = BitConverter.ToSingle(buffer, sizeof(float) * 3);
                    float b = BitConverter.ToSingle(buffer, sizeof(float) * 4);
                    float g = BitConverter.ToSingle(buffer, sizeof(float) * 5);

                    return new GlyphMapping(new Rectangle(x, y, w, h), b, g);
                });

            TextureData texture = TextureData.Load(stream, false);

            return new SpriteFontData(texture, characterMap, sizePx);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Texture.Dispose();
        }
    }
}
