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
        /*internal class TextureMixer : Texture
        {
            public TextureMixer(int width, int height) : base(width, height)
            {
            }

            public override Color GetPixel(int x, int y)
            {
                throw new NotImplementedException();
            }
        }*/

        internal class SolidColor : TextureData
        {
            public Color Color { get; set; }

            public SolidColor(Size size) : base(size) { }

            public override Color GetPixel(int x, int y)
            {
                return Color;
            }

            public override Color GetPixel(int index)
            {
                return Color;
            }

            public override Color[] GetPixels(int tx, int ty, 
                int width, int height)
            {
                ValidateTextureSection(tx, ty, width, height);

                Color[] pixels = new Color[width * height];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color;
                return pixels;
            }

            public override Color[] GetPixels(int index, int count, 
                bool throwOnCountOverflow)
            {
                ValidateIndexParams(index, ref count, throwOnCountOverflow);

                Color[] pixels = new Color[count];

                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color;

                return pixels;
            }

            protected override void Dispose(bool disposing) { }
        }

        /*
        private class GradientTextureGenerator : Texture
        {
            //Support more than just 2 colors and positions

            public GradientTextureGenerator(int width, int height)
                : base(width, height)
            {
            }

            public override Color GetPixel(int x, int y)
            {
                throw new NotImplementedException();
            }
        }*/
        /*
        private class PerlinNoiseGenerator : Texture
        {
            //http://flafla2.github.io/2014/08/09/perlinnoise.html
        }*/

        internal class Checkerboard : TextureData
        {
            public Color PrimaryColor { get; set; }

            public Color SecondaryColor { get; set; }

            public int PatternSize
            {
                get => patternSize;
                set { patternSize = Math.Max(1, patternSize); }
            }
            private int patternSize = 1;

            public Checkerboard(Size size) : base(size) { }

            public override Color GetPixel(int x, int y)
            {
                bool xChecked = ((x / PatternSize) % 2) == 0;
                bool yChecked = ((y / PatternSize) % 2) == 0;

                if (xChecked && yChecked) return PrimaryColor;
                else return SecondaryColor;
            }

            protected override void Dispose(bool disposing) { }
        }
    }
}
