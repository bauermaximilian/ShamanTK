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
using System.Linq;
using System.Runtime.InteropServices;

namespace ShamanTK.IO
{
    /// <summary>
    /// Gets a single pixel from a texture (or image-like source).
    /// </summary>
    /// <param name="tx">
    /// The X position of the requested pixel in texture coordinate space,
    /// where the origin is in the top left corner and the X axis 
    /// "points right".
    /// </param>
    /// <param name="ty">
    /// The Y position of the requested pixel in texture coordinate space,
    /// where the origin is in the top left corner and the Y axis
    /// "points downwards".
    /// </param>
    /// <returns>
    /// A new instance of the <see cref="Color"/> structure.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Is thrown when the requested pixel lies outside of the texture
    /// boundaries.
    /// </exception>
    public delegate Color PixelRetriever(int tx, int ty);

    /// <summary>
    /// Provides the base class from which textures are derived.
    /// </summary>
    public abstract class TextureData : DisposableBase
    {
        /// <summary>
        /// Represents a pointer to the pixel data of a 
        /// <see cref="TextureData"/> instance.
        /// </summary>
        public class Pointer
        {
            /// <summary>
            /// Gets a collection of supported color types.
            /// </summary>
            public static IReadOnlyCollection<Type> SupportedColorTypes 
                { get; } = new ReadOnlyCollection<Type>(new Type[]
                {
                    typeof(Color), typeof(Color.RGB), typeof(Color.RGB32),
                    typeof(Color.BGR), typeof(Color.BGRA), typeof(Color.ARGB)
                });

            /// <summary>
            /// Gets a pointer to the beginning of the pixel data in 
            /// unmanaged memory.
            /// </summary>
            public IntPtr Scan0 { get; }

            /// <summary>
            /// Gets the size of a single pixel in the unmanaged memory 
            /// in bytes.
            /// </summary>
            public int PixelSize { get; }

            /// <summary>
            /// Gets the type of the pixel data. The value is an element from
            /// <see cref="SupportedColorTypes"/>.
            /// </summary>
            public Type ColorType { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Pointer"/> class.
            /// </summary>
            /// <param name="scan0">
            /// A pointer to the beginning of the pixel data.
            /// </param>
            /// <param name="size">
            /// The size of the area in the unmanaged memory in bytes.
            /// </param>
            /// <param name="colorType">
            /// The type of the pixel data.
            /// </param>
            /// <exception cref="ArgumentNullException">
            /// Is thrown when <paramref name="colorType"/> is null.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// Is thrown when <paramref name="colorType"/> isn't contained
            /// in <see cref="SupportedColorTypes"/>.
            /// </exception>
            public Pointer(IntPtr scan0, Type colorType)
            {
                if (colorType == null)
                    throw new ArgumentNullException(nameof(colorType));
                if (!SupportedColorTypes.Contains(colorType))
                    throw new ArgumentException("The specified color type " +
                        "is not supported.");

                Scan0 = scan0;
                ColorType = colorType;

                PixelSize = Marshal.SizeOf(colorType);
            }
        }

        #region Internally used Texture implementations.
        internal class MemoryTexure : TextureData
        {
            private Color[] data;

            public override Pointer PixelData => null;

            public MemoryTexure(Size size, Color[] pixelData) : base(size)
            {
                if (pixelData == null)
                    throw new ArgumentNullException(nameof(pixelData));
                if (pixelData.Length != (Size.Width * Size.Height))
                    throw new ArgumentException("The pixel data length was " +
                        "invalid!");

                data = pixelData;
            }

            public override Color[] GetRegion(int x, int y, int width,
                int height)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(GetType().Name);

                AssertValidTextureSection(x, y, width, height);

                Color[] areaData = new Color[width * height];

                if (x == 0 && width == Size.Width)
                {
                    Array.ConstrainedCopy(data, y * Size.Width, areaData, 0, 
                        width * height);
                }
                else
                {
                    for (int i = y; i < (x + height); i++)
                    {
                        Array.ConstrainedCopy(data, i * Size.Width + x,
                            areaData, i * width, width);
                    }
                }

