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
using System.IO;

namespace Eterra.IO
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
        #region Internally used Texture implementations.
        internal class MemoryTexure : TextureData
        {
            private Color[] data;

            public MemoryTexure(Size size, Color[] pixelData) : base(size)
            {
                if (pixelData == null)
                    throw new ArgumentNullException(nameof(pixelData));
                if (pixelData.Length != (Size.Width * Size.Height))
                    throw new ArgumentException("The pixel data length was " +
                        "invalid!");

                data = pixelData;
            }

            public override Color GetPixel(int tx, int ty)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (tx < 0 || tx >= Size.Width)
                    throw new ArgumentOutOfRangeException(nameof(tx));
                if (ty < 0 || ty >= Size.Height)
                    throw new ArgumentOutOfRangeException(nameof(ty));

                return data[ty * Size.Width + tx];
            }

            public override Color GetPixel(int index)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (index < 0 || index >= data.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                else return data[index];
            }

            public override Color[] GetPixels(int index, int count, 
                bool throwOnCountOverflow)
            {
                ValidateIndexParams(index, ref count, throwOnCountOverflow);

                Color[] pixels = new Color[count];
                Array.ConstrainedCopy(data, index, pixels, 0, pixels.Length);
                return pixels;
            }

            public override Color[] GetPixels(int tx, int ty, int width,
                int height)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(GetType().Name);

                ValidateTextureSection(tx, ty, width, height);

                Color[] areaData = new Color[width * height];

                if (tx == 0 && width == Size.Width)
                {
                    Array.ConstrainedCopy(data, ty * Size.Width, areaData, 0, 
                        width * height);
                }
                else
                {
                    for (int y = ty; y < (tx + height); y++)
                    {
                        Array.ConstrainedCopy(data, y * Size.Width + tx,
                            areaData, y * width, width);
                    }
                }

                return areaData;
            }

            protected override void Dispose(bool disposing)
            {
                data = null;
            }
        }

        internal class TextureWrapper : TextureData
        {
            private PixelRetriever retrievePixel;

            public TextureWrapper(Size size, PixelRetriever pixelRetriever) 
                : base(size)
            {
                retrievePixel = pixelRetriever ??
                    throw new ArgumentNullException(nameof(pixelRetriever));
            }

            public override Color GetPixel(int tx, int ty)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (tx < 0 || tx >= Size.Width)
                    throw new ArgumentOutOfRangeException(nameof(tx));
                if (ty < 0 || ty >= Size.Height)
                    throw new ArgumentOutOfRangeException(nameof(ty));

                return retrievePixel(tx, ty);
            }

            protected override void Dispose(bool disposing)
            {
                retrievePixel = null;
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
        /// Gets a value indicating whether the current instance
        /// has been disposed (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public override bool IsDisposed => base.IsDisposed ||
            (PixelDataPointer != null && PixelDataPointer.IsDisposed);

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
        /// Gets a boolean indicating whether the data of the current 
        /// <see cref="TextureData"/> can be accessed directly through the 
        /// <see cref="PixelDataPointer"/> property (<c>true</c>) or if 
        /// pointers are not supported and <see cref="PixelDataPointer"/>
        /// is null (<c>false</c>).
        /// </summary>
        public bool SupportsPointers => PixelDataPointer != null;

        /// <summary>
        /// Gets a <see cref="MemoryPointer"/> to the pixel data in unmanaged
        /// memory with the specific color pixel layout specified by
        /// <see cref="MemoryPointer.ElementType"/> or null, if
        /// <see cref="SupportsPointers"/> is <c>false</c>.
        /// Cast the value of this property to a 
        /// <see cref="MemoryPointer{DataT}"/> instance with 
        /// the specific color type as DataT to access the pixel data using
        /// managed methods.
        /// </summary>
        /// <remarks>
        /// If this property is not null and disposed, the current
        /// <see cref="TextureData"/> instance is marked as disposed as well.
        /// </remarks>
        public virtual MemoryPointer PixelDataPointer { get; } = null;

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
        /// Gets a single pixel from the <see cref="TextureData"/>.
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
        /// Is thrown when <paramref name="tx"/> is less than 0 or greater than
        /// or equal to <see cref="Width"/> or when <paramref name="ty"/> is 
        /// less than 0 or greater than or equal to <see cref="Height"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the object data was disposed and can't be accessed
        /// anymore.
        /// </exception>
        public abstract Color GetPixel(int tx, int ty);

        /// <summary>
        /// Gets a single pixel from the <see cref="TextureData"/>.
        /// </summary>
        /// <param name="index">
        /// The index of the pixel.
        /// </param>
        /// <returns>The <see cref="Color"/> of the requested pixel.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="tx"/> is less than 0 or greater 
        /// than/equal to <see cref="Width"/> or when <paramref name="ty"/>
        /// is less than 0 or greater than/equal to <see cref="Height"/>.
        /// </exception>
        /// <remarks>
        /// If the pixel data is stored in a two-dimensional array-like 
        /// structure line by line, a X/Y coordinate can be converted to 
        /// the pixel index with <c>y * Width + x</c>.
        /// If the index is given, it can easily be converted into X/Y
        /// coordinates by using <c>index % Width</c> for X and
        /// <c>index / Width</c> for Y - assuming that <c>index</c> and
        /// <c>Width</c> are both integer numbers.
        /// </remarks>
        public virtual Color GetPixel(int index)
        {
            GetPixelPosition(index, out int tx, out int ty);
            return GetPixel(tx, ty);
        }

        /// <summary>
        /// Gets the index of a pixel at a specific position.
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
        /// The index of the pixel at the specified position.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="tx"/> is less than 0 or greater 
        /// than/equal to <see cref="Width"/> or when <paramref name="ty"/>
        /// is less than 0 or greater than/equal to <see cref="Height"/>.
        /// </exception>
        public virtual int GetPixelIndex(int tx, int ty)
        {
            if (tx < 0 || tx >= Size.Width)
                throw new ArgumentOutOfRangeException(nameof(tx));
            if (ty < 0 || ty >= Size.Height)
                throw new ArgumentOutOfRangeException(nameof(ty));

            return ty * Size.Width + tx;
        }

        /// <summary>
        /// Gets the position of a pixel with a specific index.
        /// </summary>
        /// <param name="index">The index of the requested pixel.</param>
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
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="tx"/> is less than 0 or greater 
        /// than/equal to <see cref="Width"/> or when <paramref name="ty"/>
        /// is less than 0 or greater than/equal to <see cref="Height"/>.
        /// </exception>
        public virtual void GetPixelPosition(int index, out int tx, out int ty)
        {
            if (index < 0 || index >= PixelCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            tx = index % Size.Width;
            ty = index / Size.Width;
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
        /// <param name="tx">
        /// The X position of the area origin in texture coordinate space,
        /// where the origin is in the top left corner and the X axis 
        /// "points right".
        /// </param>
        /// <param name="ty">
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
        public virtual Color[] GetPixels(int tx, int ty, 
            int width, int height)
        {
            ValidateTextureSection(tx, ty, width, height);

            Color[] pixels = new Color[width * height];

            try
            {
                int arrayIterator = 0;
                for (int y = ty; y < height + ty; y++)
                {
                    for (int x = tx; x < width + tx; x++)
                    {
                        pixels[arrayIterator] = GetPixel(x, y);
                        arrayIterator++;
                    }
                }
            }
            catch (ObjectDisposedException) { throw; }

            return pixels;
        }

        /// <summary>
        /// Gets pixels from the <see cref="TextureData"/> by its index.
        /// </summary>
        /// <param name="index">
        /// The index of the first pixel.
        /// </param>
        /// <param name="count">
        /// The amount of pixels to be retrieved.
        /// </param>
        /// <param name="throwOnCountOverflow">
        /// <c>true</c> to throw an <see cref="ArgumentOutOfRangeException"/>,
        /// if the <paramref name="index"/> plus the 
        /// <paramref name="count"/> would exceed the
        /// <see cref="PixelCount"/>, <c>false</c> to just return a smaller
        /// array then.
        /// </param>
        /// <returns>An array of <see cref="Color"/> instances.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="index"/> is less than 0
        /// or greater than/equal to <see cref="PixelCount"/>, or when
        /// <paramref name="throwOnCountOverflow"/> is <c>true</c> and
        /// <paramref name="index"/> plus <paramref name="count"/> is 
        /// greater than/equal to <see cref="PixelCount"/>.
        /// </exception>
        /// <remarks>
        /// See the remarks of <see cref="GetPixel(int)"/> for more
        /// information about pixel indicies.
        /// </remarks>
        public virtual Color[] GetPixels(int index, int count,
            bool throwOnCountOverflow)
        {
            ValidateIndexParams(index, ref count, throwOnCountOverflow);

            Color[] pixels = new Color[count];
            for (int i=0; i < (count + index); i++)
                pixels[i] = GetPixel(i);
            return pixels;
        }

        /// <summary>
        /// Validates a pixel position and throws an exception if it is
        /// outside the bounds of the current <see cref="TextureData"/>.
        /// If the parameters are valid, calling this method has no effect.
        /// </summary>
        /// <param name="tx">
        /// The X position of the pixel in texture coordinate space,
        /// where the origin is in the top left corner and the X axis 
        /// "points right".
        /// </param>
        /// <param name="ty">
        /// The Y position of the pixel in texture coordinate space,
        /// where the origin is in the top left corner and the Y axis
        /// "points downwards".
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when the parameters are less than 0,
        /// <paramref name="tx"/> is greater than/equal to <see cref="Width"/>
        /// or when <paramref name="ty"/> is greater than/equal to
        /// <see cref="Height"/>.
        /// </exception>
        protected void ValidatePixelPosition(int tx, int ty)
        {
            if (tx < 0 || tx >= Size.Width)
                throw new ArgumentOutOfRangeException(nameof(tx));
            if (ty < 0 || ty >= Size.Height)
                throw new ArgumentOutOfRangeException(nameof(ty));
        }

        /// <summary>
        /// Validates a texture section and throws an exception
        /// if it exceeds the bounds of the current <see cref="TextureData"/>,
        /// or the area is empty. If the <paramref name="section"/> is valid, 
        /// calling this method has no effect.
        /// </summary>
        /// <param name="tx">
        /// The X position of the area origin in texture coordinate space,
        /// where the origin is in the top left corner and the X axis 
        /// "points right".
        /// </param>
        /// <param name="ty">
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
        protected void ValidateTextureSection(int tx, int ty,
            int width, int height)
        {
            if (tx < 0 || tx >= Size.Width)
                throw new ArgumentOutOfRangeException(nameof(tx));
            if (ty < 0 || ty >= Size.Height)
                throw new ArgumentOutOfRangeException(nameof(ty));
            if (width <= 0 || tx + width > Size.Width)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0 || ty + height > Size.Height)
                throw new ArgumentOutOfRangeException(nameof(height));
        }

        /// <summary>
        /// Validates the <paramref name="count"/> parameter and corrects
        /// it, if <paramref name="throwOnCountOverflow"/> is <c>false</c>,
        /// so that it will be in the bounds of <see cref="PixelCount"/>.
        /// </summary>
        /// <param name="index">
        /// The index of the first pixel.
        /// </param>
        /// <param name="count">
        /// The desired amount of pixels to be retrieved. If
        /// <paramref name="throwOnCountOverflow"/> is <c>false</c>, this
        /// parameter is corrected, if necessary, to fit into the bounds
        /// of <see cref="PixelCount"/>.
        /// </param>
        /// <param name="throwOnCountOverflow">
        /// <c>true</c> to throw an <see cref="ArgumentOutOfRangeException"/>,
        /// if the <paramref name="index"/> plus the 
        /// <paramref name="count"/> would exceed the
        /// <see cref="PixelCount"/>, <c>false</c> to correct 
        /// <paramref name="count"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="index"/> is less than 0
        /// or greater than/equal to <see cref="PixelCount"/>, or when
        /// <paramref name="throwOnCountOverflow"/> is <c>true</c> and
        /// <paramref name="index"/> plus <paramref name="count"/> is 
        /// greater than/equal to <see cref="PixelCount"/>.
        /// </exception>
        protected void ValidateIndexParams(int index, ref int count,
            bool throwOnCountOverflow)
        {
            if (index < 0 || index >= PixelCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            if ((index + count) >= PixelCount && throwOnCountOverflow)
                throw new ArgumentOutOfRangeException(nameof(count));
            else count = PixelCount - index - 1;
        }

        /// <summary>
        /// Initializes a new <see cref="TextureData"/> from existing data.
        /// </summary>
        /// <param name="width">
        /// The width of the existing source image and the new texture.
        /// </param>
        /// <param name="height">
        /// The height of the existing source image and the new texture.
        /// </param>
        /// <param name="pixelRetriever">
        /// A method which gets the <see cref="Color"/> of a pixel in the
        /// source image at a specific X and Y position. This method is not
        /// used or stored beyond the scope of this method.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="TextureData"/> class.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when the <paramref name="width"/> or 
        /// <paramref name="height"/> were less than/equal to zero or 
        /// greater than <see cref="MaxWidth"/>/<see cref="MaxWidth"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="width"/> or
        /// <paramref name="height"/> exceeded the dimensions accessible
        /// by the <paramref name="pixelRetriever"/>.
        /// </exception>
        public static TextureData Create(int width, int height,
            PixelRetriever pixelRetriever)
        {
            return Create(width, height, pixelRetriever, true);
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
                for (int x = 0; x < Size.Width; x++)
                    stream.Write(GetPixel(x, y));
        }

        /// <summary>
        /// Initializes a new <see cref="TextureData"/> from existing data.
        /// </summary>
        /// <param name="width">
        /// The width of the existing source image and the new texture.
        /// </param>
        /// <param name="height">
        /// The height of the existing source image and the new texture.
        /// </param>
        /// <param name="pixelRetriever">
        /// A method which gets the <see cref="Color"/> of a pixel in the
        /// source image at a specific X and Y position.
        /// </param>
        /// <param name="bufferData">
        /// <c>true</c> to create a complete copy of the image data and store 
        /// it in the new <see cref="TextureData"/> instance (needs more 
        /// memory and longer to initialize, but has a predictable, constant 
        /// and higher GPU transfer speed and reliability), 
        /// <c>false</c> to create a wrapper around the specified delegate
        /// and redirect all image data requests of the new 
        /// <see cref="TextureData"/> (<see cref="GetPixel(int, int)"/>,...)
        /// to the specified <paramref name="pixelRetriever"/> (faster 
        /// initialisation of the texture and less memory usage, but less 
        /// predictability on speed during GPU transfer).
        /// If the specified <paramref name="pixelRetriever"/> accesses the
        /// data of an object implementing <see cref="IDisposable"/>, 
        /// it is highly recommended to specifiy <c>true</c> as 
        /// parameter value.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="TextureData"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="pixelRetriever"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when the <paramref name="width"/> or 
        /// <paramref name="height"/> were less than/equal to zero or 
        /// greater than <see cref="MaxWidth"/>/<see cref="MaxWidth"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="width"/> or
        /// <paramref name="height"/> exceeded the dimensions accessible
        /// by the <paramref name="pixelRetriever"/>.
        /// </exception>
        internal static TextureData Create(int width, int height,
            PixelRetriever pixelRetriever, bool bufferData)
        {
            if (pixelRetriever == null)
                throw new ArgumentNullException(nameof(pixelRetriever));

            if (width <= 0 || width > MaxWidth)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0 || height > MaxHeight)
                throw new ArgumentOutOfRangeException(nameof(height));

            if (bufferData)
            {
                Color[] pixelData = new Color[width * height];

                try
                {
                    int arrayIterator = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pixelData[arrayIterator] = pixelRetriever(x, y);
                            arrayIterator++;
                        }
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new ArgumentException("The specified dimensions " +
                        "exceeded the accessible area of the image source!");
                }

                return new MemoryTexure(new Size(width, height), pixelData);
            }
            else return new TextureWrapper(new Size(width, height), 
                pixelRetriever);
        }
    }
}
