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
using ShamanTK.IO;
using SharpGLTF.Animations;
using SharpGLTF.IO;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace ShamanTK.Platforms.Common.IO
{
    class GLTFormatHandler : IResourceFormatHandler
    {
        /// <summary>
        /// Defines the "step size" in seconds in which cubic spline 
        /// interpolated animations are rasterized and converted to
        /// either an animation with <see cref="InterpolationMethod.Linear"/>
        /// (if <see cref="CubicSplineInterpolationUseLinearInterpolation"/>
        /// is <c>true</c>) or with <see cref="InterpolationMethod.None"/>
        /// otherwise.
        /// </summary>
        private const float CubicSplineInterpolationRasterisationFrequency =
            1 / 25f;

        /// <summary>
        /// Defines whether an animation with cubic spline interpolation should
        /// be rasterized into an animation with 
        /// <see cref="InterpolationMethod.Linear"/> so that, for higher 
        /// framerates, the animation is still fluent (<c>true</c>) or if there
        /// should be no interpolation between the rasterized frames in the
        /// target animation which saves processing power (<c>false</c>).
        /// </summary>
        private const bool CubicSplineInterpolationUseLinearInterpolation =
            true;

        private const string VertexAttributePosition = "POSITION";

        private const string VertexAttributeNormal = "NORMAL";

        private const string VertexAttributeTexcoord = "TEXCOORD_0";

        private const string VertexAttributeColor = "COLOR_0";

        private const string VertexAttributeJoints = "JOINTS_0";

        private const string VertexAttributeWeights = "WEIGHTS_0";

        public bool SupportsExport(string fileExtensionLowercase)
        {
            return false;
        }

        public bool SupportsImport(string fileExtensionLowercase)
        {
            return fileExtensionLowercase == "gltf" || 
                fileExtensionLowercase == "glb";
        }

        public void Export(object resource, ResourceManager manager, 
            ResourcePath path, bool overwrite)
        {
            throw new NotSupportedException("Exporting is not supported " +
                "by this format handler yet.");
        }

        public T Import<T>(ResourceManager manager, ResourcePath path)
        {
            if (!SupportsImport(path.Path.GetFileExtension().ToLower()))
                throw new NotSupportedException("The specified file type is " +
                    "not supported by the current handler!");

            ModelRoot root;
            try
            {
                using Stream stream = manager.OpenFile(path);
                FileSystemPath pathRoot = path.Path.GetParentDirectory();
                ReadContext readContext = CreateReadContext(manager, pathRoot);
                try { root = readContext.ReadSchema2(stream); }
                catch (Exception exc)
                {
                    throw new FormatException("The file couldn't be " +
                        "loaded.", exc);
                }
            }
            catch (FileNotFoundException) { throw; }
            catch (FormatException) { throw; }
            catch (IOException) { throw; }

            ShamanTK.Common.Scene scene = ImportScene(root.DefaultScene);
            if (typeof(T).IsAssignableFrom(scene.GetType()))
                return (T)(object)scene;
            else throw new ArgumentException("The loaded resource of " +
                $"type '{scene.GetType().Name}' couldn't be converted to " +
                $"the requested type '{typeof(T).Name}'.");
        }

        static ShamanTK.Common.Scene ImportScene(SharpGLTF.Schema2.Scene scene)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));

            var meshCache = new Dictionary<Mesh, MeshData>();
            var textureCache = new Dictionary<Texture, TextureData>();

            var timelines = ImportTimelines(scene.LogicalParent);

            ParameterCollection targetSceneParameters = 
                new ParameterCollection("Scene");

            if (scene.Extras != null)
            {
                JsonDictionary extras = scene.TryUseExtrasAsDictionary(false);
                foreach (KeyValuePair<string, object> parameter in extras)
                {
                    targetSceneParameters[
                        ParameterIdentifier.Create(parameter.Key)] =
                        parameter.Value;
                }
            }

            ShamanTK.Common.Scene targetSceneHierarchy = 
                new ShamanTK.Common.Scene(targetSceneParameters);

            var importStack = new Stack<(Node source, 
                Node<ParameterCollection> targetParent)>();

            foreach (Node source in scene.VisualChildren)
                importStack.Push((source, targetSceneHierarchy));

            while (importStack.Count > 0)
            {
                (Node source, Node<ParameterCollection> targetParent) = 
                    importStack.Pop();

                ParameterCollection targetParameters =
                    ImportNode(source, meshCache, textureCache);

                if (timelines.TryGetValue(source, out Timeline timeline))
                    targetParameters[ParameterIdentifier.Timeline] = timeline;

                Node<ParameterCollection> target =
                    targetParent.AddChild(targetParameters);

                int childrenCount = 0;
                foreach (Node sourceChild in source.VisualChildren)
                {
                    importStack.Push((sourceChild, target));
                    childrenCount++;
                }
            }

            targetSceneHierarchy.DissolveNodesWithout(false,
                ParameterIdentifier.MeshData, ParameterIdentifier.Light);

            return targetSceneHierarchy;
        }

        static ParameterCollection ImportNode(Node node,
            Dictionary<Mesh, MeshData> meshCache,
            Dictionary<Texture, TextureData> textureCache)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            ParameterCollection parameters = 
                new ParameterCollection(node.Name ?? "");

            if (node.Mesh != null && node.Mesh.Primitives.Count > 0)
            {
                MeshPrimitive meshPrimitive = node.Mesh.Primitives[0];

                if (meshCache == null ||
                    !meshCache.TryGetValue(node.Mesh, out MeshData mesh))
                {
                    if (node.Mesh.Primitives.Count > 1)
                        Log.Warning($"The mesh '{node.Mesh.Name}' contains " +
                            "more than one mesh primitive - only the first " +
                            "mesh primitive will be imported, the others " +
                            "will be ignored.", nameof(GLTFormatHandler));

                    mesh = ImportMeshPrimitive(meshPrimitive,
                        () => ImportSkeleton(node));

                    if (meshCache != null) meshCache[node.Mesh] = mesh;
                }

                parameters[ParameterIdentifier.MeshData] = mesh;

                ImportMaterialParameters(meshPrimitive.Material,
                    parameters, textureCache);
            }

            if (node.PunctualLight != null)
                parameters[ParameterIdentifier.Light] =
                    ImportLight(node.PunctualLight, node.LocalTransform);

            if (node.Extras != null)
            {
                JsonDictionary extras = node.TryUseExtrasAsDictionary(false);
                foreach (KeyValuePair<string, object> parameter in extras)
                {
                    parameters[ParameterIdentifier.Create(parameter.Key)] =
                        parameter.Value;
                }
            }

            parameters[ParameterIdentifier.Position] =
                node.LocalTransform.Translation;
            parameters[ParameterIdentifier.Scale] =
                node.LocalTransform.Scale;
            parameters[ParameterIdentifier.Rotation] =
                node.LocalTransform.Rotation;

            return parameters;
        }

        /// <summary>
        /// Imports all animations into a collection of timelines, associated
        /// to the visual root nodes the animation belongs to.
        /// </summary>
        /// <param name="root">
        /// The <see cref="ModelRoot"/> of the file to be imported.
        /// </param>
        /// <returns>
        /// The imported <see cref="Timeline"/> instances, associated to the
        /// visual root nodes which they affect.
        /// </returns>
        static Dictionary<Node, Timeline> ImportTimelines(ModelRoot root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            // Import the timeline layers and their associated markers
            // and associate them to their visual root node with a dictionary.
            var rootNodeTimelineLayerLinks = new Dictionary<Node,
                (SortedList<double, Marker> markers, 
                List<TimelineLayer> layers)>();

            foreach (Node node in root.LogicalNodes)
            {
                SortedList<double, Marker> markers;
                List<TimelineLayer> layers;

                if (rootNodeTimelineLayerLinks.TryGetValue(node.VisualRoot,
                    out var rootNodeTimelineLayerLink))
                {
                    markers = rootNodeTimelineLayerLink.markers;
                    layers = rootNodeTimelineLayerLink.layers;
                }
                else
                {
                    markers = new SortedList<double, Marker>();
                    layers = new List<TimelineLayer>();
                }

                TimelineLayer timelineLayer = ImportTimelineLayer(node,
                    markers, root.LogicalAnimations);

                if (timelineLayer.HasKeyframes)
                {
                    layers.Add(timelineLayer);
                    rootNodeTimelineLayerLinks[node.VisualRoot] = 
                        (markers, layers);
                }
            }

            // Convert the previously generated collections into the final
            // timeline instances, linked to their common visual root node.
            var rootNodeTimelineLinks = new Dictionary<Node, Timeline>();
            foreach (var rootNodeTimelineLayerLink in 
                rootNodeTimelineLayerLinks)
            {
                rootNodeTimelineLinks[rootNodeTimelineLayerLink.Key] =
                    new Timeline(rootNodeTimelineLayerLink.Value.layers,
                    rootNodeTimelineLayerLink.Value.markers.Values);
            }

            return rootNodeTimelineLinks;
        }

        static TimelineLayer ImportTimelineLayer(Node node, 
            SortedList<double, Marker> markerTargetList,
            IEnumerable<SharpGLTF.Schema2.Animation> sourceAnimations)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (markerTargetList == null)
                throw new ArgumentNullException(nameof(markerTargetList));
            if (sourceAnimations == null)
                throw new ArgumentNullException(nameof(sourceAnimations));

            SortedList<double, Keyframe<Vector3>> positionKeyframes =
                new SortedList<double, Keyframe<Vector3>>();
            SortedList<double, Keyframe<Vector3>> scaleKeyframes =
                new SortedList<double, Keyframe<Vector3>>();
            SortedList<double, Keyframe<Quaternion>> rotationKeyframes =
                new SortedList<double, Keyframe<Quaternion>>();

            InterpolationMethod positionInterpolationMethod =
                    InterpolationMethod.None;
            InterpolationMethod scaleInterpolationMethod =
                InterpolationMethod.None;
            InterpolationMethod rotationInterpolationMethod =
                InterpolationMethod.None;

            // This variable is used to offset the start of appended animations
            // and increases after every processed animation.
            double timeOffset = 0;

            foreach (var sourceAnimation in sourceAnimations)
            {
                bool framesImported = false;

                IAnimationSampler<Vector3> translationSampler =
                    sourceAnimation.FindTranslationSampler(node);
                if (translationSampler != null)
                {
                    framesImported |= ImportKeyframes(translationSampler, 
                        positionKeyframes, timeOffset, 
                        out positionInterpolationMethod);
                }

                IAnimationSampler<Vector3> scaleSampler =
                    sourceAnimation.FindScaleSampler(node);
                if (scaleSampler != null)
                {
                    framesImported |= ImportKeyframes(scaleSampler, 
                        scaleKeyframes, timeOffset, 
                        out scaleInterpolationMethod);
                }

                IAnimationSampler<Quaternion> rotationSampler =
                    sourceAnimation.FindRotationSampler(node);
                if (rotationSampler != null)
                {
                    framesImported |= ImportKeyframes(rotationSampler, 
                        rotationKeyframes, timeOffset, 
                        out rotationInterpolationMethod);
                }

                // The marker for the current animation should only be set
                // and the offset should only be incremented if the animation
                // actually contained keyframes that were imported.
                if (!framesImported) continue;

                markerTargetList[timeOffset] = new Marker(timeOffset,
                        sourceAnimation.Name);

                timeOffset += sourceAnimation.Duration +
                        Timeline.MarkerBreak.TotalSeconds;

                /*
                // This could fail if the timeline for one node has a different
                // length (e.g. missing keyframes at the end) than the other.
                double timeOffset2 = 0;
                if (positionKeyframes.Count > 0)
                    timeOffset2 = Math.Max(timeOffset2, positionKeyframes.Keys[
                        positionKeyframes.Count - 1] + 
                        Timeline.MarkerBreak.TotalSeconds);
                if (scaleKeyframes.Count > 0)
                    timeOffset2 = Math.Max(timeOffset2, scaleKeyframes.Keys[
                        scaleKeyframes.Count - 1] +
                        Timeline.MarkerBreak.TotalSeconds);
                if (rotationKeyframes.Count > 0)
                    timeOffset2 = Math.Max(timeOffset2, rotationKeyframes.Keys[
                        rotationKeyframes.Count - 1] +
                        Timeline.MarkerBreak.TotalSeconds);      
                */
            }

            TimelineParameter<Vector3> positionChannel =
                new TimelineParameter<Vector3>(ParameterIdentifier.Position,
                positionInterpolationMethod, positionKeyframes.Values);
            TimelineParameter<Vector3> scaleChannel =
                new TimelineParameter<Vector3>(ParameterIdentifier.Scale,
                scaleInterpolationMethod, scaleKeyframes.Values);
            TimelineParameter<Quaternion> rotationChannel =
                new TimelineParameter<Quaternion>(ParameterIdentifier.Rotation,
                rotationInterpolationMethod, rotationKeyframes.Values);

            return new TimelineLayer(node.Name, new TimelineParameter[]
            {
                positionChannel, scaleChannel, rotationChannel
            });
        }

        /// <summary>
        /// Imports an animation from an <see cref="IAnimationSampler{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of keyframes of this animation.
        /// </typeparam>
        /// <param name="sourceAnimationSampler">
        /// The source <see cref="IAnimationSampler{T}"/> instance.
        /// </param>
        /// <param name="targetKeyframeCollection">
        /// The <see cref="SortedList{TKey, TValue}"/> instance, into which
        /// the keyframes from the specified 
        /// <paramref name="sourceAnimationSampler"/> should be put.
        /// </param>
        /// <param name="timeOffset">
        /// The time offset (in seconds), which will be added to every 
        /// keyframe added to the specified 
        /// <paramref name="targetKeyframeCollection"/>.
        /// </param>
        /// <param name="interpolationMethod">
        /// The <see cref="InterpolationMethod"/>, which should be used when
        /// an animation with the keyframes imported into 
        /// <paramref name="targetKeyframeCollection"/> is played back.
        /// </param>
        /// <returns>
        /// <c>true</c> if keyframes were imported and added to the specified
        /// <paramref name="targetKeyframeCollection"/>, <c>false</c> if no
        /// keyframes were loaded.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="sourceAnimationSampler"/> or
        /// <paramref name="targetKeyframeCollection"/> are null.
        /// </exception>
        static bool ImportKeyframes<T>(
            IAnimationSampler<T> sourceAnimationSampler,
            SortedList<double, Keyframe<T>> targetKeyframeCollection, 
            double timeOffset,  out InterpolationMethod interpolationMethod)
            where T : unmanaged
        {
            if (sourceAnimationSampler == null)
                throw new ArgumentNullException(
                    nameof(sourceAnimationSampler));

            bool framesImported = false;

            interpolationMethod = InterpolationMethod.None;

            // As cubic spline interpolation isn't supported by the framework,
            // this type of animation would need to be "rasterized".
            if (sourceAnimationSampler.InterpolationMode ==
                AnimationInterpolationMode.CUBICSPLINE)
            {
                interpolationMethod =
                    CubicSplineInterpolationUseLinearInterpolation ?
                    InterpolationMethod.Linear : InterpolationMethod.None;

                float begin = float.MaxValue, end = float.MinValue;
                foreach (var key in sourceAnimationSampler.GetCubicKeys())
                {
                    begin = Math.Min(key.Key, begin);
                    end = Math.Max(key.Key, end);
                }

                ICurveSampler<T> curveSampler =
                    sourceAnimationSampler.CreateCurveSampler();

                for (float time = begin; time <= end;
                    time += CubicSplineInterpolationRasterisationFrequency)
                {
                    T value = curveSampler.GetPoint(time);
                    double offsettedTime = time + timeOffset;
                    targetKeyframeCollection.Add(offsettedTime, 
                        new Keyframe<T>(offsettedTime, value));
                    
                    framesImported = true;
                }
            }
            // For the animation types "LINEAR" and "STEP", the keyframes can
            // just be copied.
            else
            {
                if (sourceAnimationSampler.InterpolationMode ==
                    AnimationInterpolationMode.LINEAR)
                    interpolationMethod = InterpolationMethod.Linear;
                else if (sourceAnimationSampler.InterpolationMode ==
                    AnimationInterpolationMode.STEP)
                    interpolationMethod = InterpolationMethod.None;

                foreach (var (time, value) in
                    sourceAnimationSampler.GetLinearKeys())
                {
                    double offsettedTime = time + timeOffset;

                    targetKeyframeCollection.Add(offsettedTime, 
                        new Keyframe<T>(offsettedTime, value));

                    framesImported = true;
                }
            }

            return framesImported;
        }

        static Light ImportLight(PunctualLight light, 
            AffineTransform lightNodeTransformation)
        {
            if (light == null)
                throw new ArgumentNullException(nameof(light));

            Vector3 lightDirection =
                MathHelper.RotateDirection(Vector3.UnitZ, 
                lightNodeTransformation.Rotation);

            if (light.LightType == PunctualLightType.Directional)
                return new Light((Color)light.Color, lightDirection);
            else if (light.LightType == PunctualLightType.Point)
                return new Light((Color)light.Color,
                    lightNodeTransformation.Translation, light.Range);
            else if (light.LightType == PunctualLightType.Spot)
                return new Light((Color)light.Color, 
                    lightNodeTransformation.Translation, light.Range, 
                    lightDirection, light.InnerConeAngle,
                    light.OuterConeAngle - light.InnerConeAngle);
            else throw new ArgumentException("The specified light type is " +
                "not supported.");
        }

        static void ImportMaterialParameters(Material material, 
            ParameterCollection target, 
            Dictionary<Texture, TextureData> textureCache)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material));
            if (target == null)
                throw new ArgumentNullException(nameof(target));    

            foreach (MaterialChannel channel in material.Channels)
            {
                void tryAssignTexture(ParameterIdentifier identifier)
                {
                    if (channel.Texture != null)
                    {
                        if (textureCache == null || 
                            !textureCache.TryGetValue(channel.Texture,
                            out TextureData data))
                        {
                            data = ImportTexture(channel.Texture);

                            if (textureCache != null) 
                                textureCache[channel.Texture] = data;
                        }

                        target[identifier] = data;
                    }
                }

                switch (channel.Key)
                {
                    case "BaseColor":
                        target[ParameterIdentifier.BaseColor] =
                            (Color)channel.Parameter;
                        tryAssignTexture(ParameterIdentifier.BaseColorMap);
                        break;
                    case "Metallic":
                    case "MetallicRoughness":
                        tryAssignTexture(ParameterIdentifier.MetallicMap);
                        break;
                    case "Normal":
                        tryAssignTexture(ParameterIdentifier.NormalMap);
                        break;
                    case "Occlusion":
                        tryAssignTexture(ParameterIdentifier.OcclusionMap);
                        break;
                    case "Emissive":
                        tryAssignTexture(ParameterIdentifier.EmissiveMap);
                        break;
                    case "Specular":
                        tryAssignTexture(ParameterIdentifier.SpecularMap);
                        break;
                    default:
                        Log.Trace("Unsupported material channel " +
                            $"'{channel.Key}' will be ignored.",
                            nameof(GLTFormatHandler));
                        break;
                }
            }
        }

        static TextureData ImportTexture(Texture texture)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            using Stream stream = texture.PrimaryImage.Content.Open();

            return BitmapTextureData.FromStream(stream);
        }

        static MeshData ImportMeshPrimitive(MeshPrimitive meshPrimitive,
            Func<Skeleton> skeletonFactory)
        {
            if (meshPrimitive == null)
                throw new ArgumentNullException(nameof(meshPrimitive));
            if (meshPrimitive.DrawPrimitiveType != PrimitiveType.TRIANGLES)
                throw new ArgumentException("The mesh " +
                    $"'{meshPrimitive.LogicalParent.Name}' has an " +
                    "unsupported draw primitive type " +
                    $"(only {nameof(PrimitiveType.TRIANGLES)} is supported.");

            // For the import of the vertex data, a memory stream "wrapper" 
            // is created around every vertex attribute channel, then we'll 
            // "read" each stream simultaneously and assemble the data 
            // into Vertex instances.
            List<Vertex> vertices = new List<Vertex>();

            MemoryStream getVertexData(string attributeName, out int stride,
                out AttributeFormat format)
            {
                Accessor accessor = meshPrimitive.GetVertexAccessor(
                    attributeName);
                if (accessor != null)
                {
                    stride = accessor.SourceBufferView.ByteStride;
                    format = accessor.Format;
                    return new MemoryStream(
                        accessor.SourceBufferView.Content.Array,
                        accessor.SourceBufferView.Content.Offset,
                        accessor.SourceBufferView.Content.Count, false);
                }
                else
                {
                    stride = 0;
                    format = default;
                    return null;
                }
            }

            MemoryStream positions = getVertexData(VertexAttributePosition,
                out int positionStride, out AttributeFormat positionFormat);
            if (positions == null)
                throw new ArgumentException("The mesh " +
                    $"'{meshPrimitive.LogicalParent.Name}' doesn't contain " +
                    "vertex positions, which is invalid and can't be " +
                    "imported.");

            MemoryStream normals = getVertexData(VertexAttributeNormal,
                out int normalStride, out AttributeFormat normalFormat);
            MemoryStream texcoords = getVertexData(VertexAttributeTexcoord,
                out int texcoordsStride, out AttributeFormat texCoordsFormat);

            MemoryStream joints = getVertexData(VertexAttributeJoints,
                out int jointsStride, out AttributeFormat jointsFormat);
            MemoryStream weights = getVertexData(VertexAttributeWeights,
                out int weightsStride, out AttributeFormat weightsFormat);
            MemoryStream colors = getVertexData(VertexAttributeColor,
                out int colorsStride, out AttributeFormat colorsFormat);

            // Currently, the vertex property data can either store bone
            // attachments (joints and weights) or colors - but not both.
            // As the former is usually more important, the color channel is
            // ignored in such cases.
            if (joints != null && weights != null && colors != null)
            {
                Log.Trace($"The mesh '{meshPrimitive.LogicalParent.Name}' " +
                    "contains vertices with both bone attachments and a " +
                    "color, which is currently not supported - the color " +
                    "channel is ignored in that case.", 
                    nameof(GLTFormatHandler));
                colors.Dispose();
                colors = null;
            }

            VertexPropertyDataFormat vertexFormat;
            if (joints != null && weights != null && skeletonFactory != null)
                vertexFormat = VertexPropertyDataFormat.DeformerAttachments;
            else if (colors != null)
                vertexFormat = VertexPropertyDataFormat.ColorLight;
            else vertexFormat = VertexPropertyDataFormat.None;

            Vertex readNextVertex()
            {
                Vector3 position = positions.Read<Vector3>();
                positions?.Seek(positionStride, SeekOrigin.Current);

                Vector3 normal = normals?.Read<Vector3>() ?? default;
                normals?.Seek(normalStride, SeekOrigin.Current);

                Vector2 texcoord = texcoords?.Read<Vector2>() ?? default;
                texcoord.Y = 1 - texcoord.Y;
                texcoords?.Seek(texcoordsStride, SeekOrigin.Current);

                (uint, uint, uint, uint) joint = default;
                if (joints != null)
                {
                    joint = jointsFormat.Encoding switch
                    {
                        EncodingType.UNSIGNED_BYTE => 
                            joints.Read<byte, byte, byte, byte>(),
                        EncodingType.UNSIGNED_SHORT =>
                            joints.Read<ushort, ushort, ushort, ushort>(),
                        EncodingType.UNSIGNED_INT =>
                            joints.Read<uint, uint, uint, uint>(),
                        _ => default,
                    };
                }

                joints?.Seek(jointsStride, SeekOrigin.Current);

                (float, float, float, float) weight = weights?
                    .Read<float, float, float, float>() ?? default;
                weights?.Seek(weightsStride, SeekOrigin.Current);

                Color color = colors?.Read<Color>() ?? Color.Black;
                colors?.Seek(colorsStride, SeekOrigin.Current);

                VertexPropertyData vertexPropertyData;
                if (joints != null && weights != null)
                {
                    vertexPropertyData = VertexPropertyData
                        .CreateAsDeformerAttachment(joint, weight);
                }
                else if (colors != null)
                {
                    vertexPropertyData = VertexPropertyData
                        .CreateAsColorLight(color, Color.Black);
                }
                else vertexPropertyData = default;

                return new Vertex(position, normal, texcoord,
                    vertexPropertyData);
            }

            try
            {
                while (positions.Position < positions.Length)
                    vertices.Add(readNextVertex());
            }
            catch (Exception exc)
            {
                throw new FormatException("The mesh import failed after " +
                    $"vertex #{vertices.Count}.", exc);
            }
            finally
            {
                positions?.Dispose();
                normals?.Dispose();
                texcoords?.Dispose();
                colors?.Dispose();
                joints?.Dispose();
                weights?.Dispose();
            }

            // The import of the faces is much more straightforward, luckily.
            List<Face> faces = new List<Face>();
            foreach ((uint, uint, uint) face in
                meshPrimitive.GetTriangleIndices())
                faces.Add(new Face(face.Item1, face.Item2, face.Item3));

            if (vertexFormat == VertexPropertyDataFormat.DeformerAttachments)
                return MeshData.Create(vertices.ToArray(), faces.ToArray(),
                    skeletonFactory());
            else return MeshData.Create(vertices.ToArray(), faces.ToArray(), 
                vertexFormat);
        }

        static Skeleton ImportSkeleton(Node skinnedNode, 
            bool makeReadOnly = true)
        {
            if (skinnedNode == null)
                throw new ArgumentNullException(nameof(skinnedNode));
            if (skinnedNode.Skin == null)
                throw new ArgumentException("The specified node doesn't " +
                    "have a skin that could be imported as skeleton.");

            Skin skin = skinnedNode.Skin;

            // The visual root of the current skinned node usually contains 
            // both the skinned mesh node (= skinnedNode) and the 
            // root bone node(s) - so it can be used as root for the skeleton.
            Skeleton skeleton = new Skeleton(
                skinnedNode.VisualRoot.LocalTransform.Matrix);            

            Stack<(Node source, Node<Bone> targetParent)> conversionStack = 
                new Stack<(Node, Node<Bone>)>();

            Dictionary<Node, (Matrix4x4 offset, int boneIndex)> boneNodes =
                new Dictionary<Node, (Matrix4x4, int)>();

            for (int i = 0; i < skin.JointsCount; i++)
            {
                (Node boneNode, Matrix4x4 boneOffset) = skin.GetJoint(i);
                boneNodes[boneNode] = (boneOffset, i);

                // The boneNodeStack will be initialized with the root bone(s)
                // and the skeleton (root node) as target root.
                if (boneNode.VisualParent == boneNode.VisualRoot)
                    conversionStack.Push((boneNode, skeleton));
            }

            while (conversionStack.Count > 0)
            {
                (Node source, Node<Bone> targetParent) = conversionStack.Pop();

                if (boneNodes.TryGetValue(source, out var boneProperties))
                {
                    int boneIndex = boneProperties.boneIndex;
                    Matrix4x4 offset = boneProperties.offset;

                    if (boneIndex <= byte.MaxValue &&
                        boneIndex >= byte.MinValue)
                    {
                        // The parent "skeleton node" for the children in the 
                        // current GLTF node will either be the current 
                        // converted GTLF node or - if the node is invalid -
                        // the current parent "skeleton node".
                        targetParent = targetParent.AddChild(
                            new Bone(source.Name, (byte)boneIndex, offset));
                    }
                    else Log.Warning($"Bone node '{source.Name}' ignored as " +
                        "its bone index exceeds supported/valid range.",
                        nameof(GLTFormatHandler));
                }
                else Log.Warning($"Invalid node '{source.Name}' in source " +
                        "bone hierarchy found and ignored.",
                        nameof(GLTFormatHandler));

                // Enqueue the child bone nodes to continue the traversal at
                // a lower hierarchy level.
                foreach (var childBoneNode in source.VisualChildren)
                    conversionStack.Push((childBoneNode, targetParent));
            }

            if (makeReadOnly) return skeleton.ToReadOnly(false);
            else return skeleton;
        }

        static ReadContext CreateReadContext(ResourceManager manager,
            FileSystemPath contextRootDirectory)
        {
            if (!contextRootDirectory.IsAbsolute)
                throw new ArgumentException("The specified path isn't " +
                    "absolute and can't be used as context root.");
            if (!contextRootDirectory.IsDirectoryPath)
                throw new ArgumentException("The specified path isn't " +
                    "a valid directory path.");

            ArraySegment<byte> ReadFileSystemFile(string pathString)
            {
                FileSystemPath path;
                try { path = new FileSystemPath(pathString); }
                catch (ArgumentNullException) { throw; }
                catch (ArgumentException) { throw; }

                if (!path.IsAbsolute)
                    path = FileSystemPath.Combine(contextRootDirectory, path);

                MemoryStream memoryBuffer = new MemoryStream();
                try
                {
                    using Stream fileStream = manager.OpenFile(path);
                    fileStream.CopyTo(memoryBuffer);
                }
                catch (Exception exc)
                {
                    throw new IOException("The file couldn't be buffered.",
                        exc);
                }
                return new ArraySegment<byte>(memoryBuffer.ToArray());
            }

            return ReadContext.Create(ReadFileSystemFile);
        }
    }
}
