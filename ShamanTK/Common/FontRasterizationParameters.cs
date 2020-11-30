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

using ShamanTK.IO;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;

namespace ShamanTK.Common
{
    /// <summary>
    /// Provides various properties which define how a system font or standard
    /// font file should appear as rasterized sprite font.
    /// </summary>
    public class FontRasterizationParameters
    {
        private const char CharSetFlagsSeparator = ',';

        /// <summary>
        /// Defines the minimum size for a font (in pixels).
        /// </summary>
        public const float FontSizeMinimum = 3;

        /// <summary>
        /// Defines the query key for the <see cref="SizePx"/> parameter.
        /// Its value is the string representation of a positive floating-point
        /// number greater than zero (with two decimals and in the
        /// <see cref="CultureInfo.InvariantCulture"/>).
        /// Like all query parameters of <see cref="FontRasterizationParameters"/>, this 
        /// parameter is optional - if unspecified, its value should default 
        /// to <see cref="SizePx"/> of <see cref="Default"/>.
        /// </summary>
        public const string QueryKeySize = "size";

        /// <summary>
        /// Defines the query key for the <see cref="Bold"/> parameter.
        /// Its value is the string representation of a <see cref="bool"/>.
        /// Like all query parameters of <see cref="FontRasterizationParameters"/>, this 
        /// parameter is optional - if unspecified, its value should default 
        /// to <see cref="Bold"/> of <see cref="Default"/>.
        /// </summary>
        public const string QueryKeyBoldStyle = "bold";

        /// <summary>
        /// Defines the query key for the <see cref="Italic"/> parameter.
        /// Its value is the string representation of a <see cref="bool"/>.
        /// Like all query parameters of <see cref="FontRasterizationParameters"/>, this 
        /// parameter is optional - if unspecified, its value should default 
        /// to <see cref="Italic"/> of <see cref="Default"/>.
        /// </summary>
        public const string QueryKeyItalicStyle = "italic";

        /// <summary>
        /// Defines the query key for the <see cref="CharSet"/> parameter, which consists
        /// of the string representations of one or more flags of the 
        /// <see cref="Common.CharSet"/> enum, seperated by a comma.
        /// Like all query parameters of <see cref="FontRasterizationParameters"/>, this 
        /// parameter is optional - if unspecified, its value should default
        /// to <see cref="CharSet"/> of <see cref="Default"/>.
        /// </summary>
        public const string QueryKeyCharSet = "charset";

        /// <summary>
        /// Defines the query key for the anti aliasing 
        /// </summary>
        public const string QueryKeyUseAntiAliasing = "useAntiAliasing";

        /// <summary>
        /// Defines the query key for the font family parameter, which 
        /// can be optionally specified via the
        /// <see cref="ToSystemFontResourcePath(string)"/> method.
        /// This query parameter should only be considered if the 
        /// <see cref="FileSystemPath"/> of the resulting 
        /// <see cref="ResourcePath"/> is equal to 
        /// <see cref="SystemFontFileAlias"/> and is ignored otherwise.
        /// </summary>
        public const string QueryKeyFontFamily = "family";

        /// <summary>
        /// Defines the file extension of the "alias file" which should be
        /// supported by <see cref="IResourceFormatHandler"/> instances capable 
        /// of importing system fonts as <see cref="SpriteFontData"/> 
        /// instances.
        /// </summary>
        public const string SystemFontFileAliasFormat = "systemfont";

        /// <summary>
        /// Gets a new <see cref="FontRasterizationParameters"/> instance with
        /// all values set to their default.
        /// </summary>
        public static FontRasterizationParameters Default => 
            new FontRasterizationParameters();

