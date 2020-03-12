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

using System;
using System.Collections.Generic;
using System.Numerics;
using Eterra.Common;
using OpenTK.Graphics.OpenGL;

namespace Eterra.Platforms.Windows.Graphics
{
    abstract class Shader
    {
        #region Uniform and Attribute class definitions
        /// <summary>
        /// Provides an accessor to a uniform values in a shader, which can be 
        /// used to upload uniform values to the GPU.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the uniform value in the C# context.
        /// </typeparam>
        public abstract class Uniform<T>
        {
            /// <summary>
            /// Gets the current value of the uniform.
            /// </summary>
            public abstract T CurrentValue { get; }

            /// <summary>
            /// Gets a value which indicates whether the current uniform 
            /// exists in the shader (<c>true</c>) or if the uniform does not exist
            /// and value assignments will have no effect (<c>false</c>).
            /// </summary>
            public abstract bool IsAccessible { get; }

            /// <summary>
            /// Sets the value of the parameter on the shader.
            /// </summary>
            /// <param name="value">
            /// The new parameter value.
            /// </param>
            /// <returns>
            /// <c>true</c> when the uniform was available and the value was
            /// assigned, <c>false</c> otherwise.
            /// </returns>
            /// <exception cref="ArgumentNullException">
            /// Is thrown when <see cref="T"/> is a class type and 
            /// <paramref name="value"/> is null.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// Is thrown when the specified value was invalid or had an 
            /// invalid size.
            /// </exception>
            protected abstract bool OnSet(in T value);

            /// <summary>
            /// Sets the value of the parameter on the shader.
            /// </summary>
            /// <param name="value">
            /// The new parameter value.
            /// </param>
            /// <returns>
            /// <c>true</c> when the uniform was available and the value was
            /// assigned, <c>false</c> otherwise.
            /// </returns>
            /// <exception cref="ArgumentNullException">
            /// Is thrown when <see cref="T"/> is a class type and 
            /// <paramref name="value"/> is null.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// Is thrown when the specified value was invalid or had an 
            /// invalid size.
            /// </exception>
            public bool Set(in T value)
            {
                return OnSet(value);
            }
        }

        /// <summary>
        /// Provides an accessor to a attributes in a shader, which contain 
        /// data for each vertex.
        /// </summary>
        public class Attribute
        {
            /// <summary>
            /// Gets a value which indicates whether the current attribute 
            /// exists in the shader (<c>true</c>) or if the attribute does not exist
            /// and value associations will Fail (<c>false</c>).
            /// </summary>
            public bool IsAccessible { get => Location >= 0; }

            /// <summary>
            /// Gets the location of the attribute.
            /// </summary>
            public int Location { get; private set; }

            /// <summary>
            /// Gets the type of the attribute 
            /// as <see cref="VertexAttribPointerType"/>.
            /// </summary>
            public VertexAttribPointerType Type { get; private set; }

            /// <summary>
            /// Gets the size of an single component of the attribute value 
            /// in bytes.
            /// </summary>
            public int ComponentSize { get; private set; }

            /// <summary>
            /// Gets the amount of components the attribute has.
            /// For an array, this would be the amount of array elements, for
            /// a vector this would be the amount of vector components (e.g.
            /// 4 for a four-dimensional vector).
            /// </summary>
            public int ComponentCount { get; private set; }

            /// <summary>
            /// Gets the size of a complete attribute value.
            /// </summary>
            public int Size { get => ComponentCount * ComponentSize; }

            private readonly string attributeIdentifier;

            /// <summary>
            /// Creates a new instance of the <see cref="Attribute"/> class.
            /// </summary>
            /// <param name="programHandle">
            /// The handle of the target shader program.
            /// </param>
            /// <param name="attributeIdentifier">
            /// The identifier of the attribute.
            /// </param>
            /// <param name="elementCount">
            /// The amount of elements this attribute has. For an array, this would
            /// be the amount of array elements, for a vector this is the amount of
            /// vector components.
            /// </param>
            /// <returns>
            /// A new instance of the <see cref="Attribute"/> class.
            /// </returns>
            /// <exception cref="ArgumentNullException">
            /// Is thrown when <paramref name="attributeIdentifier"/> is null.
            /// </exception>
            /// <exception cref="ArgumentOutOfRangeException">
            /// Is thrown when <paramref name="elementCount"/> is equal or less 
            /// than zero.
            /// </exception>
            /// <exception cref="NotSupportedException">
            /// Is thrown when <paramref name="attributeType"/> is one of the 
            /// following elements:
            /// <see cref="VertexAttribPointerType.Fixed"/>,
            /// <see cref="VertexAttribPointerType.HalfFloat"/>,
            /// <see cref="VertexAttribPointerType.Int2101010Rev"/>.
            /// </exception>
            public Attribute(int programHandle, string attributeIdentifier,
                int elementCount, VertexAttribPointerType attributeType)
            {
                this.attributeIdentifier = attributeIdentifier
                    ?? throw new ArgumentNullException(
                        nameof(attributeIdentifier));

                if (elementCount <= 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(elementCount));

                Location = GL.GetAttribLocation(programHandle,
                    attributeIdentifier);

                Type = attributeType;
                ComponentSize = GetAttributeSize(Type);
                ComponentCount = elementCount;
            }