                return areaData;
            }

            protected override void Dispose(bool disposing)
            {
                data = null;
            }
        }
        #endregion

        /// <summary>
        /// Defines the maximum width of a texture source.
        /// </summary>
        public const int MaxWidth = 8192;

        /// <summary>
        /// Defines the maximum height of a texture source.
        /// </summary>
        public const int MaxHeight = 8192;

        /// <summary>
        /// Gets the maximum size of a texture source, which may not be
        /// exceeded either in <see cref="Size.Width"/> or 
        /// <see cref="Size.Height"/>.
        /// </summary>
        public static Size MaxSize { get; } = new Size(MaxWidth, MaxHeight);

        /// <summary>
        /// Gets the size of the texture source.
        /// </summary>
        public Size Size { get; }

        /// <summary>
        /// Gets the amount of pixels in the current <see cref="TextureData"/>
        /// instance.
        /// </summary>
        public int PixelCount => Size.Width * Size.Height;

        /// <summary>
        /// Gets a pointer to the pixel data in the unmanaged memory or null,
        /// if the current instance doesn't support accessing the pixel data
        /// from unmanaged memory.
        /// </summary>
        public abstract Pointer PixelData { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureData"/>
        /// class.
        /// </summary>
        /// <param name="size">The size of the texture source.</param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="Size.Width"/> of <paramref name="size"/> 
        /// is less than/equal to zero or greater than <see cref="MaxWidth"/>  
        /// or when <see cref="Size.Height"/> of <paramref name="size"/> is 
        /// less than/equal to zero or greater than <see cref="MaxHeight"/>.
        /// </exception>
        public TextureData(Size size)
        {
            if (size.IsEmpty || size.Exceeds(MaxSize))
                throw new ArgumentException("The specified size was either " +
                    "empty or exceeded the maximum size of texture sources.");

            Size = size;
        }

        /// <summary>
        /// Gets the index of a pixel at a specific position.
        /// </summary>
        /// <param name="x">
        /// The X position of the requested pixel in texture coordinate space,
        /// where the origin is in the top left corner and the X axis 
        /// "points right".
        /// </param>
        /// <param name="y">
        /// The Y position of the requested pixel in texture coordinate space,
        /// where the origin is in the top left corner and the Y axis
        /// "points downwards".
        /// </param>
        /// <returns>
        /// The index of the pixel at the specified position.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="x"/> is less than 0 or greater 
        /// than/equal to <see cref="Width"/> or when <paramref name="y"/>
        /// is less than 0 or greater than/equal to <see cref="Height"/>.
        /// </exception>
        public virtual int GetPixelIndex(int x, int y)
        {
            if (x < 0 || x >= Size.Width)
                throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= Size.Height)
                throw new ArgumentOutOfRangeException(nameof(y));

            return y * Size.Width + x;
        }