        /// <summary>
        /// Gets the <see cref="FileSystemPath"/> of the alias file in the
        /// root of any <see cref="IFileSystem"/>, that notifies the supporting
        /// <see cref="IResourceFormatHandler"/> to load an installed system
        /// font instead of a font file from that <see cref="IFileSystem"/>.
        /// The file type of this file (as defined by 
        /// <see cref="SystemFontFileAlias"/>) should not be used in any other
        /// way than this!
        /// </summary>
        public static FileSystemPath SystemFontFileAlias { get; }
            = new FileSystemPath("/." + SystemFontFileAliasFormat);

        /// <summary>
        /// Gets a value indicating whether the font loaded with this
        /// <see cref="FontRasterizationParameters"/> instance should be bold 
        /// (<c>true</c>) or not (<c>false</c>). This property can be combined 
        /// with <see cref="Italic"/>. If the font doesn't define a bold style, 
        /// this property is ignored.
        /// </summary>
        public bool Bold { get; set; } = false;

        /// <summary>
        /// Gets a value indicating whether the font loaded with this
        /// <see cref="FontRasterizationParameters"/> instance should be italic 
        /// (<c>true</c>) or not (<c>false</c>). This property can be combined 
        /// with <see cref="Bold"/>. If the font doesn't define a italic style, 
        /// this property is ignored.
        /// </summary>
        public bool Italic { get; set; } = false;

        /// <summary>
        /// Gets the size of the font (as sprites) in pixels.
        /// </summary>
        /// <remarks>
        /// A higher value means a better look for letters taking up much space
        /// on the screen (especially when they're close), but could also 
        /// result into the sprite font not being able to be created (due to 
        /// the letters taking up too much size and exceeding the maximum 
        /// texture size).
        /// </remarks>
        public float SizePx
        {
            get => sizePx;
            set => sizePx = Math.Max(FontSizeMinimum, value);
        }
        private float sizePx = 32;

        /// <summary>
        /// Gets a value indicating wheter the font is rasterized using 
        /// anti-aliasing for smoother edges (<c>true</c>, default) or
        /// whether anti-aliasing is turned off and the edges are "hard"
        /// (<c>false</c>).
        /// </summary>
        public bool UseAntiAliasing { get; set; } = true;

        /// <summary>
        /// Gets the characters which should be available in the 
        /// <see cref="SpriteFontData"/> created with this 
        /// <see cref="FontRasterizationParameters"/> instance. Supports 
        /// bitwise combination of different elements (e.g. 
        /// <c><see cref="CharSet.BasicLatin"/> | 
        /// <see cref="CharSet.Latin1Supplement"/></c>).
        /// </summary>
        public CharSet CharSet { get; set; } = CharSet.DefaultEuropeAmerica;

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="FontRasterizationParameters"/> class with the 
        /// default parameters.
        /// </summary>
        public FontRasterizationParameters() { }

        /// <summary>
        /// Creates a new <see cref="FontRasterizationParameters"/> instance 
        /// with the parameters from a <see cref="ResourcePath"/> and its 
        /// contained <see cref="ResourceQuery"/>. The default values are used 
        /// for parameters that are undefined in the provided
        /// <see cref="ResourcePath.Query"/>.
        /// </summary>
        /// <param name="path">
        /// The resource path to be parsed.
        /// </param>
        /// <param name="isSystemFont">
        /// <c>true</c> if the <paramref name="path"/> specified a system font
        /// (which means the <paramref name="fontFamily"/> contains the name
        /// of the system font family which should be loaded),
        /// <c>false</c> if the <paramref name="path"/> specified a font file
        /// (which means the <paramref name="fontFilePath"/> contains the path
        /// of the font file to be loaded in a <see cref="IFileSystem"/>).
        /// </param>
        /// <param name="fontFilePath">
        /// The path to the font file or <see cref="FileSystemPath.Empty"/>, if
        /// the specified <paramref name="path"/> references a system font 
        /// (with the <see cref="SystemFontFileAlias"/> alias file).
        /// </param>
        /// <param name="fontFamily">
        /// The name of the font family or null, if the specified 
        /// <paramref name="path"/> references a font file in the current
        /// <see cref="IFileSystem"/> instead of a system font.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="FontRasterizationParameters"/> 
        /// class.
        /// </returns>
        public static FontRasterizationParameters FromResourcePath(
            ResourcePath path, out bool isSystemFont,
            out FileSystemPath fontFilePath, out string fontFamily)
        {
            FontRasterizationParameters fontStyle = FromResourceQuery(
                path.Query, out string fontFamilyValue);
            if (path.Path == SystemFontFileAlias)
            {
                fontFamily = fontFamilyValue;
                fontFilePath = FileSystemPath.Empty;
                isSystemFont = true;
            }
            else
            {
                fontFamily = null;
                fontFilePath = path.Path;
                isSystemFont = false;
            }
            return fontStyle;
        }

