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
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace ShamanTK.Platforms.DesktopGL.Graphics
{
    class ShaderRenderStage : Shader
    {
        #region Shader-specific uniform class definitions
        /// <remarks>
        /// The uniform must be a struct with the following layout:
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

            protected override bool OnSet(Rectangle value)
            {
                if (!IsAccessible) return false;

                GL.Uniform2(positionLocation, value.X, value.Y);
                GL.Uniform2(scaleLocation, value.Width, value.Height);

                currentValue = value;
                return true;
            }
        }

        /// <remarks>
        /// The uniform must be a struct with the following layout:
        /// <c>
        /// struct Fog {
        ///     float onsetDistance;
        ///     float falloffLength;
        ///     vec4 color;
        ///     bool ignoreYAxis;
        /// };
        /// </c>
        /// </remarks>
        protected class UniformFog : Uniform<Fog>
        {
            public override bool IsAccessible
            {
                get
                {
                    return onsetDistanceUniformLocation > -1 ||
                        falloffLengthUniformLocation > -1 ||
                        colorUniform.IsAccessible ||
                        ignoreYAxisUniformLocation > -1;
                }
            }

            public override Fog CurrentValue => currentValue;
            private Fog currentValue;

            private readonly int onsetDistanceUniformLocation;
            private readonly int falloffLengthUniformLocation;
            private readonly UniformColorRGBA colorUniform;
            private readonly int ignoreYAxisUniformLocation;

            public UniformFog(int programHandle, string identifier)
            {
                if (identifier == null)
                    throw new ArgumentNullException(nameof(identifier));

                onsetDistanceUniformLocation = GL.GetUniformLocation(
                    programHandle, identifier + ".onsetDistance");
                falloffLengthUniformLocation = GL.GetUniformLocation(
                    programHandle, identifier + ".falloffLength");
                colorUniform = new UniformColorRGBA(programHandle, 
                    identifier + ".color");
                ignoreYAxisUniformLocation = GL.GetUniformLocation(
                    programHandle, identifier + ".ignoreYAxis");
            }

            protected override bool OnSet(Fog value)
            {
                if (!IsAccessible) return false;
                currentValue = value;

                GL.Uniform1(onsetDistanceUniformLocation, value.OnsetDistance);
                GL.Uniform1(falloffLengthUniformLocation, value.FalloffLength);
                colorUniform.Set(value.Color);
                GL.Uniform1(ignoreYAxisUniformLocation, 
                    value.IgnoreYAxis ? 1 : 0);

                return true;
            }
        }

        /// <remarks>
        /// The uniform must be a struct with the following layout:
        /// <c>
        /// struct Light {
        ///     int type;
        ///     vec3 color;
        ///     vec3 position;
        ///     vec3 direction;
        ///     float cutoff;
        ///     float cutoffWidth;
        ///     float radius;
        /// };
        /// </c>
        /// </remarks>
        protected class UniformLight : Uniform<Light>
        {
            public override Light CurrentValue => currentValue;
            private Light currentValue;

            private readonly UniformInt type;
            private readonly UniformColorRGB color;
            private readonly UniformVector3 position, direction;
            private readonly UniformFloat cutoff, cutoffWidth, radius;

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
            }

            protected override bool OnSet(Light value)
            {
                bool allSet = true;

                if (value.Type == LightType.Ambient)
                {
                    allSet &= color.Set(value.Color);
                    allSet &= direction.Set(value.Direction);
                }
                else if (value.Type == LightType.Point)
                {
                    allSet &= color.Set(value.Color);
                    allSet &= position.Set(value.Position);
                    allSet &= radius.Set(value.Radius);
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
                }
                else if (value.Type != LightType.Disabled) return false;

                //Set the light type at last to prevent setting the light type
                //when the light parameters are invalid.
                allSet &= type.Set((int)value.Type);

                if (allSet) currentValue = value;

                return allSet;
            }
        }

        /*
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
                string identifier, int count)
            {
                if (identifier == null)
                    throw new ArgumentNullException(nameof(identifier));
                if (count < 0)
                    throw new ArgumentOutOfRangeException(nameof(count));

                if (programHandle >= 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        UniformLight light = new UniformLight(
                            programHandle, identifier + "[" + i + "]");
                        if (light.IsAccessible) lights.Add(light);
                        else break;
                    }
                }
            }

            protected override bool OnSet(LightSlot value)
            {
                if (!IsAccessible || value.Slot >= lights.Count) return false;

                currentValue = value;
                return lights[(int)value.Slot].Set(value.Light);
            }
        }
        */

        /// <remarks>
        /// The uniform must be a struct with the following layout:
        /// <c>
        /// struct SampleList {
        ///     int count;
        ///     ELEMENT_TYPE elements[MAXIMUM_COUNT];
        /// };
        /// uniform SampleList samples;
        /// </c>
        /// The MAXIMUM_COUNT constant value should be equal to the
        /// maximumCount parameter provided to the constructor of this class.
        /// The ELEMENT_TYPE should be the GLSL equivalent type of
        /// <typeparamref name="T"/>, which gets assigned by a element uniform
        /// provided by the factory specified in the constructor of this clas.
        /// </remarks>
        protected class UniformCollection<T> : Uniform<IReadOnlyCollection<T>>
        {
            private readonly int countUniformLocation;
            private readonly Uniform<T>[] elementUniforms;

            public override IReadOnlyCollection<T> CurrentValue 
                => currentValue;
            private IReadOnlyCollection<T> currentValue;

            public override bool IsAccessible =>
                elementUniforms.Length > 0 && countUniformLocation > -1;

            public UniformCollection(int programHandle, string identifier,
                int maximumCount, 
                Func<int, string, Uniform<T>> elementUniformFactory)
            {
                if (identifier == null)
                    throw new ArgumentNullException(nameof(identifier));
                if (maximumCount < 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(maximumCount));

                if (programHandle >= 0)
                {
                    countUniformLocation = GL.GetUniformLocation(
                        programHandle, identifier + ".count");

                    List<Uniform<T>> elementUniformsList = 
                        new List<Uniform<T>>();
                    for (int i = 0; i < maximumCount; i++)
                    {
                        string elementUniformIdentifier = identifier +
                            ".elements[" + i + "]";

                        Uniform<T> elementUniform = elementUniformFactory(
                            programHandle, elementUniformIdentifier);

                        if (elementUniform.IsAccessible) 
                            elementUniformsList.Add(elementUniform);
                        else break;
                    }
                    elementUniforms = elementUniformsList.ToArray();
                }
            }

            protected override bool OnSet(IReadOnlyCollection<T> value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!IsAccessible) return false;

                GL.Uniform1(countUniformLocation, value.Count);

                currentValue = value;

                bool allSet = value.Count <= elementUniforms.Length;

                int i = 0;
                foreach (T element in value)
                {
                    allSet &= elementUniforms[i++].Set(element);
                    if (i >= elementUniforms.Length) break;
                }

                return allSet;
            }
        }

        /// <remarks>
        /// The uniform must be of a type which implements the 
        /// following struct layout:
        /// <c>
        /// const int CAPACITY = 16;
        /// struct List {
        ///     int size;
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

            public UniformDeformer(int programHandle, string identifier, 
                int count)
            {
                if (identifier == null)
                    throw new ArgumentNullException(nameof(identifier));
                if (count < 0)
                    throw new ArgumentOutOfRangeException(nameof(count));

                if (programHandle >= 0)
                {
                    assignedElementsLocation = GL.GetUniformLocation(
                        programHandle, identifier + ".size");
                    for (int i = 0; i < count; i++)
                    {
                        UniformMatrix4x4 matrix =
                            new UniformMatrix4x4(programHandle,
                            identifier + ".elements[" + i + "]");
                        if (matrix.IsAccessible) matrices.Add(matrix);
                        else break;
                    }
                }
            }

            protected override bool OnSet(Deformer value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!IsAccessible) return false;

                GL.Uniform1(assignedElementsLocation, value.Length);

                currentValue = value;

                bool allSet = value.Length <= matrices.Count;

                for (byte i = 0; i < value.Length && i < matrices.Count; i++)
                    allSet &= matrices[i].Set(value[i]);

                return allSet;
            }
        }
        #endregion

        #region GLSL shader source code (and related constants)
        /// <summary>
        /// Defines the amount of vector uniforms which are always used by
        /// the vertex shader of this class (excluding the possible bone
        /// deformers with 4 vectors per deformer/bone).
        /// </summary>
        internal const int BaseVertexShaderVectorCount = 20;

        private static readonly string VertexShaderCode = 
            VertexShaderVersionPrefix +