            private int GetAttributeSize(VertexAttribPointerType type)
            {
                return type switch
                {
                    VertexAttribPointerType.Byte => sizeof(byte),
                    VertexAttribPointerType.Double => sizeof(double),
                    VertexAttribPointerType.Float => sizeof(float),
                    VertexAttribPointerType.Int => sizeof(int),
                    VertexAttribPointerType.Short => sizeof(short),
                    VertexAttribPointerType.UnsignedByte => sizeof(byte),
                    VertexAttribPointerType.UnsignedInt => sizeof(uint),
                    VertexAttribPointerType.UnsignedInt2101010Rev => 
                    sizeof(uint),
                    VertexAttribPointerType.UnsignedShort => sizeof(ushort),
                    _ => throw new NotSupportedException("The specified " +
                    "attribute type was not supported."),
                };
            }

            /// <summary>
            /// Executes the command <see cref="GL.VertexAttribPointer(int, int, VertexAttribPointerType, bool, int, int)"/> with the parameters
            /// and <see cref="GL.EnableVertexAttribArray(int)"/> for the 
            /// current <see cref="Attribute"/> instance. 
            /// </summary>
            /// <param name="strideBytes">
            /// The value for the stride parameter in the original GL command.
            /// </param>
            /// <param name="offsetBytes">
            /// The value for the offset parameter in the original GL command.
            /// </param>
            public void InitializeVertexAttribPointer(int strideBytes,
                int offsetBytes)
            {
                if (strideBytes < 0)
                    throw new ArgumentOutOfRangeException("strideBytes");
                if (offsetBytes < 0)
                    throw new ArgumentOutOfRangeException("offsetBytes");

                GL.VertexAttribPointer(Location, ComponentCount,
                        Type, false, strideBytes,
                        offsetBytes);
                GL.EnableVertexAttribArray(Location);
            }

            public override string ToString()
            {
                return Enum.GetName(typeof(VertexAttribPointerType), Type)
                    + " \"" + attributeIdentifier + "\" (@" + Location + ", "
                    + Size + " bytes)";
            }
        }
#endregion

        #region Type-specific uniform implementations
        protected abstract class UniformSingleValue<ValueT>
            : Uniform<ValueT>
        {
            public override bool IsAccessible { get => Location >= 0; }

            public override ValueT CurrentValue => currentValue;
            private ValueT currentValue;

            /// <summary>
            /// The location of the uniform.
            /// </summary>
            protected readonly int Location;

            /// <summary>
            /// The identifier of the uniform.
            /// </summary>
            protected readonly string UniformIdentifier;

            protected UniformSingleValue(int programHandle,
                string uniformIdentifier)
            {
                UniformIdentifier = uniformIdentifier ??
                    throw new ArgumentNullException(nameof(uniformIdentifier));

                if (programHandle >= 0)
                    Location = GL.GetUniformLocation(programHandle,
                        UniformIdentifier);
            }

            protected override bool OnSet(in ValueT value)
            {
                currentValue = value;
                return true;
            }
        }

        protected class UniformMatrix4x4 : UniformSingleValue<Matrix4x4>
        {
            private readonly bool transpose;

            public UniformMatrix4x4(int programHandle,
                string identifier, bool transpose = false)
                : base(programHandle, identifier)
            {
                this.transpose = transpose;
            }

            protected override bool OnSet(in Matrix4x4 value)
            {
                if (!IsAccessible) return false;

                global::OpenTK.Matrix4 originalMatrix =
                    new global::OpenTK.Matrix4(
                        value.M11, value.M12, value.M13, value.M14,
                        value.M21, value.M22, value.M23, value.M24,
                        value.M31, value.M32, value.M33, value.M34,
                        value.M41, value.M42, value.M43, value.M44);

                GL.UniformMatrix4(Location, transpose, ref originalMatrix);
                return base.OnSet(value);
            }
        }