        private static FontRasterizationParameters FromResourceQuery(
            ResourceQuery query, out string fontFamily)
        {
            NameValueCollection parameters = query.ToNameValueCollection();
            string sizeValue = parameters[QueryKeySize];
            string boldValue = parameters[QueryKeyBoldStyle];
            string italicValue = parameters[QueryKeyItalicStyle];
            string useAntiAliasingValue = parameters[QueryKeyUseAntiAliasing];
            string charSetValue = parameters[QueryKeyCharSet];
            fontFamily = parameters[QueryKeyFontFamily];

            FontRasterizationParameters @default = Default;
            float size = @default.SizePx;
            bool bold = @default.Bold;
            bool italic = @default.Italic;
            bool useAntiAliasing = @default.UseAntiAliasing;
            CharSet charSet = @default.CharSet;

            if (!string.IsNullOrWhiteSpace(sizeValue) && float.TryParse(
                sizeValue, NumberStyles.Float, CultureInfo.InvariantCulture,
                out float parsedSize)) size = parsedSize;
            if (!string.IsNullOrWhiteSpace(boldValue) && bool.TryParse(
                boldValue, out bool parsedBold)) bold = parsedBold;
            if (!string.IsNullOrWhiteSpace(italicValue) && bool.TryParse(
                italicValue, out bool parsedItalic)) italic = parsedItalic;
            if (!string.IsNullOrWhiteSpace(useAntiAliasingValue) && 
                bool.TryParse(useAntiAliasingValue, 
                out bool parsedUseAntiAliasingValue))
                useAntiAliasing = parsedUseAntiAliasingValue;
            if (!string.IsNullOrWhiteSpace(charSetValue))
            {
                CharSet parsedCharSet = 0; 
                string[] charSetValues = charSetValue.Split(
                    CharSetFlagsSeparator);

                foreach (string charSetFlagValue in charSetValue.Split(
                    CharSetFlagsSeparator))
                {
                    if (Enum.TryParse(charSetFlagValue,
                        out CharSet parsedCharSetFlagValue))
                        parsedCharSet |= parsedCharSetFlagValue;
                }

                charSet = parsedCharSet;
            }

            return new FontRasterizationParameters()
            {
                SizePx = size,
                Bold = bold,
                Italic = italic,
                UseAntiAliasing = useAntiAliasing,
                CharSet = charSet
            };
        }

        /// <summary>
        /// Creates a new <see cref="FontRasterizationParameters"/> instance 
        /// with the parameters from a <see cref="ResourceQuery"/>. The default
        /// values are used for parameters that are undefined in the provided 
        /// <paramref name="query"/>.
        /// </summary>
        /// <param name="query"></param>
        /// <returns>
        /// A new instance of the <see cref="FontRasterizationParameters"/> 
        /// class.
        /// </returns>
        public static FontRasterizationParameters FromResourceQuery(
            ResourceQuery query)
        {
            return FromResourceQuery(query, out _);
        }

