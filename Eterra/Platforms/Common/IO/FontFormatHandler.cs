/*
 * Eterra Framework Platforms
 * Eterra platform providers for various operating systems and devices.
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

//Uncomment the following flag to draw the inner and outer character bound
//into the charmap and save the resulting sprite font image to the path
//defined in the "SpriteFontOutputPath" constant. Useful for debugging and
//understanding how that stuff works.
//#define DEBUG_DISPLAYBOUNDS

using Eterra.Common;
using Eterra.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Eterra.Platforms.Windows.IO
{
    public class FontFormatHandler : IResourceFormatHandler
    {
#if DEBUG_DISPLAYBOUNDS
        private const string SpriteFontOutputPath = "debug_spritefont.png";
#endif

        private const int MaximumSpriteFontTextureLength = 2048;

        public bool SupportsImport(string fileExtensionLowercase)
        {
            return fileExtensionLowercase == "otf" ||
                fileExtensionLowercase == "ttf" ||
                fileExtensionLowercase == 
                FontRasterizationParameters.SystemFontFileAliasFormat;
        }

        public bool SupportsExport(string fileExtensionLowercase)
        {
            return false;
        }

        private static FontStyle GetFontStyle(
            FontRasterizationParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            FontStyle style = FontStyle.Regular;
            if (parameters.Bold) style |= FontStyle.Bold;
            if (parameters.Italic) style |= FontStyle.Italic;
            return style;
        }

        private static Font LoadFontFile(IFileSystem fileSystem, 
            FileSystemPath fontFilePath, 
            FontRasterizationParameters parameters)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            PrivateFontCollection fonts = new PrivateFontCollection();
            using (Stream stream = fileSystem.OpenFile(fontFilePath, false))
            {
                byte[] buffer = stream.ReadBuffer((uint)stream.Length);
                IntPtr pointer = IntPtr.Zero;
                try
                {
                    pointer = Marshal.AllocHGlobal(buffer.Length);
                    Marshal.Copy(buffer, 0, pointer, buffer.Length);
                    fonts.AddMemoryFont(pointer, buffer.Length);
                }
                catch (Exception exc)
                {
                    throw new InvalidOperationException("The font " +
                        "resource file couldn't be copied to unmanaged " +
                        "memory/to the private font collection.", exc);
                }
                finally
                {
                    if (pointer != IntPtr.Zero) Marshal.FreeHGlobal(pointer);
                }
            }

            return new Font(fonts.Families[0], parameters.SizePx, 
                GetFontStyle(parameters), GraphicsUnit.Pixel);
        }

        private static Font LoadSystemFont(string fontFamily,
            FontRasterizationParameters parameters)
        {
            if (fontFamily == null)
                throw new ArgumentNullException(nameof(fontFamily));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            
            Font font = new Font(fontFamily, parameters.SizePx,
                    GetFontStyle(parameters), GraphicsUnit.Pixel);
            if (font.FontFamily.Name != fontFamily)
                throw new ArgumentException("No font family with the " +
                    "specified name was found on the system.");
            else return font;
        }

        public T Import<T>(ResourceManager manager, ResourcePath path)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            if (typeof(T) != typeof(SpriteFontData))
                throw new FileNotFoundException("The current importer " +
                    "only supports font files.");

            FontRasterizationParameters rasterizationParameters = 
                FontRasterizationParameters.FromResourcePath(path,
                out bool isSystemFont,
                out FileSystemPath fontFilePath,
                out string fontFamily);

            char[] charSetChars = 
                rasterizationParameters.CharSet.GetCharacters();

            if (rasterizationParameters.SizePx * Math.Sqrt(charSetChars.Length) >
                MaximumSpriteFontTextureLength)
                throw new ArgumentException("The estimated dimensions of " +
                    "the resulting sprite font texture would exceed the " +
                    "limit of " + MaximumSpriteFontTextureLength + " pixels " +
                    "with a font size of " + rasterizationParameters.SizePx + 
                    " and a char set with " + charSetChars.Length + " " +
                    "characters. With that char set, a maximum font size of " +
                    Math.Floor(MaximumSpriteFontTextureLength /
                    Math.Sqrt(charSetChars.Length)) + " would be possible.");

            Font font;
            try
            {
                if (isSystemFont)
                    font = LoadSystemFont(fontFamily, rasterizationParameters);
                else font = LoadFontFile(manager.FileSystem, fontFilePath,
                    rasterizationParameters);
            }
            catch (Exception exc)
            {
                throw new ArgumentException("The parameters couldn't be " +
                    "used to load a font.", exc);
            }
            

            return (T)(object)GenerateSpriteFont(charSetChars, font,
                Eterra.Common.Color.White, 
                rasterizationParameters.UseAntiAliasing);
        }

        private SpriteFontData GenerateSpriteFont(char[] charSet,
            Font font, Eterra.Common.Color baseColor, bool useAntiAliasing)
        {
            if (charSet == null)
                throw new ArgumentNullException(nameof(charSet));

            Dictionary<char, GlyphMapping> mappings = 
                new Dictionary<char, GlyphMapping>();

            //Initialize the required variables.
            int cellsPerRow = (int)Math.Ceiling(Math.Sqrt(charSet.Length));
            System.Drawing.Size canvasSize = GetTextureMapSize(charSet, font,
                cellsPerRow, out Dictionary<char, SizeF> glyphSizes);
            float fontHeightAbs = font.GetHeight();
            float fontHeightRel = fontHeightAbs / canvasSize.Height;

            //Create a string format for measuring the metrics of each glyph.
            StringFormat glyphFormat = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            glyphFormat.SetMeasurableCharacterRanges(new CharacterRange[]
                { new CharacterRange(0, 1) });

            //Draw each character to a new bitmap, where each row contains an 
            //equal amount of glyphs, and store the position information and 
            //additional glyph parameters to the output mappings dictionary.
            float currentX = 0, currentY = 0;
            int currentCell = 0;

            Bitmap charBitmap = new Bitmap(canvasSize.Width, 
                canvasSize.Height, PixelFormat.Format32bppArgb);
            using (System.Drawing.Graphics graphics = 
                System.Drawing.Graphics.FromImage(charBitmap))
            {
                if (useAntiAliasing)
                    graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                else
                    graphics.TextRenderingHint = 
                        TextRenderingHint.SingleBitPerPixel;
                SolidBrush brush = new SolidBrush(
                    System.Drawing.Color.FromArgb(baseColor.Alpha, baseColor.R, 
                    baseColor.G, baseColor.B));

                foreach (KeyValuePair<char, SizeF> glyph in glyphSizes)
                {
                    RectangleF glyphTarget = new RectangleF(currentX, currentY,
                        glyph.Value.Width, fontHeightAbs);

                    //Calculate the bearing of the character (relative values).
                    Region[] regions = graphics.MeasureCharacterRanges(
                        glyph.Key.ToString(), font, glyphTarget, glyphFormat);
                    RectangleF glyphBounds = regions[0].GetBounds(graphics);
                    float bearingLeft = 
                        (glyphBounds.Left - glyphTarget.Left)
                        / canvasSize.Width;
                    float glyphWidth = (glyphBounds.Width > 0 ?
                        glyphBounds.Width : glyphTarget.Width) 
                        / canvasSize.Width;

                    //Draws the string into the character bitmap at the current
                    //position into a adequately sized rectangle.
                    graphics.DrawString(glyph.Key.ToString(), font,
                        brush, glyphTarget, glyphFormat);

#if DEBUG_DISPLAYBOUNDS
                    graphics.DrawRectangle(Pens.Red, glyphTarget.X,
                        glyphTarget.Y, glyphTarget.Width, glyphTarget.Height);
                    graphics.DrawRectangle(Pens.Blue, glyphBounds.X,
                        glyphBounds.Y, glyphBounds.Width, glyphBounds.Height);
                    graphics.DrawLine(Pens.Yellow, glyphTarget.X,
                        glyphTarget.Y, 
                        glyphTarget.X + (glyphBounds.Left - glyphTarget.Left),
                        glyphTarget.Y);
#endif

                    //Calculate the relative clipping rectangle.
                    //All pixel values need to be converted to a relative
                    //value between 0.0 and 1.0, while the Y position must
                    //be inverted (in texcoords, 0/0 is at the bottom left).
                    Vector2 clippingPosition = new Vector2(
                        (currentX) / canvasSize.Width,
                        (canvasSize.Height - currentY - fontHeightAbs)
                        / canvasSize.Height);
                    Vector2 clippingSize = new Vector2(
                        (glyph.Value.Width) / canvasSize.Width,
                        (fontHeightAbs) / canvasSize.Height);

                    //Create and store the mapping to the drawn glyph.
                    mappings.Add(glyph.Key, new GlyphMapping(clippingPosition, 
                        clippingSize, bearingLeft, glyphWidth));

                    //Update the current X and Y position.
                    currentCell++;
                    currentX += glyph.Value.Width;
                    if (currentCell % cellsPerRow == 0)
                    {
                        currentX = 0;
                        currentY += fontHeightAbs;
                    }
                }

                graphics.Flush();
            }

#if DEBUG_DISPLAYBOUNDS
            try { charBitmap.Save(SpriteFontOutputPath); } catch { }
#endif

            TextureData spriteFontTexture =
                new BitmapTextureData(charBitmap, false);

            return new SpriteFontData(spriteFontTexture, mappings, font.Size);
        }

        private static System.Drawing.Size GetTextureMapSize(
            IList<char> charSet, Font font, int cellsPerRow, 
            out Dictionary<char, SizeF> glyphSizes)
        {
            if (charSet == null)
                throw new ArgumentNullException(nameof(charSet));
            if (font == null)
                throw new ArgumentNullException(nameof(font));
            if (font.Unit != GraphicsUnit.Pixel)
                throw new ArgumentException("The specified font unit is " +
                    "not supported.");

            glyphSizes = new Dictionary<char, SizeF>();

            float fontHeightAbs = font.GetHeight();

            float maximumWidth = 0, currentWidth = 0,
                currentHeight = fontHeightAbs;
            int currentColumn = 0, currentRow = 0;

            using (var bitmap = new Bitmap(1, 1))
            {
                using (System.Drawing.Graphics graphics = 
                    System.Drawing.Graphics.FromImage(bitmap))
                {
                    foreach (char character in charSet)
                    {
                        SizeF charSize = graphics.MeasureString(
                            character.ToString(), font);
                        charSize = new SizeF(charSize.Width, fontHeightAbs);
                        glyphSizes.Add(character, charSize);

                        if (currentColumn >= cellsPerRow)
                        {
                            maximumWidth = Math.Max(maximumWidth,
                                currentWidth);
                            currentHeight += fontHeightAbs;

                            currentColumn = 0;
                            currentWidth = 0;
                            currentRow++;
                        }
                        currentWidth += charSize.Width;
                        currentColumn++;
                    }
                }
            }

            return new System.Drawing.Size((int)Math.Max(1, 
                Math.Ceiling(maximumWidth)), 
                (int)Math.Max(1, Math.Ceiling(currentHeight)));
        }

        public void Export(object resource, ResourceManager manager, 
            ResourcePath path, bool overwrite)
        {
            throw new NotSupportedException("Exporting fonts is " +
                "not supported.");
        }
    }
}