        protected class UniformMatrix4 : 
            UniformSingleValue<global::OpenTK.Matrix4>
        {
            public UniformMatrix4(int programHandle,
                string identifier) : base(programHandle, identifier) { }

            protected override bool OnSet(in global::OpenTK.Matrix4 value)
            {
                if (!IsAccessible) return false;
                global::OpenTK.Matrix4 matrix = value;
                GL.UniformMatrix4(Location, false, ref matrix);
                return base.OnSet(value);
            }
        }

        protected class UniformVector2 : UniformSingleValue<Vector2>
        {
            public UniformVector2(int programHandle,
                string identifier) : base(programHandle, identifier) { }

            protected override bool OnSet(in Vector2 value)
            {
                if (!IsAccessible) return false;
                GL.Uniform2(Location, value.X, value.Y);
                return base.OnSet(value);
            }
        }

        protected class UniformVector3 : UniformSingleValue<Vector3>
        {
            private readonly bool invertZ;

            public UniformVector3(int programHandle,
                string identifier, bool invertZ)
                : base(programHandle, identifier)
            {
                this.invertZ = invertZ;
            }

            protected override bool OnSet(in Vector3 value)
            {
                if (!IsAccessible) return false;
                GL.Uniform3(Location, value.X, value.Y,
                    (invertZ ? -1 : 1) * value.Z);
                return base.OnSet(value);
            }
        }

        protected class UniformQuaternion : UniformSingleValue<Quaternion>
        {
            public UniformQuaternion(int programHandle,
                string identifier) : base(programHandle, identifier) { }

            protected override bool OnSet(in Quaternion value)
            {
                if (!IsAccessible) return false;
                GL.Uniform4(Location, value.X, value.Y, value.Z, value.W);
                return base.OnSet(value);
            }
        }

        protected class UniformBool : UniformSingleValue<bool>
        {
            public UniformBool(int programHandle,
                string uniformIdentifier)
                : base(programHandle, uniformIdentifier) { }

            protected override bool OnSet(in bool value)
            {
                if (!IsAccessible) return false;
                GL.Uniform1(Location, (value ? 1 : 0));
                return base.OnSet(value);
            }
        }

        protected class UniformInt : UniformSingleValue<int>
        {
            public UniformInt(int programHandle,
                string uniformIdentifier)
                : base(programHandle, uniformIdentifier) { }

            protected override bool OnSet(in int value)
            {
                if (!IsAccessible) return false;
                GL.Uniform1(Location, value);
                return base.OnSet(value);
            }
        }

        protected class UniformFloat : UniformSingleValue<float>
        {
            public UniformFloat(int programHandle,
                string uniformIdentifier)
                : base(programHandle, uniformIdentifier) { }

            protected override bool OnSet(in float value)
            {
                if (!IsAccessible) return false;
                GL.Uniform1(Location, value);
                return base.OnSet(value);
            }
        }

        protected class UniformColorRGB : Uniform<Color>
        {
            private readonly UniformVector3 baseUniform;

            public override Color CurrentValue => color;
            private Color color;

            public override bool IsAccessible => baseUniform.IsAccessible;

            public UniformColorRGB(int programHandle,
                string identifier)
            {
                baseUniform = new UniformVector3(programHandle,
                    identifier, false);
            }

            protected override bool OnSet(in Color value)
            {
                if (!IsAccessible) return false;
                baseUniform.Set(new Vector3(
                    (float)value.R / byte.MaxValue,
                    (float)value.G / byte.MaxValue,
                    (float)value.B / byte.MaxValue));
                color = value;
                return true;
            }
        }

        protected class UniformColorRGBA : Uniform<Color>
        {
            private readonly UniformQuaternion baseUniform;

            public override Color CurrentValue => color;
            private Color color;

            public override bool IsAccessible => baseUniform.IsAccessible;

            public UniformColorRGBA(int programHandle,
                string identifier)
            {
                baseUniform = new UniformQuaternion(programHandle,
                    identifier);
            }

            protected override bool OnSet(in Color value)
            {
                if (!IsAccessible) return false;
                baseUniform.Set(new Quaternion(
                    (float)value.R / byte.MaxValue,
                    (float)value.G / byte.MaxValue,
                    (float)value.B / byte.MaxValue,
                    (float)value.Alpha / byte.MaxValue));
                color = value;
                return true;
            }
        }

