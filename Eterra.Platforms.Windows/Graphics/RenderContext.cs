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
using Eterra.Graphics;
using System;
using System.Numerics;

namespace Eterra.Platforms.Windows.Graphics
{
    abstract internal class RenderContext : IRenderContext
    {
        public bool IsDisposed { get; private set; }

        public Eterra.Graphics.MeshBuffer Mesh
        {
            get => IsDisposed ? null : mesh;
            set => mesh = value;
        }
        private Eterra.Graphics.MeshBuffer mesh;

        public Matrix4x4 Location
        {
            get => IsDisposed ? Matrix4x4.Identity : 
                Context.Shader.Model.CurrentValue;
            set
            {
                if (!IsDisposed)
                {
                    Context.Shader.Model.Set(value);
                    if (!Matrix4x4.Invert(value, out Matrix4x4 invertedValue))
                        invertedValue = value;
                    Context.Shader.ModelTransposedInversed.Set(
                        Matrix4x4.Transpose(invertedValue));
                }
            }
        }

        public Deformer Deformation
        {
            get => IsDisposed ? null : Context.Shader.Deformers.CurrentValue;
            set
            {
                if (!IsDisposed)
                    Context.Shader.Deformers.Set(value ?? Deformer.Empty);
            }
        }

        public Color Color
        {
            get => IsDisposed ? 
                Color.Black : Context.Shader.Color.CurrentValue;
            set { if (!IsDisposed) Context.Shader.Color.Set(value); }
        }

        public abstract Eterra.Graphics.TextureBuffer Texture { get; set; }

        public abstract Rectangle TextureClipping { get; set; }

        public abstract MixingMode TextureMixingMode { get; set; }

        public float Opacity
        {
            get => IsDisposed ? 0 : Context.Shader.Opacity.CurrentValue;
            set { if (!IsDisposed) Context.Shader.Opacity.Set(value); }
        }

        protected IGraphicsContext Context { get; }

        protected RenderContext(IGraphicsContext context, ShadingMode shadingMode)
        {
            Context = context ??
                throw new ArgumentNullException(nameof(context));

            if (Enum.IsDefined(typeof(ShadingMode), shadingMode))
                context.Shader.ShadingMode.Set((int)shadingMode);
            else throw new ArgumentException("The specified shading mode " +
                "is invalid.");

            Mesh = null;
            Location = Matrix4x4.Identity;
            Deformation = null;
            Color = Color.Black;
            Opacity = 1;
        }

        public virtual void Dispose()
        {
            IsDisposed = true;
        }

        public virtual bool Draw()
        {
            if (mesh is MeshBuffer meshBuffer && !IsDisposed)
            {
                Context.DrawMeshBuffer(meshBuffer);
                return true;
            }
            return false;
        }
    }
}
