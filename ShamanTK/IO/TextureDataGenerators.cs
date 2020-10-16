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

namespace ShamanTK.IO
{
    /// <summary>
    /// Provides various generated textures.
    /// </summary>
    internal static class TextureDataGenerators
    {
        internal class SolidColor : TextureData
        {
            public Color Color { get; set; }

            public override Pointer PixelData => null;

            public SolidColor(Size size) : base(size) { }

            public override Color[] GetRegion(int x, int y, 
                int width, int height)
            {
                AssertValidTextureSection(x, y, width, height);

                Color[] pixels = new Color[width * height];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color;
                return pixels;
            }

            protected override void Dispose(bool disposing) { }
        }

        internal class Checkerboard : TextureData
        {
            public Color PrimaryColor { get; set; }

            public Color SecondaryColor { get; set; }

            public override Pointer PixelData => null;

            public int PatternSize
            {
                get => patternSize;
                set { patternSize = Math.Max(1, patternSize); }
            }
            private int patternSize = 1;

            public Checkerboard(Size size) : base(size) { }

            public override Color[] GetRegion(int x, int y,
                int width, int height)
            {
                AssertValidTextureSection(x, y, width, height);

                Color[] pixels = new Color[width * height];

                for (int i = 0; i < pixels.Length; i++)
                {
                    bool xChecked = ((x / PatternSize) % 2) == 0;
                    bool yChecked = ((y / PatternSize) % 2) == 0;

                    if (xChecked && yChecked) pixels[i] = PrimaryColor;
                    else pixels[i] = SecondaryColor;
                }
                return pixels;
            }

            protected override void Dispose(bool disposing) { }
        }
    }
}