        /// <summary>
        /// Gets the position of a pixel with a specific index.
        /// </summary>
        /// <param name="index">The index of the requested pixel.</param>
        /// <param name="x">
        /// The X position of the requested pixel in texture coordinate space,
        /// where the origin is in the top left corner and the X axis 
        /// "points right".
        /// </param>
        /// <param name="y">
        /// The Y position of the requested pixel in texture coordinate space,
        /// where the origin is in the top left corner and the Y axis
        /// "points downwards".
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="x"/> is less than 0 or greater 
        /// than/equal to <see cref="Width"/> or when <paramref name="y"/>
        /// is less than 0 or greater than/equal to <see cref="Height"/>.
        /// </exception>
        public virtual void GetPixelPosition(int index, out int x, out int y)
        {
            if (index < 0 || index >= PixelCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            x = index % Size.Width;
            y = index / Size.Width;
        }

        /// <summary>
        /// Gets pixels in an area of the <see cref="TextureData"/>.
        /// </summary>
        /// <remarks>
        /// It is recommended to use this method for retrieving the pixel data,
        /// which should be transferred to the GPU.
        /// When overriding the <see cref="TextureData"/> class, it is 
        /// recommended to override this method and optimize its performance,
        /// as the base implementation retrieves the image data pixel by pixel
        /// using the <see cref="GetPixel(int, int)"/> method.
        /// </remarks>
        /// <param name="x">
        /// The X position of the area origin in texture coordinate space,
        /// where the origin is in the top left corner and the X axis 
        /// "points right".
        /// </param>
        /// <param name="y">
        /// The Y position of the requested pixel in texture coordinate space,
        /// where the origin is in the top left corner and the Y axis
        /// "points downwards".
        /// </param>
        /// <param name="width">
        /// The width of the area.
        /// </param>
        /// <param name="height">
        /// The height of the area.
        /// </param>
        /// <returns>An array of <see cref="Color"/> instances.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when the texture area specified by the parameters exceeds
        /// the current textures boundaries or when the 
        /// <paramref name="width"/> or <paramref name="height"/> are less
        /// than/equal to 0.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the object data was disposed and can't be accessed
        /// anymore.
        /// </exception>
        public abstract Color[] GetRegion(int x, int y, int width, int height);

        /// <summary>
        /// Validates a pixel position and throws an exception if it is
        /// outside the bounds of the current <see cref="TextureData"/>.
        /// If the parameters are valid, calling this method has no effect.
        /// </summary>
        /// <param name="x">
        /// The X position of the pixel in texture coordinate space,
        /// where the origin is in the top left corner and the X axis 
        /// "points right".
        /// </param>
        /// <param name="y">
        /// The Y position of the pixel in texture coordinate space,
        /// where the origin is in the top left corner and the Y axis
        /// "points downwards".
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when the parameters are less than 0,
        /// <paramref name="x"/> is greater than/equal to <see cref="Width"/>
        /// or when <paramref name="y"/> is greater than/equal to
        /// <see cref="Height"/>.
        /// </exception>
        protected void AssertValidPixelPosition(int x, int y)
        {
            if (x < 0 || x >= Size.Width)
                throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= Size.Height)
                throw new ArgumentOutOfRangeException(nameof(y));
        }

