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
using ShamanTK.Graphics;
using System;

namespace ShamanTK.Platforms.DesktopGL.Graphics
{
    internal class RenderContextPhong : RenderContext, IRenderContextPhong
    {
        public override ShamanTK.Graphics.TextureBuffer Texture
        {
            get => IsDisposed ? null : Context.Shader.TextureMain.CurrentValue;
            set
            {
                if (!IsDisposed) Context.Shader.TextureMain.Set(value);
            }
        }

        public ShamanTK.Graphics.TextureBuffer SpecularMap
        {
            get => IsDisposed ? 
                null : Context.Shader.TextureEffect01.CurrentValue;
            set
            {
                if (!IsDisposed) Context.Shader.TextureEffect01.Set(value);
            }
        }

        public ShamanTK.Graphics.TextureBuffer NormalMap
        {
            get => IsDisposed ?
                null : Context.Shader.TextureEffect02.CurrentValue;
            set
            {
                if (!IsDisposed) Context.Shader.TextureEffect02.Set(value);
            }
        }

        public ShamanTK.Graphics.TextureBuffer EmissionMap
        {
            get => IsDisposed ?
                null : Context.Shader.TextureEffect03.CurrentValue;
            set
            {
                if (!IsDisposed) Context.Shader.TextureEffect03.Set(value);
            }
        }

        public RenderContextPhong(IGraphicsContext context) : base(context)
        {
            Texture = null;
            SpecularMap = null;
            NormalMap = null;
            EmissionMap = null;
        }
    }
}
