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

using Eterra.Common;
using Eterra.IO;
using System;
using System.Drawing.Imaging;

namespace Eterra.Platforms.Windows.IO
{
    public class BitmapTextureData : TextureData
    {
        private System.Drawing.Bitmap bitmap;
        private BitmapData bitmapData;

        public override MemoryPointer PixelDataPointer
        {
            get
            {
                if (colorBgraPointer != null) return colorBgraPointer;
                else return colorBgrPointer;
            }
        }

        private readonly MemoryPointer<Color.BGRA> colorBgraPointer = null;
        private readonly MemoryPointer<Color.BGR> colorBgrPointer = null;
        private readonly int paddingWidth;

        public BitmapTextureData(System.Drawing.Bitmap bitmap,
            bool preferPointer)
            : base(new Size(bitmap != null ? bitmap.Width : 1,
                  bitmap != null ? bitmap.Height : 1))
        {
            this.bitmap = bitmap ??
                throw new ArgumentNullException(nameof(bitmap));
            bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0,
                Size.Width, Size.Height), ImageLockMode.ReadWrite,
                bitmap.PixelFormat);

            if (bitmap.PixelFormat == PixelFormat.Format32bppArgb &&
                preferPointer)
            {
                colorBgraPointer = MemoryPointer<Color.BGRA>.Create(
                    bitmapData.Scan0, Size.Area, true, false);
                paddingWidth = Math.Max(0, Math.Abs(bitmapData.Stride) / 4
                    - Size.Width);
            }
            else if (bitmap.PixelFormat == PixelFormat.Format24bppRgb &&
                preferPointer)
            {
                colorBgrPointer = MemoryPointer<Color.BGR>.Create(
                    bitmapData.Scan0, Size.Area, true, false);
                paddingWidth = Math.Max(0, Math.Abs(bitmapData.Stride) / 3
                    - Size.Width);
            }
            else
            {
                bitmap.UnlockBits(bitmapData);
                bitmapData = null;
            }
        }

        public override Color GetPixel(int tx, int ty)
        {
            ThrowIfDisposed();
            ValidatePixelPosition(tx, ty);

            int pixelIndex = ty * Size.Width + tx;

            if (colorBgraPointer != null)
                return (Color)colorBgraPointer.Read(pixelIndex);
            else if (colorBgrPointer != null)
                return (Color)colorBgrPointer.Read(pixelIndex);
            else
            {
                System.Drawing.Color color = bitmap.GetPixel(tx, ty);
                return new Color(color.R, color.G, color.B, color.A);
            }
        }

        public override int GetPixelIndex(int tx, int ty)
        {
            if (tx < 0 || tx >= Size.Width)
                throw new ArgumentOutOfRangeException(nameof(tx));
            if (ty < 0 || ty >= Size.Height)
                throw new ArgumentOutOfRangeException(nameof(ty));

            return ty * (Size.Width + paddingWidth) + tx;
        }

        public override void GetPixelPosition(int index, out int tx,
            out int ty)
        {
            if (index < 0 || index >= PixelCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            tx = index % (Size.Width + paddingWidth);
            ty = index / (Size.Width + paddingWidth);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && bitmap != null)
            {
                if (bitmapData != null)
                {
                    bitmap.UnlockBits(bitmapData);
                    bitmapData = null;
                }

                bitmap.Dispose();
                bitmap = null;
            }
        }
    }
}