        protected class UniformTexture
            : UniformSingleValue<Eterra.Graphics.TextureBuffer>
        {
            private readonly static TextureUnit[] textureUnits;

            static UniformTexture()
            {
                List<TextureUnit> textureUnitList = new List<TextureUnit>();

                foreach (TextureUnit unit
                    in Enum.GetValues(typeof(TextureUnit)))
                    textureUnitList.Add(unit);

                textureUnits = textureUnitList.ToArray();
            }

            private readonly int slot;
            private readonly UniformBool assignedUniform;

            public UniformTexture(int programHandle, string identifier,
                int textureSlot)
                : base(programHandle, identifier)
            {
                assignedUniform = new UniformBool(programHandle,
                    identifier + "_assigned");

                if (slot < 0 || slot > textureUnits.Length)
                    throw new ArgumentOutOfRangeException(nameof(slot));

                slot = textureSlot;
            }

            protected override bool OnSet(
                in Eterra.Graphics.TextureBuffer value)
            {
                if (!IsAccessible)
                {
                    if (value == null) base.OnSet(null);
                    else return false;
                }

                int targetHandle;

                if (value is TextureBuffer bufferValue)
                    targetHandle = !bufferValue.IsDisposed ?
                        bufferValue.Handle : 0;
                else if (value is RenderTextureBuffer renderBufferValue)
                    targetHandle = !renderBufferValue.IsDisposed ?
                        renderBufferValue.TextureHandle : 0;
                else if (value == null) targetHandle = 0;
                else throw new ArgumentException("The specified buffer " +
                    "was no valid texture buffer in the current context.");

                GL.ActiveTexture(textureUnits[slot]);
                GL.BindTexture(TextureTarget.Texture2D, targetHandle);
                GL.Uniform1(Location, slot);

                if (assignedUniform.IsAccessible)
                    assignedUniform.Set(targetHandle > 0);

                return base.OnSet(value);
            }
        }
#endregion

