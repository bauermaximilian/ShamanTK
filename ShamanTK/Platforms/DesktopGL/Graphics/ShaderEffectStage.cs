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

using System;
using System.Numerics;

namespace ShamanTK.Platforms.DesktopGL.Graphics
{
    class ShaderEffectStage : Shader
    {
        #region GLSL shader source code
        private const string VertexShaderCode =
            VertexShaderVersionPrefix +
@"in vec3 position;
in vec3 normal;
in vec2 textureCoordinate;
in vec4 propertySegment1;
in vec4 propertySegment2;

out vec2 vertexTextureCoordinate;

uniform mat4 model;
uniform mat4 projection;
uniform bool flip_texture_y = false;

void main()
{
    float flipFactor = 1;
    if (flip_texture_y) flipFactor = -1;
    vertexTextureCoordinate = vec2(textureCoordinate.x, 
        textureCoordinate.y * flipFactor);
    gl_Position = (projection * model) * vec4(position, 1.0f);
}
";

        private const string FragmentShaderCode =
            FragmentShaderVersionPrefix +
@"in vec2 vertexTextureCoordinate;

uniform sampler2D texture_color;
uniform vec3 color_resolution = vec3(255, 255, 255);

void main()
{
    vec4 color = texture(texture_color, vertexTextureCoordinate);
    vec3 color_reduced = floor(color.rgb * (color_resolution - 1.0) + 0.5) / 
        (color_resolution - 1.0);
    color = vec4(min(color_reduced, 1.0), color.a);
    gl_FragColor = color;
}
";
        #endregion

        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// model transformation matrix.
        /// </summary>
        public Uniform<Matrix4x4> Model { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the texture which 
        /// should be drawn to the render target with the effects applied.
        /// </summary>
        public Uniform<ShamanTK.Graphics.TextureBuffer> Texture { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the flag which defines
        /// whether the Y axis of the texture coordinate should be inverted
        /// or not.
        /// </summary>
        public Uniform<bool> FlipTextureY { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the color resolution
        /// vector, which defines how many shades of red 
        /// (<see cref="Vector3.X"/>), green (<see cref="Vector3.Y"/>) and
        /// blue (<see cref="Vector3.Z"/>) each pixel may have.
        /// </summary>
        public Uniform<Vector3> ColorResolution { get; }

        private ShaderEffectStage(int programHandle) : base(programHandle)
        {
            Model = new UniformMatrix4x4(programHandle, "model");
            Texture = new UniformTexture(programHandle, "texture_color", 0);
            ColorResolution = new UniformVector3(programHandle, 
                "color_resolution", false);
            FlipTextureY = new UniformBool(programHandle, "flip_texture_y");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderRenderStage"/>
        /// class.
        /// </summary>
        /// <returns>
        /// A new instance of the <see cref="ShaderEffectStage"/> class.
        /// </returns>
        /// <exception cref="ApplicationException">
        /// Is thrown when the shader creation failed due to version 
        /// incompatibility issues or errors in the shader code.
        /// </exception>
        public static ShaderEffectStage Create()
        {
            int programHandle;
            try
            {
                programHandle = CreateShaderProgram(
                    VertexShaderCode, FragmentShaderCode);
            }
            catch (Exception exc)
            {
                throw new ApplicationException("The shader program " +
                    "couldn't be created.", exc);
            }
            return new ShaderEffectStage(programHandle);
        }
    }
}