@"const int MAX_BONES = %MAX_BONES%;

const int PROPERTYFORMAT_NONE = " 
+ (int)ShamanTK.Common.VertexPropertyDataFormat.None + @";
const int PROPERTYFORMAT_DEFORMER_ATTACHMENTS = " 
+ (int)ShamanTK.Common.VertexPropertyDataFormat.DeformerAttachments + @";
const int PROPERTYFORMAT_COLOR_LIGHT = " 
+ (int)ShamanTK.Common.VertexPropertyDataFormat.ColorLight + @";

struct BoneList {
    int size;
    mat4 elements[MAX_BONES];
};

struct Rectangle {
    vec2 position;
    vec2 scale;
};

in vec3 position;
in vec3 normal;
in vec2 textureCoordinate;
in vec4 propertySegment1;
in vec4 propertySegment2;

out vec3 vertexNormal;
out vec2 vertexTextureCoordinate;
out vec3 vertexFragmentPosition;
out vec4 vertexColor;
out vec4 vertexLight;

uniform mat4 model;
uniform mat4 modelTransposedInversed;
uniform mat4 view;
uniform mat4 projection;
uniform int vertexPropertyDataFormat;
uniform BoneList bones;
uniform Rectangle textureClipping;// = Rectangle(vec2(0,0), vec2(1,1));