        /// <summary>
        /// Gets the handle of the shader program.
        /// </summary>
        public int Handle { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// projection transformation matrix.
        /// </summary>
        public Uniform<global::OpenTK.Matrix4> Projection { get; }

        /// <summary>
        /// Gets the vertex attribute of the shader, which defines the 
        /// position of a vertex.
        /// </summary>
        public Attribute VertexPosition { get; }

        /// <summary>
        /// Gets the vertex attribute of the shader, which defines the
        /// normal of a vertex.
        /// </summary>
        public Attribute VertexNormal { get; }

        /// <summary>
        /// Gets the vertex attribute of the shader, which defines the
        /// texture coordinate of a vertex.
        /// </summary>
        public Attribute VertexTextureCoordinate { get; }

        /// <summary>
        /// Gets the vertex attribute of the shader, which defines the
        /// IDs of the bones the vertex is attached to.
        /// </summary>
        public Attribute VertexBoneMappingIds { get; }

        /// <summary>
        /// Gets the vertex attribute of the shader, which defines the
        /// weights of the bone attachments the vertex is attached to.
        /// </summary>
        public Attribute VertexBoneMappingWeights { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Shader"/> 
        /// base class.
        /// </summary>
        /// <param name="programHandle">
        /// The handle to the shader program. This handle can be generated
        /// with <see cref="CreateShaderProgram(string, string)"/>.
        /// </param>
        protected Shader(int programHandle)
        {
            Handle = programHandle;
            Projection = new UniformMatrix4(programHandle,
                "projection");

            VertexPosition = new Attribute(programHandle,
                "position", 3, VertexAttribPointerType.Float);
            VertexNormal = new Attribute(programHandle,
                "normal", 3, VertexAttribPointerType.Float);
            VertexTextureCoordinate = new Attribute(programHandle,
                "textureCoordinate", 2, VertexAttribPointerType.Float);
            VertexBoneMappingIds = new Attribute(programHandle,
                "boneIds", 4, VertexAttribPointerType.UnsignedByte);
            VertexBoneMappingWeights = new Attribute(programHandle,
                "boneWeights", 4, VertexAttribPointerType.UnsignedByte);
        }

        /// <summary>
        /// Makes the shader the currently used shader in the GL context
        /// and initializes the uniform values.
        /// </summary>
        public void Use()
        {
            GL.UseProgram(Handle);
        }

        /// <summary>
        /// Generates and enables the vertex attribute pointers for all
        /// attributes in the current shader. The required format of the
        /// vertex stream is PPPNNNTTIIIIWWWW, where 'P' is a component of
        /// the vertex position vector, the 'N' stands for the normal
        /// vector, the 'T' for the texture coordinate, the 'I' for the
        /// bone index, the 'W' for the bone weight.
        /// </summary>
        public virtual void InitializeVertexAttribPointers()
        {
            int stride = VertexPosition.Size + VertexNormal.Size
                + VertexTextureCoordinate.Size + VertexBoneMappingIds.Size
                + VertexBoneMappingWeights.Size;

            int offset = 0;

            VertexPosition.InitializeVertexAttribPointer(stride, offset);
            offset += VertexPosition.Size;

            VertexNormal.InitializeVertexAttribPointer(stride, offset);
            offset += VertexNormal.Size;

            VertexTextureCoordinate.InitializeVertexAttribPointer(stride,
                offset);
            offset += VertexTextureCoordinate.Size;

            VertexBoneMappingIds.InitializeVertexAttribPointer(stride,
                offset);
            offset += VertexBoneMappingIds.Size;

            VertexBoneMappingWeights.InitializeVertexAttribPointer(stride,
                offset);
            //offset += VertexBoneMappingWeights.Size;
        }

        /// <summary>
        /// Creates a new OpenGL shader program.
        /// </summary>
        /// <param name="vertexShaderCode">
        /// The code of the vertex shader.
        /// </param>
        /// <param name="fragmentShaderCode">
        /// The code of the fragment shader.
        /// </param>
        /// <returns>The handle of the ready-to-use shader program.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="vertexShaderCode"/> or
        /// <paramref name="fragmentShaderCode"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the code from either 
        /// <paramref name="vertexShaderCode"/> or 
        /// <paramref name="fragmentShaderCode"/> couldn't be compiled, or
        /// when an error ocurred while linking the two shaders together to
        /// a shader program.
        /// </exception>
        protected static int CreateShaderProgram(string vertexShaderCode,
            string fragmentShaderCode)
        {
            if (vertexShaderCode == null)
                throw new ArgumentNullException(nameof(vertexShaderCode));
            if (fragmentShaderCode == null)
                throw new ArgumentNullException(nameof(fragmentShaderCode));

            //Generate new shader program.
            int handle = GL.CreateProgram();

            //Try compiling both shaders.
            int vertexShaderHandle, fragmentShaderHandle;
            try
            {
                vertexShaderHandle = CompileShader(vertexShaderCode,
                    ShaderType.VertexShader);
                GL.AttachShader(handle, vertexShaderHandle);
            }
            catch (Exception exc)
            {
                throw new ArgumentException("Error while compiling the " +
                    "vertex shader. " + exc.Message);
            }
            try
            {
                fragmentShaderHandle = CompileShader(fragmentShaderCode,
                    ShaderType.FragmentShader);
                GL.AttachShader(handle, fragmentShaderHandle);
            }
            catch (Exception exc)
            {
                GL.DeleteShader(vertexShaderHandle);
                throw new ArgumentException("Error while compiling the " +
                    "fragment shader. " + exc.Message);
            }

            //Try to link both shaders to the shader program and dispose
            //the single shader objects (which are no longer required).
            GL.LinkProgram(handle);

            GL.DeleteShader(vertexShaderHandle);
            GL.DeleteShader(fragmentShaderHandle);

            GL.GetProgram(handle, GetProgramParameterName.LinkStatus,
                out int linkStatus);
            GL.GetProgram(handle, GetProgramParameterName.ValidateStatus,
                out int validateStatus);
            if (linkStatus != 1)
                throw new ArgumentException("Error while linking " +
                    "vertex and fragment shader. "
                    + GL.GetProgramInfoLog(handle));
            if (validateStatus != 1 && validateStatus != 0)
                Log.Warning("The OpenGL shader program validation failed "
                    + "(Code " + validateStatus + "). " + 
                    GL.GetProgramInfoLog(handle));

            return handle;
        }

        private static int CompileShader(string code, ShaderType type)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));

            int handle = GL.CreateShader(type);
            GL.ShaderSource(handle, code);
            GL.CompileShader(handle);
            GL.GetShader(handle, ShaderParameter.CompileStatus,
                out int shaderStatus);
            if (shaderStatus != 1)
                throw new Exception(GL.GetShaderInfoLog(handle).Trim());
            else return handle;
        }
    }
}