        /// <summary>
        /// Validates a texture section and throws an exception
        /// if it exceeds the bounds of the current <see cref="TextureData"/>,
        /// or the area is empty. If the <paramref name="section"/> is valid, 
        /// calling this method has no effect.
        /// </summary>
        /// <param name="x">
        /// The X position of the area origin in texture coordinate space,
        /// where the origin is in the top left corner and the X axis 
        /// "points right".
        /// </param>
        /// <param name="y">
        /// The Y position of the requested pixel in texture coordinate space,
        /// where the origin is in the top left corner and the Y axis
        /// "points downwards".
        /// </param>
        /// <param name="width">
        /// The width of the area.
        /// </param>
        /// <param name="height">
        /// The height of the area.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when the specified parameters exceed the valid ranges
        /// by being negative (or less than 1 for <paramref name="width"/>
        /// and <paramref name="height"/>) or when the area would exceed
        /// the bounds of the current instance.
        /// </exception>
        protected void AssertValidTextureSection(int x, int y,
            int width, int height)
        {
            if (x < 0 || x >= Size.Width)
                throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= Size.Height)
                throw new ArgumentOutOfRangeException(nameof(y));
            if (width <= 0 || x + width > Size.Width)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0 || y + height > Size.Height)
                throw new ArgumentOutOfRangeException(nameof(height));
        }

        /// <summary>
        /// Initializes a new texture with a solid color and the size 2x2.
        /// </summary>
        /// <param name="color">The color of the texture.</param>
        /// <returns>
        /// A new instance of the <see cref="TextureData"/> class.
        /// </returns>
        public static TextureData CreateSolidColor(Color color)
        {
            return new TextureDataGenerators.SolidColor(new Size(2, 2))
            { Color = color };
        }

        /// <summary>
        /// Initializes a new texture with a solid color.
        /// </summary>
        /// <param name="color">The color of the texture.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <returns>
        /// A new instance of the <see cref="TextureData"/> class.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when the <paramref name="width"/> or the 
        /// <paramref name="height"/> are invalid.
        /// </exception>
        public static TextureData CreateSolidColor(Color color, int width,
            int height)
        {
            return new TextureDataGenerators.SolidColor(
                new Size(width, height)) { Color = color };
        }

        /// <summary>
        /// Initializes a new texture with a checkerboard pattern with 
        /// two colors.
        /// </summary>
        /// <param name="primaryColor">
        /// The primary color of the checkerboard pattern 
        /// (e.g. <see cref="Color.Black"/>)
        /// </param>
        /// <param name="secondaryColor">
        /// The secondary color of the checkerboard pattern 
        /// (e.g. <see cref="Color.White"/>)
        /// </param>
        /// <param name="patternSize">
        /// The width/height of a single square in the pattern.
        /// Must be 1 or larger.
        /// </param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <returns>
        /// A new instance of the <see cref="TextureData"/> class.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when the <paramref name="width"/> or the 
        /// <paramref name="height"/> are invalid or when 
        /// <paramref name="patternSize"/> is less than 1.
        /// </exception>
        public static TextureData CreateCheckerboardPattern(Color primaryColor,
            Color secondaryColor, int patternSize, int width, int height)
        {
            if (patternSize < 1)
                throw new ArgumentOutOfRangeException(nameof(patternSize));

            return new TextureDataGenerators.Checkerboard(
                new Size(width, height))
            {
                PrimaryColor = primaryColor,
                SecondaryColor = secondaryColor,
                PatternSize = patternSize
            };
        }

        /// <summary>
        /// Loads a <see cref="TextureData"/> instance in the internal format 
        /// used by <see cref="NativeFormatHandler"/> from the current position
        /// of a <paramref name="stream"/>.
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
        /// A new instance of the <see cref="TextureData"/> class.
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
        /// <exception cref="System.IO.IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <see cref="BaseStream"/> of 
        /// <paramref name="streamWrapper"/> was disposed.
        /// </exception>
        internal static TextureData Load(Stream stream,
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
                if (resourceType != ResourceType.Texture)
                    throw new FormatException("The specified resource " +
                        "was no texture resource.");
            }

            uint width = stream.ReadUnsignedInteger();
            uint height = stream.ReadUnsignedInteger();

            if (width < 1) throw new FormatException("The image width " +
                "was less than 1, which is too low.");
            if (height < 1) throw new FormatException("The image height " +
                "was less than 1, which is too low.");
            if (width > MaxWidth) throw new FormatException(
                "The image width was greater than " + MaxWidth + ", which " +
                "is too low.");
            if (height > MaxHeight) throw new FormatException(
                "The image height was greater than " + MaxHeight + ", which " +
                "is too low.");

            Color[] pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = stream.Read<Color>();

            return new MemoryTexure(new Size((int)width, (int)height), pixels);
        }

        /// <summary>
        /// Saves the current <see cref="TextureData"/> instance in the 
        /// internal format used by <see cref="NativeFormatHandler"/> to
        /// the current position of the <paramref name="stream"/>.
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
        /// <exception cref="System.IO.IOException">
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
                NativeFormatHandler.WriteEntryHeader(ResourceType.Texture,
                    stream);

            stream.WriteUnsignedInteger((uint)Size.Width);
            stream.WriteUnsignedInteger((uint)Size.Height);

            for (int y = 0; y < Size.Height; y++)
            {
                Color[] row = GetRegion(0, y, Size.Width, 1);

                for (int x = 0; x < Size.Width; x++) stream.Write(row[x]);
            }
        }
    }
}
