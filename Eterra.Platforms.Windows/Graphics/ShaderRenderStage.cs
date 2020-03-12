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
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Eterra.Platforms.Windows.Graphics
{
    class ShaderRenderStage : Shader
    {
        /// <summary>
        /// Defines the maximum amount of lights (excluding the ambient light)
        /// which are supported by the shader.
        /// </summary>
        public const int LightsLimit = 8;

        #region Shader-specific uniform class definitions
        /// <remarks>
        /// The uniform must be of a type which implements the 
        /// following struct layout:
        /// <c>
        /// struct Rectangle {
        ///     vec2 position;
        ///     vec2 scale;
        /// };
        /// </c>
        /// </remarks>
        protected class UniformRectangle : Uniform<Rectangle>
        {
            public override bool IsAccessible
            {
                get
                {
                    return positionLocation > -1 ||
                        scaleLocation > -1;
                }
            }

            public override Rectangle CurrentValue => currentValue;
            private Rectangle currentValue;

            private readonly int positionLocation;
            private readonly int scaleLocation;

            public UniformRectangle(int programHandle,
                string identifier)
            {
                if (identifier == null)
                    throw new ArgumentNullException(nameof(identifier));

                positionLocation = GL.GetUniformLocation(programHandle,
                    identifier + ".position");
                scaleLocation = GL.GetUniformLocation(programHandle,
                    identifier + ".scale");
            }

            protected override bool OnSet(in Rectangle value)
            {
                if (!IsAccessible) return false;

                GL.Uniform2(positionLocation, value.X, value.Y);
                GL.Uniform2(scaleLocation, value.Width, value.Height);

                currentValue = value;
                return true;
            }
        }

        protected class UniformLight : Uniform<Light>
        {
            public override Light CurrentValue => currentValue;
            private Light currentValue;

            private readonly UniformInt type;
            private readonly UniformColorRGB color;
            private readonly UniformVector3 position, direction;
            private readonly UniformFloat cutoff, cutoffWidth, radius;
            //private readonly UniformBool castShadows;

            public override bool IsAccessible => type.IsAccessible;

            public UniformLight(int programHandle, string identifier)
            {
                if (identifier == null)
                    throw new ArgumentNullException(nameof(identifier));

                type = new UniformInt(programHandle,
                    identifier + ".type");
                color = new UniformColorRGB(programHandle,
                    identifier + ".color");
                position = new UniformVector3(programHandle,
                    identifier + ".position", false);
                direction = new UniformVector3(programHandle,
                    identifier + ".direction", false);
                cutoff = new UniformFloat(programHandle,
                    identifier + ".cutoff");
                cutoffWidth = new UniformFloat(programHandle,
                    identifier + ".cutoffWidth");
                radius = new UniformFloat(programHandle,
                    identifier + ".radius");
                /*castShadows = new UniformBool(programHandle,
                    identifier + ".castShadows");*/
            }

            protected override bool OnSet(in Light value)
            {
                bool allSet = true;

                if (value.Type == LightType.Ambient)
                {
                    allSet &= color.Set(value.Color);
                    allSet &= direction.Set(value.Direction);
                    //allSet &= castShadows.Set(value.CastShadows);
                }
                else if (value.Type == LightType.Point)
                {
                    allSet &= color.Set(value.Color);
                    allSet &= position.Set(value.Position);
                    allSet &= radius.Set(value.Radius);
                    //allSet &= castShadows.Set(value.CastShadows);
                }
                else if (value.Type == LightType.Spot)
                {
                    allSet &= color.Set(value.Color);
                    allSet &= position.Set(value.Position);
                    allSet &= direction.Set(value.Direction);
                    allSet &= cutoff.Set((float)Math.Cos(
                        value.Cutoff.Radians));
                    allSet &= cutoffWidth.Set((float)Math.Sin(
                        value.CutoffWidth.Radians));
                    allSet &= radius.Set(value.Radius);
                    //allSet &= castShadows.Set(value.CastShadows);
                }
                else if (value.Type != LightType.Disabled)
                    throw new ArgumentException("The specified light type " +
                        "is not supported.");

                //Set the light type at last to prevent setting the light type
                //when the light parameters are invalid.
                allSet &= type.Set((int)value.Type);

                if (allSet) currentValue = value;

                return allSet;
            }
        }

        /// <remarks>
        /// Warning - the following documentation is outdated and needs to be
        /// revised to be completely valid.
        /// The accessed uniform must be of a type which implements the 
        /// following struct layout:
        /// <c>
        /// const int MAX_LIGHTS = 8;
        /// 
        /// const int LIGHTTYPE_AMBIENT = 0;
        /// const int LIGHTTYPE_POINT = 1;
        /// const int LIGHTTYPE_SPOT = 2;
        /// 
        /// struct Light
        /// {
        ///     int type;//Required by all light types
        ///     vec3 color;//Required by LIGHTTYPE_AMBIENT, LIGHTTYPE_POINT, LIGHTTYPE_SPOT
        ///     vec3 position;//Required by LIGHTTYPE_POINT and LIGHTTYPE_SPOT
        ///     vec3 direction;//Required by LIGHTTYPE_AMBIENT and LIGHTTYPE_SPOT
        ///     float cutoff;//Required by LIGHTTYPE_SPOT
        ///     float radius;//Required by LIGHTTYPE_POINT
        ///     bool castShadows;//Required by all light types
        /// };
        /// </c>
        /// The value for "MAX_LIGHTS" has to be defined as required.
        /// The values for the constants must be the same as the constant 
        /// values <see cref="Light.TypeAmbient"/>, 
        /// <see cref="Light.TypeSpot"/> and <see cref="Light.TypePoint"/>.
        /// </remarks>
        protected class UniformLightSlot : Uniform<LightSlot>
        {
            private readonly List<UniformLight> lights
                = new List<UniformLight>();

            public override LightSlot CurrentValue => currentValue;
            private LightSlot currentValue;

            public override bool IsAccessible => lights.Count > 0;

            public UniformLightSlot(int programHandle,
                string identifier, int lightSlotCount)
            {
                if (identifier == null)
                    throw new ArgumentNullException(nameof(identifier));
                if (lightSlotCount < 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(lightSlotCount));

                if (programHandle >= 0)
                {
                    for (int i = 0; i < lightSlotCount; i++)
                    {
                        UniformLight light = new UniformLight(
                            programHandle, identifier + "[" + i + "]");
                        if (light.IsAccessible) lights.Add(light);
                        else break;
                    }
                }
            }

            protected override bool OnSet(in LightSlot value)
            {
                if (!IsAccessible) return false;

                if (value.Slot >= lights.Count)
                    throw new ArgumentException("The slot of the specified " +
                        "light was too large and not supported by the " +
                        "shader!");

                currentValue = value;
                return lights[(int)value.Slot].Set(value.Light);
            }
        }

        /// <remarks>
        /// The uniform must be of a type which implements the 
        /// following struct layout:
        /// <c>
        /// const int CAPACITY = 16;
        /// struct List {
        ///     uint size;
        ///     mat4 elements[CAPACITY];
        /// };
        /// </c>
        /// The value for "CAPACITY" has to be defined as required.
        /// </remarks>
        protected class UniformDeformer : Uniform<Deformer>
        {
            private readonly List<UniformMatrix4x4> matrices
                = new List<UniformMatrix4x4>();

            public override Deformer CurrentValue => currentValue;
            private Deformer currentValue;

            /// <summary>
            /// Defines the size of the uploaded array for the shader, so that
            /// not every undefined element has to be uploaded as undefined 
            /// but can left as it is and just be ignored in the shader.
            /// </summary>
            private readonly int assignedElementsLocation;

            public override bool IsAccessible =>
                matrices.Count > 0 && assignedElementsLocation > -1;

            public UniformDeformer(int programHandle, string identifier)
            {
                if (identifier == null)
                    throw new ArgumentNullException(nameof(identifier));

                if (programHandle >= 0)
                {
                    assignedElementsLocation = GL.GetUniformLocation(
                        programHandle, identifier + ".size");
                    for (int i = 0; i < Deformer.MaximumSize; i++)
                    {
                        UniformMatrix4x4 matrix =
                            new UniformMatrix4x4(programHandle,
                            identifier + ".elements[" + i + "]");
                        if (matrix.IsAccessible) matrices.Add(matrix);
                        else break;
                    }
                }
            }

            protected override bool OnSet(in Deformer value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!IsAccessible) return false;

                if (value.Length > matrices.Count)
                    throw new ArgumentException("The specified value list " +
                        "was larger than the uniform list capacity " +
                        "in the shader!");

                GL.Uniform1(assignedElementsLocation, (uint)value.Length);

                currentValue = value;

                bool allSet = true;

                for (byte i = 0; i < value.Length; i++)
                    allSet &= matrices[i].Set(value[i]);

                return allSet;
            }
        }
        #endregion
        
        #region GLSL shader source code (as string constants)
        private static readonly string vertexShaderCode =
@"#version 140

const int MAX_BONES = " + Deformer.MaximumSize + @";

struct BoneList {
    uint size;
    mat4 elements[MAX_BONES];
};

struct Rectangle {
    vec2 position;
    vec2 scale;
};

in vec3 position;
in vec3 normal;
in vec2 textureCoordinate;
in vec4 boneIds;
in vec4 boneWeights;

out vec3 vertexNormal;
out vec2 vertexTextureCoordinate;
out vec3 vertexFragmentPosition;

uniform mat4 model;
uniform mat4 modelTransposedInversed;
uniform mat4 view;
uniform mat4 projection;
uniform BoneList bones;
uniform Rectangle textureClipping = Rectangle(vec2(0,0), vec2(1,1));

void main()
{
    //Calculate the clipped texture coordinates.
    vertexTextureCoordinate = vec2(
        textureClipping.position.x + textureCoordinate.x 
            * textureClipping.scale.x,
        -textureClipping.position.y - textureCoordinate.y 
            * textureClipping.scale.y);

    //If bone deformations were uploaded and the current vertex is mapped
    //to any bones, the individual deformation matrix is calculated.
    //Otherwise, the deformation will be the identity matrix.
    mat4 deformation;
    if (bones.size > uint(0) && boneWeights != vec4(0.0, 0.0, 0.0, 0.0)) 
    {
        deformation = bones.elements[uint(boneIds[0])] 
            * (float(boneWeights[0]) / 255.0);
        deformation += bones.elements[uint(boneIds[1])] 
            * (float(boneWeights[1]) / 255.0);
        deformation += bones.elements[uint(boneIds[2])] 
            * (float(boneWeights[2]) / 255.0);
        deformation += bones.elements[uint(boneIds[3])] 
            * (float(boneWeights[3]) / 255.0);
    }
    else deformation = mat4(1.0);
    
    //Combine all transformations.
    vec4 vertexPosition = deformation * vec4(position, 1.0);
    vertexNormal = vec3(modelTransposedInversed * 
        (vec4(normal, 0.0) * deformation));
    vertexFragmentPosition = vec3(model * vertexPosition);
    gl_Position = projection * view * model * vertexPosition;
}
";

        private static readonly string fragmentShaderCode =
@"#version 140

const int MAX_LIGHTS = " + LightsLimit + @";

const int LIGHTTYPE_AMBIENT = " + (int)LightType.Ambient + @";
const int LIGHTTYPE_POINT = " + (int)LightType.Point + @";
const int LIGHTTYPE_SPOT = " + (int)LightType.Spot + @";

const int SHADINGMODE_FLAT = " + (int)Eterra.Graphics.ShadingMode.Flat + @";
const int SHADINGMODE_PHONG = " + (int)Eterra.Graphics.ShadingMode.Phong + @";
const int SHADINGMODE_PBR = " + (int)Eterra.Graphics.ShadingMode.PBR + @";

const int MIXINGMODE_NORMAL = " + (int)MixingMode.Normal + @";
const int MIXINGMODE_MULTIPLY = " + (int)MixingMode.Multiply + @";

struct Light {
	int type;//Required by all light types
    vec3 color;//Required by all light types
	vec3 position;//Required by LIGHTTYPE_POINT and LIGHTTYPE_SPOT
	vec3 direction;//Required by LIGHTTYPE_AMBIENT and LIGHTTYPE_SPOT
	float cutoff;//Required by LIGHTTYPE_SPOT
    float cutoffWidth;//Required by LIGHTTYPE_SPOT
	float radius;//Required by LIGHTTYPE_POINT
    bool castShadows;//Required by all light types
};

in vec3 vertexNormal;
in vec2 vertexTextureCoordinate;
in vec3 vertexFragmentPosition;

out vec4 color;

uniform vec3 viewPosition;

uniform vec4 modelColor;
uniform int shadingMode;
uniform int mixingMode;
uniform float opacity;
uniform float specularIntensity = 40.0;

uniform sampler2D texture_main;
uniform bool texture_main_assigned;
uniform sampler2D texture_effect01;
uniform bool texture_effect01_assigned;
uniform sampler2D texture_effect02;
uniform bool texture_effect02_assigned;
uniform sampler2D texture_effect03;
uniform bool texture_effect03_assigned;

uniform Light[MAX_LIGHTS] lights;

vec4 CalculateFragmentFlat();
vec4 CalculateFragmentWithPhong();
vec4 CalculateFragmentWithPbr();
float GetFogFactor(float start, float length);

void main()
{
    float fogFactor = 1;//GetFogFactor(20, 5);

    if (shadingMode == SHADINGMODE_PHONG)
        color = CalculateFragmentWithPhong();
    else if (shadingMode == SHADINGMODE_PBR)
        color = CalculateFragmentWithPbr();
    else color = CalculateFragmentFlat();
    
    color.a *= opacity;//Old code: 1 - ((1 - opacity) * 0.2)
    color.a *= fogFactor;
    if (color.a < 0.05) discard;
}

float GetFogFactor(float start, float length)
{
    float distance = distance(viewPosition, vertexFragmentPosition);
    return min(max(start + length - distance, 0), length) / length;
}

vec4 GetTextureColor(sampler2D tex, bool isAssigned, bool mixWithBaseColor)
{
    if (!isAssigned) 
    {
        if (mixWithBaseColor) return modelColor;
        else return vec4(0, 0, 0, 1);
    }
    
    vec4 textureColor = texture(tex, vertexTextureCoordinate);
    if (!mixWithBaseColor) return textureColor;
    
    float textureOpacity = min(1, textureColor.a);
    float textureOpacityInverted = 1.0f - textureOpacity;

    if (textureOpacity < 0.05) discard;

    vec3 opacityReducedColor = modelColor.rgb * textureOpacityInverted;
    vec3 opacityReducedTextureColor = textureColor.rgb * textureOpacity;

    if (mixingMode == MIXINGMODE_NORMAL)
    {
        return vec4(opacityReducedColor.r + opacityReducedTextureColor.r,
            opacityReducedColor.g + opacityReducedTextureColor.g,
            opacityReducedColor.b + opacityReducedTextureColor.b,
            textureOpacity);
    }
    else
    {
        return vec4(modelColor.r * opacityReducedTextureColor.r,
            modelColor.g * opacityReducedTextureColor.g,
            modelColor.b * opacityReducedTextureColor.b,
            textureOpacity);
    }
}

vec4 CalculateFragmentFlat()
{
    return GetTextureColor(texture_main, texture_main_assigned, true);
}

vec4 CalculateFragmentWithPhong()
{
    vec4 fragmentDiffuse = GetTextureColor(texture_main, 
        texture_main_assigned, true);

    if (fragmentDiffuse.a < 0.01) discard;

    vec4 fragmentSpecular = GetTextureColor(texture_effect01, 
        texture_effect01_assigned, false);
    vec4 fragmentNormal = GetTextureColor(texture_effect02, 
        texture_effect02_assigned, false);
    vec4 fragmentEmission = GetTextureColor(texture_effect03, 
        texture_effect03_assigned, false);

    vec3 normal = normalize(vertexNormal);
    vec3 viewDirection = normalize(viewPosition - vertexFragmentPosition);

    vec3 illuminatedFragment = vec3(0, 0, 0);

    bool lightsFound = false;

    for (int i=0; i < MAX_LIGHTS; i++) 
    {
        Light light = lights[i];

        vec3 surfaceToLightDirection;
        if (light.type == LIGHTTYPE_AMBIENT) 
            surfaceToLightDirection = -light.direction;
        else if (light.type == LIGHTTYPE_POINT || light.type == LIGHTTYPE_SPOT)
            surfaceToLightDirection = 
                normalize(light.position - vertexFragmentPosition);
        else continue;

        lightsFound = true;

        vec3 ambient = light.color * fragmentDiffuse.rgb;
        vec3 diffuse = max(0.0, dot(normal, surfaceToLightDirection))
            * light.color * fragmentDiffuse.rgb;
        vec3 specular = pow(max(0.0, dot(viewDirection, 
            reflect(-surfaceToLightDirection, normal))), specularIntensity) * 
            light.color * fragmentSpecular.rgb;

        float attenuation = 1;

        if (light.type == LIGHTTYPE_POINT || light.type == LIGHTTYPE_SPOT)
        {
            float distanceToLight = 
                length(light.position - vertexFragmentPosition);

            float linear = ((115.22 / light.radius) + 84.97) / 
                (1.15 * light.radius * light.radius);
            float quadratic = ((2.85 / light.radius) + 4.9) / 
                (light.radius * 1.08);
            attenuation = 1.0 / 
                (quadratic * pow(distanceToLight, 2) 
                + linear * distanceToLight + 0.1);

            if (light.type == LIGHTTYPE_SPOT) 
            {                 
                float lightToSurfaceAngle = dot(surfaceToLightDirection, 
                    normalize(-light.direction));
                attenuation *= clamp((lightToSurfaceAngle - (light.cutoff 
                    - light.cutoffWidth)) / light.cutoffWidth, 0.0, 1.0);
            }
        }

        illuminatedFragment += (ambient + diffuse + specular) * attenuation;
    }

    if (lightsFound) 
    {
        illuminatedFragment = illuminatedFragment + 
            (fragmentEmission.rgb * fragmentEmission.a);
    
        return vec4(illuminatedFragment, fragmentDiffuse.a);
    }
    else return fragmentDiffuse + 
        vec4(fragmentEmission.rgb * fragmentEmission.a, 0);
}

vec4 CalculateFragmentWithPbr()
{
    //TODO: Implement PBR.
    return vec4(1,0,0,1);
}
";
        #endregion

        #region Uniform property declarations
        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// model transformation matrix. If the value of that uniform is 
        /// updated, the value of <see cref="ModelTransposedInversed"/>
        /// needs to be updated too!
        /// </summary>
        public Uniform<Matrix4x4> Model { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the
        /// inversed and then transposed version of the <see cref="Model"/>.
        /// </summary>
        public Uniform<Matrix4x4> ModelTransposedInversed { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the
        /// view transformation matrix.
        /// </summary>
        public Uniform<global::OpenTK.Matrix4> View { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// view position.
        /// </summary>
        public Uniform<Vector3> ViewPosition { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// bone deformation matrices.
        /// </summary>
        public Uniform<Deformer> Deformers { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the
        /// base model color.
        /// </summary>
        public Uniform<Color> Color { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the
        /// opacity of the drawn model. Values between 0.0 and 1.0 are valid.
        /// </summary>
        public Uniform<float> Opacity { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the
        /// shading mode (integer representation of 
        /// <see cref="Eterra.Graphics.ShadingMode"/>).
        /// </summary>
        public Uniform<int> ShadingMode { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the
        /// color texture mixing mode (integer representation of
        /// <see cref="MixingMode"/>).
        /// </summary>
        public Uniform<int> ColorTextureMixingMode { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// main texture buffer.
        /// The appeareance of this texture in the rendered image depends on
        /// the current value of <see cref="ShadingMode"/> and 
        /// <see cref="EnableTextures"/>.
        /// </summary>
        public Uniform<Eterra.Graphics.TextureBuffer> TextureMain { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// primary effect texture buffer.
        /// The appeareance of this texture in the rendered image depends on
        /// the current value of <see cref="ShadingMode"/> and 
        /// <see cref="EnableTextures"/>.
        /// </summary>
        public Uniform<Eterra.Graphics.TextureBuffer> TextureEffect01 { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// secondary effect texture buffer.
        /// The appeareance of this texture in the rendered image depends on
        /// the current value of <see cref="ShadingMode"/> and 
        /// <see cref="EnableTextures"/>.
        /// </summary>
        public Uniform<Eterra.Graphics.TextureBuffer> TextureEffect02 { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// tertiary effect texture buffer.
        /// The appeareance of this texture in the rendered image depends on
        /// the current value of <see cref="ShadingMode"/> and 
        /// <see cref="EnableTextures"/>.
        /// </summary>
        public Uniform<Eterra.Graphics.TextureBuffer> TextureEffect03 { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the clipping
        /// of the currently used material.
        /// </summary>
        public Uniform<Rectangle> TextureClipping { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the light array.
        /// </summary>
        public Uniform<LightSlot> Lights { get; }
        #endregion

        private ShaderRenderStage(int programHandle) : base(programHandle)
        {
            Model = new UniformMatrix4x4(
                programHandle, "model");
            ModelTransposedInversed = new UniformMatrix4x4(
                programHandle, "modelTransposedInversed");
            View = new UniformMatrix4(
                programHandle, "view");
            ViewPosition = new UniformVector3(
                programHandle, "viewPosition", true);
            Deformers = new UniformDeformer(
                programHandle, "bones");
            Color = new UniformColorRGBA(
                programHandle, "modelColor");
            Opacity = new UniformFloat(
                programHandle, "opacity");
            ShadingMode = new UniformInt(
                programHandle, "shadingMode");
            ColorTextureMixingMode = new UniformInt(
                programHandle, "mixingMode");
            TextureMain = new UniformTexture(
                programHandle, "texture_main", 0);
            TextureEffect01 = new UniformTexture(
                programHandle, "texture_effect01", 1);
            TextureEffect02 = new UniformTexture(
                programHandle, "texture_effect02", 2);
            TextureEffect03 = new UniformTexture(
                programHandle, "texture_effect03", 3);
            TextureClipping = new UniformRectangle(
                programHandle, "textureClipping");
            Lights = new UniformLightSlot(
                programHandle, "lights", LightsLimit);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderRenderStage"/> class.
        /// </summary>
        /// <returns></returns>
        public static ShaderRenderStage Create()
        {
            int programHandle;
            try
            {
                programHandle = CreateShaderProgram(
                    vertexShaderCode, fragmentShaderCode);
            }
            catch (Exception exc)
            {
                throw new ApplicationException("The shader program " +
                    "couldn't be compiled.", exc);
            }
            return new ShaderRenderStage(programHandle);
        }
    }
}