void main()
{
    //Calculate the clipped texture coordinates.
    vertexTextureCoordinate = vec2(
        textureClipping.position.x + textureCoordinate.x 
            * textureClipping.scale.x,
        -textureClipping.position.y - textureCoordinate.y 
            * textureClipping.scale.y);

    //If the vertex properties are defined as deformer attachments and the 
    //current vertex is mapped to any bones (which is the case when the second 
    //vertex properties segment is not just 0.0), the individual deformation 
    //matrix is calculated using the two vertex property segments and the 
    //vertex colors are set to 0.
    //Otherwise, the deformation will be the identity matrix and the property
    //segments are interpreted as color/light and sent to the fragment shader.
    mat4 deformation = mat4(1.0);

    vertexColor = vec4(0.0);
    vertexLight = vec4(1.0);

    if (vertexPropertyDataFormat == PROPERTYFORMAT_DEFORMER_ATTACHMENTS) 
    {
        if (bones.size > 0 && propertySegment2 != vec4(0.0, 0.0, 0.0, 0.0)) 
        {
            deformation = bones.elements[int(propertySegment1[0])] 
                * (float(propertySegment2[0]) / 255.0);
            deformation += bones.elements[int(propertySegment1[1])] 
                * (float(propertySegment2[1]) / 255.0);
            deformation += bones.elements[int(propertySegment1[2])] 
                * (float(propertySegment2[2]) / 255.0);
            deformation += bones.elements[int(propertySegment1[3])] 
                * (float(propertySegment2[3]) / 255.0);
        }
    } 
    else if (vertexPropertyDataFormat == PROPERTYFORMAT_COLOR_LIGHT)
    {
        vertexColor = vec4(float(propertySegment1[0]) / 255.0,
            float(propertySegment1[1]) / 255.0, 
            float(propertySegment1[2]) / 255.0,
            float(propertySegment1[3]) / 255.0);
        vertexLight = vec4(float(propertySegment1[0]) / 255.0,
            float(propertySegment1[1]) / 255.0, 
            float(propertySegment1[2]) / 255.0,
            float(propertySegment1[3]) / 255.0);
    }

    //Combine all transformations.
    vec4 vertexPosition = deformation * vec4(position, 1.0);
    vertexNormal = vec3(modelTransposedInversed * 
        (vec4(normal, 0.0) * deformation));
    vertexFragmentPosition = vec3(model * vertexPosition);
    gl_Position = projection * view * model * vertexPosition;
}
";

        /// <summary>
        /// Defines the amount of vector uniforms which are always used by
        /// the fragment shader of this class (excluding the possible light
        /// source vectors with 3 vectors per light source).
        /// </summary>
        internal const int BaseFragmentShaderVectorCount = 2;

        private static readonly string FragmentShaderCode =
            FragmentShaderVersionPrefix +
