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
using SharpGLTF.Animations;
using SharpGLTF.IO;
using SharpGLTF.Runtime;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Eterra.Platforms.Windows.IO
{
    static class ParameterNames
    {
        /// <summary>
        /// Defines the name of a parameter of type <see cref="Matrix4x4"/>
        /// that specifies the global, absolute transformation.
        /// </summary>
        public const string GlobalTransformation = "GlobalTransformation";

        /// <summary>
        /// Defines the name of a parameter of type <see cref="Matrix4x4"/>
        /// that specifies the transformation, relatively to its parent.
        /// </summary>
        public const string LocalTransformation = "LocalTransformation";

        /// <summary>
        /// Defines the name of a parameter of type <see cref="bool"/> 
        /// that specifies whether an object is visible or not. 
        /// </summary>
        public const string Visible = "Visible";
    }

    public class Parameters : Dictionary<string, object> { }

    public class SceneHierarchy : Node<Parameters>
    {
        public SceneHierarchy() : base(new Parameters())
        {

        }
    }

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
                ReadContext readContext = CreateReadContext(manager);
                using Stream stream = readContext.OpenFile(path);
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

            Common.Scene scene = GenerateScene(root);
            if (typeof(T).IsAssignableFrom(scene.GetType()))
                return (T)(object)scene;
            else throw new ArgumentException("The loaded resource of " +
                $"type '{scene.GetType().Name}' couldn't be converted to " +
                $"the requested type '{typeof(T).Name}'.");
        }

        private static Common.Scene GenerateScene(ModelRoot root)
        {
            Dictionary<Mesh, MeshData> importedMeshes = 
                new Dictionary<Mesh, MeshData>();

            foreach (var node in root.LogicalNodes)
            {
                if (node.Mesh != null)
                    ImportMesh(node, importedMeshes);
            }

            //root.LogicalAnimations.First().FindTranslationSampler()
            throw new NotImplementedException();
        }

        private static TimelineLayer ImportTimelineLayer(Node node, 
            SharpGLTF.Schema2.Animation sourceAnimation)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (sourceAnimation == null)
                throw new ArgumentNullException(nameof(sourceAnimation));

            List<TimelineChannel> timelineChannels = 
                new List<TimelineChannel>();

            IAnimationSampler<Vector3> translationSampler = 
                sourceAnimation.FindTranslationSampler(node);
            if (translationSampler != null)
            {
                timelineChannels.Add(ImportTimelineChannel(translationSampler,
                    ChannelIdentifier.Position));
            }

            IAnimationSampler<Vector3> scaleSampler =
                sourceAnimation.FindScaleSampler(node);
            if (scaleSampler != null)
            {
                timelineChannels.Add(ImportTimelineChannel(scaleSampler,
                    ChannelIdentifier.Scale));
            }

            IAnimationSampler<Quaternion> rotationSampler =
                sourceAnimation.FindRotationSampler(node);
            if (rotationSampler != null)
            {
                timelineChannels.Add(ImportTimelineChannel(rotationSampler,
                    ChannelIdentifier.Rotation));
            }

            return new TimelineLayer(node.Name, timelineChannels);
        }

        private static TimelineChannel<T> ImportTimelineChannel<T>(
            IAnimationSampler<T> sourceAnimationSampler, 
            ChannelIdentifier channelIdentifier)
            where T : unmanaged
        {
            if (sourceAnimationSampler == null)
                throw new ArgumentNullException(
                    nameof(sourceAnimationSampler));
            if (channelIdentifier == null)
                throw new ArgumentNullException(nameof(channelIdentifier));

            InterpolationMethod interpolationMethod = InterpolationMethod.None;

            List<Keyframe<T>> keyframes = new List<Keyframe<T>>();

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

                for (float p = begin; p <= end;
                    p += CubicSplineInterpolationRasterisationFrequency)
                {
                    T keyframeValue = curveSampler.GetPoint(p);
                    keyframes.Add(new Keyframe<T>(p, keyframeValue));
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

                foreach (var (position, value) in
                    sourceAnimationSampler.GetLinearKeys())
                {
                    keyframes.Add(new Keyframe<T>(position, value));
                }
            }

            try
            {
                return new TimelineChannel<T>(channelIdentifier,
                    interpolationMethod, keyframes);
            }
            catch (ArgumentException) { throw; }
        }

        private static MeshData ImportMesh(Node meshNode, 
            Dictionary<Mesh, MeshData> importedMeshCache = null)
        {
            if (meshNode == null)
                throw new ArgumentNullException(nameof(meshNode));
            if (meshNode.Mesh == null)
                throw new ArgumentException("The specified node doesn't " +
                    "have a mesh that could be imported.");

            if (meshNode.Mesh.Primitives.Count == 0)
                throw new ArgumentException("The specified node contains an " +
                    "empty mesh (mesh without primitives) that can't " +
                    "be imported.");
            if (meshNode.Mesh.Primitives.Count > 1)
                Log.Warning($"The mesh node '{meshNode.Name}' contains more " +
                    "than one mesh primitive - only the first mesh " +
                    "primitive will be imported, the others will be ignored.",
                        nameof(GLTFormatHandler));

            MeshPrimitive meshPrimitive = meshNode.Mesh.Primitives[0];

            if (meshPrimitive.DrawPrimitiveType != PrimitiveType.TRIANGLES)
                throw new ArgumentException("The mesh of node " +
                    $"'{meshNode.Name}' has an unsupported draw primitive " +
                    $"type (only {nameof(PrimitiveType.TRIANGLES)} is " +
                    "supported.");

            // To prevent the same logical mesh being loaded more than once,
            // a dictionary which contains previously imported meshes can
            // be specified. If the dictionary contains the mesh of the
            // current node already, its converted MeshData variant 
            // is returned.
            if (importedMeshCache != null)
            {
                if (importedMeshCache.TryGetValue(meshNode.Mesh,
                    out MeshData mesh)) return mesh;
            }

            // For the import of the vertex data, a memory stream "wrapper" 
            // is created around every vertex attribute channel, then we'll 
            // "read" each stream simultaneously and assemble the data 
            // into Vertex instances.
            List<Vertex> vertices = new List<Vertex>();

            MemoryStream getVertexData(string attributeName, out int stride)
            {
                Accessor accessor = meshPrimitive.GetVertexAccessor(
                    attributeName);
                if (accessor != null)
                {
                    stride = accessor.SourceBufferView.ByteStride;
                    return new MemoryStream(
                        accessor.SourceBufferView.Content.Array,
                        accessor.SourceBufferView.Content.Offset,
                        accessor.SourceBufferView.Content.Count, false);
                }
                else
                {
                    stride = 0;
                    return null;
                }
            }

            MemoryStream positions = getVertexData(VertexAttributePosition,
                out int positionStride);
            if (positions == null)
                throw new ArgumentException("The specified node contains a " +
                    "mesh with a primitive, but without vertex positions, " +
                    "which is invalid and can't be imported.");

            MemoryStream normals = getVertexData(VertexAttributeNormal,
                out int normalStride);
            MemoryStream texcoords = getVertexData(VertexAttributeTexcoord,
                out int texcoordsStride);

            MemoryStream joints = getVertexData(VertexAttributeJoints,
                out int jointsStride);
            MemoryStream weights = getVertexData(VertexAttributeWeights,
                out int weightsStride);
            MemoryStream colors = getVertexData(VertexAttributeColor,
                out int colorsStride);

            // Currently, the vertex property data can either store bone
            // attachments (joints and weights) or colors - but not both.
            // As the former is usually more important, the color channel is
            // ignored in such cases.
            if (joints != null && weights != null && colors != null)
            {
                Log.Warning($"The mesh node '{meshNode.Name}' contains " +
                    "vertices with both bone attachments and a color, which " +
                    "is currently not supported - the color channel is " +
                    "ignored in that case.", nameof(GLTFormatHandler));
                colors.Dispose();
                colors = null;
            }

            VertexPropertyDataFormat vertexFormat;
            if (joints != null && weights != null)
                vertexFormat = VertexPropertyDataFormat.DeformerAttachments;
            else if (colors != null)
                vertexFormat = VertexPropertyDataFormat.ColorLight;
            else vertexFormat = VertexPropertyDataFormat.None;

            try
            {
                while (positions.Position < positions.Length)
                {
                    Vector3 position = positions.Read<Vector3>();
                    positions?.Seek(positionStride, SeekOrigin.Current);

                    Vector3 normal = normals?.Read<Vector3>() ?? default;
                    normals?.Seek(normalStride, SeekOrigin.Current);

                    Vector2 texcoord = texcoords?.Read<Vector2>() ?? default;
                    texcoords?.Seek(texcoordsStride, SeekOrigin.Current);

                    (ushort, ushort, ushort, ushort) joint = joints?
                        .Read<ushort, ushort, ushort, ushort>() ?? default;
                    joints?.Seek(jointsStride, SeekOrigin.Current);

                    (float, float, float, float) weight = weights?
                        .Read<float, float, float, float>() ?? default;
                    weights?.Seek(weightsStride, SeekOrigin.Current);

                    Color color = colors?.Read<Color>() ?? Color.Black;
                    colors?.Seek(colorsStride, SeekOrigin.Current);

                    VertexPropertyData vertexPropertyData;
                    if (joints != null && weights != null)
                        vertexPropertyData = VertexPropertyData
                            .CreateAsDeformerAttachment(joint, weight);
                    else if (colors != null)
                        vertexPropertyData = VertexPropertyData
                            .CreateAsColorLight(color, Color.Black);
                    else vertexPropertyData = default;

                    vertices.Add(new Vertex(position, normal, texcoord,
                        vertexPropertyData));
                }
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

            MeshData meshData;
            if (vertexFormat == VertexPropertyDataFormat.DeformerAttachments)
                meshData = MeshData.Create(vertices.ToArray(),
                    faces.ToArray(), ImportSkeleton(meshNode));
            else meshData = MeshData.Create(vertices.ToArray(),
                    faces.ToArray(), vertexFormat);

            if (importedMeshCache != null)
                importedMeshCache[meshNode.Mesh] = meshData;

            return meshData;
        }

        private static Skeleton ImportSkeleton(Node skinnedNode, 
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

        private static ReadContext CreateReadContext(ResourceManager manager)
        {
            ArraySegment<byte> ReadFileSystemFile(string pathString)
            {
                FileSystemPath path;
                try { path = new FileSystemPath(pathString); }
                catch (ArgumentNullException) { throw; }
                catch (ArgumentException) { throw; }

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