        private ResourceQuery ToResourceQuery(string fontFamily = null)
        {
            FontRasterizationParameters @default = Default;
            NameValueCollection queryParameters = new NameValueCollection();

            if (!string.IsNullOrWhiteSpace(fontFamily))
                queryParameters.Add(QueryKeyFontFamily, fontFamily);
            if (SizePx != @default.SizePx) queryParameters.Add(QueryKeySize, 
                SizePx.ToString("N2", CultureInfo.InvariantCulture));
            if (Bold != @default.Bold) queryParameters.Add(QueryKeyBoldStyle,
                Bold.ToString().ToLower());
            if (Italic != @default.Italic) queryParameters.Add(
                QueryKeyItalicStyle, Italic.ToString().ToLower());
            if (UseAntiAliasing != @default.UseAntiAliasing) 
                queryParameters.Add(QueryKeyUseAntiAliasing, 
                    UseAntiAliasing.ToString().ToLower());

            if (CharSet != Default.CharSet)
            {
                CharSet[] charSetFlags = 
                    (CharSet[])Enum.GetValues(typeof(CharSet));
                StringBuilder charSetStringBuilder = new StringBuilder();
                for (int i = 0; i < charSetFlags.Length; i++)
                {
                    if (CharSet.HasFlag(charSetFlags[i]))
                    {
                        charSetStringBuilder.Append(
                            charSetFlags[i].ToString());
                        if ((i + 1) < charSetFlags.Length)
                            charSetStringBuilder.Append(CharSetFlagsSeparator);
                    }
                }
                queryParameters.Add(QueryKeyCharSet, 
                    charSetStringBuilder.ToString());
            }

            return new ResourceQuery(queryParameters);
        }

        /// <summary>
        /// Creates a new <see cref="ResourcePath"/> instance using the
        /// parameters from the current 
        /// <see cref="FontRasterizationParameters"/> instance
        /// and a <see cref="FileSystemPath"/> to a font file on the current
        /// <see cref="IFileSystem"/>.
        /// To create a <see cref="ResourcePath"/> for a system font, use
        /// <see cref="ToSystemFontResourcePath(string)"/>.
        /// </summary>
        /// <param name="fontFilePath">
        /// The path to the font file on a <see cref="IFileSystem"/>.
        /// Must not be <see cref="SystemFontFileAlias"/>.
        /// </param>
        /// <returns>A new <see cref="ResourcePath"/> instance.</returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="fontFilePath"/> is equal to
        /// <see cref="SystemFontFileAlias"/>.
        /// </exception>
        public ResourcePath ToFontFileResourcePath(FileSystemPath fontFilePath)
        {
            if (fontFilePath == SystemFontFileAlias)
                throw new ArgumentException("The specified font file " +
                    "path is no valid font file path, but the alias file " +
                    "for a system font - without a font family specified, " +
                    "this is invalid in this context.");

            return new ResourcePath(fontFilePath, ToResourceQuery());
        }

        /// <summary>
        /// Creates a new <see cref="ResourcePath"/> instance using the
        /// parameters from the current 
        /// <see cref="FontRasterizationParameters"/> instance
        /// and the name of a font family currently installed on the system.
        /// To create a <see cref="ResourcePath"/> for a font file in a
        /// <see cref="IFileSystem"/>, use
        /// <see cref="ToFontFileResourcePath(FileSystemPath)"/>.
        /// </summary>
        /// <param name="fontFamily"></param>
        /// <returns>A new <see cref="ResourcePath"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fontFamily"/> is null.
        /// </exception>
        public ResourcePath ToSystemFontResourcePath(string fontFamily)
        {
            if (fontFamily == null)
                throw new ArgumentNullException(nameof(fontFamily));

            return new ResourcePath(SystemFontFileAlias,
                ToResourceQuery(fontFamily));
        }

        public static implicit operator ResourceQuery(
            FontRasterizationParameters fontStyle)
        {
            if (fontStyle == null)
                throw new ArgumentNullException(nameof(fontStyle));

            return fontStyle.ToResourceQuery();
        }
    }
}