@"const int MAX_LIGHTS = %MAX_LIGHTS%;

const int LIGHTTYPE_AMBIENT = " + (int)LightType.Ambient + @";
const int LIGHTTYPE_POINT = " + (int)LightType.Point + @";
const int LIGHTTYPE_SPOT = " + (int)LightType.Spot + @";

const int BLENDINGMODE_NONE = " + (int)BlendingMode.None + @";
const int BLENDINGMODE_ADD = " + (int)BlendingMode.Add + @";
const int BLENDINGMODE_MULTIPLY = " + (int)BlendingMode.Multiply + @";

struct Light {
	int type;//Required by all light types
    vec3 color;//Required by all light types
	vec3 position;//Required by LIGHTTYPE_POINT and LIGHTTYPE_SPOT
	vec3 direction;//Required by LIGHTTYPE_AMBIENT and LIGHTTYPE_SPOT
	float cutoff;//Required by LIGHTTYPE_SPOT
    float cutoffWidth;//Required by LIGHTTYPE_SPOT
	float radius;//Required by LIGHTTYPE_POINT
};

struct LightList {
    int count;
    Light elements[MAX_LIGHTS];
};

struct Fog {
    float onsetDistance;
    float falloffLength;
    vec4 color;
    bool ignoreYAxis;
};

in vec3 vertexNormal;
in vec2 vertexTextureCoordinate;
in vec3 vertexFragmentPosition;
in vec4 vertexColor;
in vec4 vertexLight;

uniform vec3 viewPosition;

uniform vec4 modelColor;
uniform int vertexPropertyDataFormat;
uniform int colorBlending;
uniform int textureBlending;
uniform float opacity;
uniform float specularIntensity = 40.0;
uniform Fog fog;
uniform LightList lights;

uniform sampler2D texture_main;
uniform bool texture_main_assigned;
uniform sampler2D texture_effect01;
uniform bool texture_effect01_assigned;
uniform sampler2D texture_effect02;
uniform bool texture_effect02_assigned;
uniform sampler2D texture_effect03;
uniform bool texture_effect03_assigned;

float getFogIntensity();
vec4 blend(vec4 primaryColor, vec4 secondaryColor, int blendingMode);
vec3 calculateLightingPhong();

void main()
{
    float fogIntensity = getFogIntensity();
    if (fogIntensity >= 1.0) 
    {
        gl_FragColor = fog.color;
        return;
    }

    vec4 surfaceColor = blend(modelColor, vertexColor, colorBlending);

    if (texture_main_assigned)
    {
        vec4 textureColor = texture(texture_main, vertexTextureCoordinate);
        surfaceColor = blend(textureColor, surfaceColor, textureBlending);
    }

    if (surfaceColor.a < 0.012) discard;

    vec4 surfaceLight = vertexLight;
    if (lights.count > 0) 
    {
        vec3 phongLight = calculateLightingPhong();
        surfaceLight += vec4(phongLight.x, phongLight.y, phongLight.z, 1);
    }

    gl_FragColor = surfaceColor * surfaceLight;
    
    if (texture_effect03_assigned)
    {
        gl_FragColor += texture(texture_effect03, vertexTextureCoordinate);
    }

    gl_FragColor.a *= opacity;

    gl_FragColor *= (1 - fogIntensity);
    gl_FragColor += fog.color * fogIntensity;
}

vec4 blend(vec4 primaryColor, vec4 secondaryColor, int blendingMode) 
{
    if (blendingMode == BLENDINGMODE_ADD) 
        return primaryColor + secondaryColor;
    else if (blendingMode == BLENDINGMODE_MULTIPLY)
        return primaryColor * secondaryColor;
    else return secondaryColor;
}

float getFogIntensity() 
{
    if (fog.onsetDistance == 0 && fog.falloffLength == 0) return 0.0;

    float fragmentDistance;

    if (fog.ignoreYAxis) 
        fragmentDistance = distance(vec3(viewPosition.x, 0, -viewPosition.z),
            vec3(vertexFragmentPosition.x, 0, vertexFragmentPosition.z));
    else fragmentDistance = distance(vec3(viewPosition.x, viewPosition.y, 
        -viewPosition.z), vertexFragmentPosition);

    return 1 - min(max(fog.onsetDistance + fog.falloffLength - 
        fragmentDistance, 0), fog.falloffLength) / fog.falloffLength;
}

vec3 calculateLightingPhong() 
{
    vec4 specularColor = vec4(0.0);
    if (texture_effect01_assigned) 
        specularColor = texture(texture_effect01, vertexTextureCoordinate);

    vec3 normal = normalize(vertexNormal);
    vec3 viewDirection = normalize(viewPosition - vertexFragmentPosition);

    vec3 lighting = vec3(0);

    for (int i=0; i < lights.count; i++) 
    {
        Light light = lights.elements[i];

        vec3 surfaceToLightDirection;
        if (light.type == LIGHTTYPE_AMBIENT) 
            surfaceToLightDirection = -light.direction;
        else if (light.type == LIGHTTYPE_POINT || light.type == LIGHTTYPE_SPOT)
            surfaceToLightDirection = 
                normalize(light.position - vertexFragmentPosition);
        else continue; //For unrecognized light types or disabled lights

        vec3 ambient = light.color;
        vec3 diffuse = max(0, dot(normal, surfaceToLightDirection)) 
            * light.color;
        vec3 specular = pow(max(0.0, dot(viewDirection, 
            reflect(-surfaceToLightDirection, normal))), specularIntensity) * 
            light.color;

        float attenuation = 1;

        if (light.type == LIGHTTYPE_POINT || light.type == LIGHTTYPE_SPOT)
        {
            float distanceToLight = 
                length(light.position - vertexFragmentPosition);

            float linear = ((115.22 / light.radius) + 84.97) / 
                (1.15 * light.radius * light.radius);
            float quadratic = ((2.85 / light.radius) + 4.9) / 
                (light.radius * 1.08);
            attenuation = 1.0 / (quadratic * pow(distanceToLight, 2) 
                + linear * distanceToLight + 0.1);

            if (light.type == LIGHTTYPE_SPOT) 
            {                 
                float lightToSurfaceAngle = dot(surfaceToLightDirection, 
                    normalize(-light.direction));
                attenuation *= clamp((lightToSurfaceAngle - (light.cutoff 
                    - light.cutoffWidth)) / light.cutoffWidth, 0.0, 1.0);
            }
        }

        lighting += (ambient + diffuse + specular) * attenuation;
    }

    return lighting;
}
/*
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
}*/
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
        public Uniform<OpenTK.Mathematics.Matrix4> View { get; }

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
        /// primary vertex color mixing mode (integer representation of
        /// <see cref="VertexPropertyDataFormat"/>).
        /// </summary>
        public Uniform<int> VertexPropertyDataFormat { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the
        /// color blending mode (integer representation of
        /// <see cref="BlendingMode"/>).
        /// </summary>
        public Uniform<int> ColorBlending { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the
        /// texture blending mode (integer representation of
        /// <see cref="BlendingMode"/>).
        /// </summary>
        public Uniform<int> TextureBlending { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// main texture buffer.
        /// The appeareance of this texture in the rendered image depends on
        /// the current value of <see cref="ShadingMode"/> and 
        /// <see cref="EnableTextures"/>.
        /// </summary>
        public Uniform<ShamanTK.Graphics.TextureBuffer> TextureMain { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// primary effect texture buffer.
        /// The appeareance of this texture in the rendered image depends on
        /// the current value of <see cref="ShadingMode"/> and 
        /// <see cref="EnableTextures"/>.
        /// </summary>
        public Uniform<ShamanTK.Graphics.TextureBuffer> TextureEffect01 { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// secondary effect texture buffer.
        /// The appeareance of this texture in the rendered image depends on
        /// the current value of <see cref="ShadingMode"/> and 
        /// <see cref="EnableTextures"/>.
        /// </summary>
        public Uniform<ShamanTK.Graphics.TextureBuffer> TextureEffect02 { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the 
        /// tertiary effect texture buffer.
        /// The appeareance of this texture in the rendered image depends on
        /// the current value of <see cref="ShadingMode"/> and 
        /// <see cref="EnableTextures"/>.
        /// </summary>
        public Uniform<ShamanTK.Graphics.TextureBuffer> TextureEffect03 { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the clipping
        /// of the currently used material.
        /// </summary>
        public Uniform<Rectangle> TextureClipping { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the light collection.
        /// </summary>
        public Uniform<IReadOnlyCollection<Light>> Lights { get; }

        /// <summary>
        /// Gets the shader uniform value accessor for the fog effect.
        /// </summary>
        public Uniform<Fog> Fog { get; }
        #endregion

        private ShaderRenderStage(int programHandle, 
            int supportedDeformersCount, int supportedLightsCount) 
            : base(programHandle)
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
                programHandle, "bones", supportedDeformersCount);
            Color = new UniformColorRGBA(
                programHandle, "modelColor");
            Opacity = new UniformFloat(
                programHandle, "opacity");
            VertexPropertyDataFormat = new UniformInt(
                programHandle, "vertexPropertyDataFormat");
            ColorBlending = new UniformInt(
                programHandle, "colorBlending");
            TextureBlending = new UniformInt(
                programHandle, "textureBlending");
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
            Lights = new UniformCollection<Light>(
                programHandle, "lights", supportedLightsCount,
                (p, i) => new UniformLight(p, i));
            Fog = new UniformFog(
                programHandle, "fog");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderRenderStage"/> 
        /// class.
        /// </summary>
        /// <returns>
        /// A new instance of the <see cref="ShaderRenderStage"/> class.
        /// </returns>
        /// <exception cref="ApplicationException">
        /// Is thrown when the shader creation failed due to version 
        /// incompatibility issues or errors in the shader code.
        /// </exception>
        public static ShaderRenderStage Create(Graphics parentGraphics)
        {
            if (!parentGraphics.TryGetPlatformLimit(PlatformLimit.DeformerSize,
                out int supportedDeformersCount)) 
                supportedDeformersCount = Graphics.MaximumDeformers;
            if (!parentGraphics.TryGetPlatformLimit(PlatformLimit.LightCount,
                out int supportedLightsCount)) 
                supportedLightsCount = Graphics.MaximumLights;

            int programHandle;
            try
            {
                programHandle = CreateShaderProgram(
                    VertexShaderCode.Replace("%MAX_BONES%",
                    supportedDeformersCount.ToString()), 
                    FragmentShaderCode.Replace("%MAX_LIGHTS%",
                    supportedLightsCount.ToString()));
            }
            catch (Exception exc)
            {
                throw new ApplicationException("The shader program " +
                    "couldn't be created.", exc);
            }
            return new ShaderRenderStage(programHandle,
                supportedDeformersCount, supportedLightsCount);
        }
    }
}
