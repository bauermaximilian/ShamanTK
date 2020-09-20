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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Numerics;
using static Assimp.Metadata;

namespace Eterra.Platforms.Windows.IO
{
    class AssimpFormatHandler : IResourceFormatHandler
    {
        private class AssimpIOStream : Assimp.IOStream
        {
            public override bool IsValid => baseStream != null;

            private readonly Stream baseStream;

            public AssimpIOStream(IFileSystem fileSystem, string pathString)
                : base(pathString, Assimp.FileIOMode.Read)
            {
                if (fileSystem == null)
                    throw new ArgumentNullException(nameof(fileSystem));
                if (pathString == null)
                    throw new ArgumentNullException(nameof(pathString));

                FileSystemPath path = new FileSystemPath(pathString,
                    false);
                if (!path.IsAbsolute)
                    path = path.ToAbsolute(FileSystemPath.Root);
                baseStream = fileSystem.OpenFile(path, false);
            }

            public override void Flush()
            {
                baseStream.Flush();
            }

            public override long GetFileSize()
            {
                return baseStream.Length;
            }

            public override long GetPosition()
            {
                return baseStream.Position;
            }

            public override long Read(byte[] dataRead, long count)
            {
                return baseStream.Read(dataRead, 0, (int)count);
            }

            public override Assimp.ReturnCode Seek(long offset, 
                Assimp.Origin seekOrigin)
            {
                var convertedSeekOrigin = seekOrigin switch
                {
                    Assimp.Origin.Current => SeekOrigin.Current,
                    Assimp.Origin.Set => SeekOrigin.Begin,
                    Assimp.Origin.End => SeekOrigin.End,
                    _ => throw new ArgumentException("The seek origin " +
                    "is invalid."),
                };

                try
                {
                    baseStream.Seek(offset, convertedSeekOrigin);
                    return Assimp.ReturnCode.Success;
                }
                catch
                {
                    return Assimp.ReturnCode.Failure;
                }
            }

            public override long Write(byte[] dataToWrite, long count)
            {
                throw new NotSupportedException();
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing && IsValid) baseStream.Dispose();
            }
        }

        private class AssimpIOSystem : Assimp.IOSystem
        {
            private readonly IFileSystem fileSystem;

            public AssimpIOSystem(IFileSystem fileSystem) : base()
            {
                this.fileSystem = fileSystem ??
                    throw new ArgumentNullException(nameof(fileSystem));
            }

            public override Assimp.IOStream OpenFile(string pathToFile,
                Assimp.FileIOMode fileMode)
            {
                return new AssimpIOStream(fileSystem, pathToFile);
            }
        }

        public const string QueryKeyModel = "model";
        public const string QueryKeyModelIndex = "modelIndex";
        public const string QueryKeyMesh = "mesh";
        public const string QueryKeyAnimationTimeline = "animation";

        private readonly Assimp.AssimpContext assimp;
        private readonly Exception assimpInitException;
        private readonly object assimpLock = new object();

        public AssimpFormatHandler()
        {
            try { assimp = new Assimp.AssimpContext(); }
            catch (Exception exc) { assimpInitException = exc; }
        }

        public bool SupportsExport(string fileExtensionLowercase)
        {
            return false;
        }

        public bool SupportsImport(string fileExtensionLowercase)
        {
            if (assimp == null) return false;
            else return assimp.IsImportFormatSupported("." + 
                fileExtensionLowercase);
        }

        public void Export(object resource, ResourceManager manager, 
            ResourcePath path, bool overwrite)
        {
            throw new NotSupportedException("Exporting is not supported " +
                "by this format handler yet.");
        }

        /// <remarks>
        /// The available query parameters for <see cref="ResourcePath.Query"/>
        /// and their function to select what should be imported from an 
        /// <see cref="Assimp.Scene"/> are defined by
        /// <see cref="QueryKeyModel"/>, <see cref="QueryKeyMesh"/> and
        /// <see cref="QueryKeyAnimationTimeline"/>.
        /// If a model is requested and this model contains more than one mesh,
        /// the <see cref="QueryKeyModelIndex"/> parameter can be used to 
        /// specify which of the meshes should be loaded (this index parameter
        /// is ignored otherwise). By default, the first mesh will be used.
        /// </remarks>
        public T Import<T>(ResourceManager manager, ResourcePath path)
        {
            if (!SupportsImport(path.Path.GetFileExtension()))
            {
                if (assimpInitException != null)
                    throw new NotSupportedException("The current platform " +
                        "doesn't support ASSIMP or the unmanaged libraries " +
                        "weren't found.");
                else throw new NotSupportedException("The specified file " +
                    "format was not supported.");
            }
            //TODO: Add correct behaviour in case of disposed file system
            lock (assimpLock)
            {
                assimp.SetIOSystem(new AssimpIOSystem(manager.FileSystem));
                try { return ImportAssimpObject<T>(manager, path); }
                finally { assimp.RemoveIOSystem(); }
            }
        }

        private T ImportAssimpObject<T>(ResourceManager manager, 
            ResourcePath path)
        {
            NameValueCollection queryParameters =
                path.Query.ToNameValueCollection();

            string meshQuery = (queryParameters[QueryKeyMesh] ?? "").Trim();
            string animationQuery = (queryParameters[QueryKeyAnimationTimeline]
                ?? "").Trim();

            bool meshQuerySpecified = meshQuery.Length > 0;
            bool animationQuerySpecified = animationQuery.Length > 0;

            Type t = typeof(T);
            bool meshDataRequested = t == typeof(MeshData);
            //bool timelineRequested = t == typeof(Timeline);
            bool sceneRequested = t == typeof(Scene);

            if ((meshQuerySpecified ? 1 : 0) +
                (animationQuerySpecified ? 1 : 0) > 1)
                throw new ArgumentException("More than one query " +
                    "parameter was specified - only one may be used " +
                    "in a query at the same time.");

            Assimp.Scene scene = ImportScene(path.Path);

            if ((meshDataRequested && meshQuerySpecified)
                || meshDataRequested)
                return (T)(object)ExtractMesh(scene, meshQuery, true);
            else if (sceneRequested)
                return (T)(object)ConvertScene(scene, path.Path, manager);
            else throw new FileNotFoundException("The specified path " +
                "couldn't be resolved into a resource of the requested " +
                "type.");
        }

        private Scene ConvertScene(Assimp.Scene assimpScene,
            FileSystemPath sceneFilePath, ResourceManager resourceManager)
        {
            static byte ColorFloatToByte(float colorComponent)
            {
                return (byte)(colorComponent * byte.MaxValue);
            }

            static Color ConvertAssimpColor4D(Assimp.Color4D color)
            {
                return new Color(ColorFloatToByte(color.R),
                    ColorFloatToByte(color.G),
                    ColorFloatToByte(color.B),
                    ColorFloatToByte(color.A));
            }

            static Color ConvertAssimpLightColor3D(Assimp.Color3D color)
            {
                return new Color(ColorFloatToByte(color.R / 10.0f),
                    ColorFloatToByte(color.G / 10.0f),
                    ColorFloatToByte(color.B / 10.0f));
            }

            Scene scene = new Scene();

            Dictionary<string, Light> lights = new Dictionary<string, Light>();
            foreach (Assimp.Light light in assimpScene.Lights)
            {
                string name = light.Name;

                Color color = ConvertAssimpLightColor3D(light.ColorDiffuse);
                Vector3 direction = new Vector3(light.Direction.X,
                    light.Direction.Y, light.Direction.Z);
                Vector3 position = new Vector3(light.Position.X,
                    light.Position.Y, light.Position.Z);

                float radius = 0;

                Assimp.Node lightNode = assimpScene.RootNode.FindNode(name);
                if (lightNode != null && 
                    lightNode.Metadata.TryGetValue("LightRadius", 
                    out Entry radiusValue))
                {
                    double? radiusValueAsDouble = radiusValue.DataAs<double>();
                    float? radiusValueAsFloat = radiusValue.DataAs<float>();
                    int? radiusValueAsInt = radiusValue.DataAs<int>();

                    if (radiusValueAsDouble.HasValue)
                        radius = (float)radiusValueAsDouble.Value;
                    else if (radiusValueAsFloat.HasValue)
                        radius = radiusValueAsFloat.Value;
                    else if (radiusValueAsInt.HasValue)
                        radius = radiusValueAsInt.Value;
                }

                if (radius <= 0 && light.AttenuationLinear > float.Epsilon)
                    radius = 2 / light.AttenuationLinear;
                else if (radius <= 0 && 
                    light.AttenuationQuadratic > float.Epsilon)
                    radius = (float)Math.Sqrt(2 / light.AttenuationQuadratic);

                if (light.LightType == Assimp.LightSourceType.Directional)
                    lights.Add(name, new Light(color, direction));
                else if (light.LightType == Assimp.LightSourceType.Point)
                    lights.Add(name, new Light(color, position, radius));
                else if (light.LightType == Assimp.LightSourceType.Spot)
                    lights.Add(name, new Light(color, position, radius,
                        direction, light.AngleInnerCone,
                        light.AngleOuterCone - light.AngleInnerCone));
            }

            Dictionary<string, Timeline> animations =
                ExtractAnimations(assimpScene);

            bool LoadTextureData(string filePath, Entity targetEntity,
                EntityParameter targetEntityParameter, bool throwOnError)
            {
                if (string.IsNullOrWhiteSpace(filePath)) return false;

                ResourcePath resourcePath;
                try
                {
                    resourcePath = new ResourcePath(filePath);
                    if (!resourcePath.IsAbsolute)
                        resourcePath = resourcePath.ToAbsolute(sceneFilePath);
                }
                catch (Exception exc)
                {
                    if (throwOnError)
                        throw new Exception("The file path is invalid.", exc);
                    else return false;
                }

                try
                {
                    TextureData textureData =
                        resourceManager.ImportResourceFile<TextureData>(
                            resourcePath);

                    targetEntity.Set(targetEntityParameter, textureData);

                    return true;
                }
                catch (Exception exc)
                {
                    if (throwOnError)
                        throw new Exception("The texture data couldn't " +
                            "be loaded.", exc);
                    else return false;
                }
            }

            void ApplyAssimpMetadataToEntity(Assimp.Node node, 
                Entity entity)
            {
                foreach (var metadata in node.Metadata)
                {
                    string id = metadata.Key;
                    string valueStr = metadata.Value.Data as string;

                    if (id == EntityParameter.IsVisible.ToString())
                    {
                        try
                        {
                            entity.Set(EntityParameter.IsVisible,
                                bool.Parse(valueStr));
                        }
                        catch (Exception exc)
                        {
                            Log.Trace("Invalid 'IsVisible' " +
                                "parameter encountered and ignored. " +
                                "Error: " + exc.Message);
                        }
                    }
                    else if (id ==
                        EntityParameter.ColliderPrimitive.ToString())
                    {
                        try
                        {
                            entity.Set(EntityParameter.ColliderPrimitive,
                                ColliderPrimitive.Parse(valueStr));
                        }
                        catch (Exception exc)
                        {
                            //TODO: Create scene conversion report!
                            Log.Trace("Invalid 'ColliderPrimitive' " +
                                "parameter encountered and ignored. " +
                                "Error: " + exc.Message);
                        }
                    }
                    else if (id == EntityParameter.ColliderMeshData.ToString())
                    {
                        try
                        {
                            entity.Set(EntityParameter.ColliderMeshData,
                                ExtractMesh(assimpScene, valueStr, false));
                        }
                        catch (Exception exc)
                        {
                            Log.Trace("Invalid 'ColliderMeshData' " +
                                "parameter encountered and ignored. " +
                                "Error: " + exc.Message);
                        }
                    }
                    else if (id == EntityParameter.Dynamics.ToString())
                    {
                        try
                        {
                            entity.Set(EntityParameter.Dynamics,
                                Dynamics.Parse(valueStr));
                        }
                        catch (Exception exc)
                        {
                            Log.Trace("Invalid 'Dynamics' " +
                                "parameter encountered and ignored. " +
                                "Error: " + exc.Message);
                        }
                    }
                    else if (id ==
                        EntityParameter.ModelAnimationTimeline.ToString())
                    {
                        if (!animations.TryGetValue(valueStr,
                            out Timeline timeline))
                        {
                            foreach (string candidateName in animations.Keys)
                            {
                                if (candidateName.ToLowerInvariant().Contains(
                                    valueStr.ToLowerInvariant()))
                                {
                                    timeline = animations[candidateName];
                                    break;
                                }
                            }
                        }

                        if (timeline != null)
                            entity.Set(
                                EntityParameter.ModelAnimationTimeline,
                                timeline);
                        else Log.Trace("Invalid 'ModelAnimationTimeline' " +
                            "parameter encountered and ignored. " +
                            "Error: Animation name not found.");
                    }
                    else if (id ==
                      EntityParameter.TextureDataEffect01.ToString())
                    {
                        try
                        {
                            LoadTextureData(valueStr, entity,
                                EntityParameter.TextureDataEffect01,
                                true);
                        }
                        catch (Exception exc)
                        {
                            Log.Trace("Invalid 'TextureDataEffect01' " +
                            "parameter encountered and ignored. " +
                            "Error: " + exc.Message);
                        }
                    }
                    else if (id ==
                      EntityParameter.TextureDataEffect02.ToString())
                    {
                        try
                        {
                            LoadTextureData(valueStr, entity,
                                EntityParameter.TextureDataEffect02,
                                true);
                        }
                        catch (Exception exc)
                        {
                            Log.Trace("Invalid 'TextureDataEffect02' " +
                            "parameter encountered and ignored. " +
                            "Error: " + exc.Message);
                        }
                    }
                    else if (id ==
                      EntityParameter.TextureDataEffect03.ToString())
                    {
                        try
                        {
                            LoadTextureData(valueStr, entity,
                                EntityParameter.TextureDataEffect03,
                                true);
                        }
                        catch (Exception exc)
                        {
                            Log.Trace("Invalid 'TextureDataEffect03' " +
                            "parameter encountered and ignored. " +
                            "Error: " + exc.Message);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            if (!string.IsNullOrWhiteSpace(valueStr))
                                entity.Set(id, valueStr);
                            else
                            {
                                bool? valueBool =
                                    metadata.Value.DataAs<bool>();
                                int? valueInt =
                                    metadata.Value.DataAs<int>();
                                float? valueFloat =
                                    metadata.Value.DataAs<float>();
                                double? valueDouble =
                                    metadata.Value.DataAs<double>();

                                if (valueBool.HasValue)
                                    entity.Set(id, valueBool.Value);
                                else if (valueInt.HasValue)
                                    entity.Set(id, valueInt.Value);
                                else if (valueFloat.HasValue)
                                    entity.Set(id, valueFloat.Value);
                                else if (valueDouble.HasValue)
                                    entity.Set(id, valueDouble.Value);
                            }
                        }
                    }
                }

                /*
                //This should find the animation for the current node by
                //just checking for animations where the animation name 
                //contains the name of the current node.
                //TODO: Find better solution for this, this is kinda hacky
                //and only works in the ideal case. For now, the target 
                //animation needs to be specified as parameter. Sorry :C
                if (!entity.Contains(
                    EntityParameter.ModelAnimationTimeline))
                {
                    string animationName = node.Name;
                    if (!animations.TryGetValue(animationName,
                        out Timeline timeline))
                    {
                        foreach (string candidateName in animations.Keys)
                        {
                            if (candidateName.ToLowerInvariant().Contains(
                                animationName.ToLowerInvariant()))
                            {
                                timeline = animations[candidateName];
                                break;
                            }
                        }
                    }

                    if (timeline != null)
                        entity.Set(EntityParameter.ModelAnimationTimeline,
                            timeline);
                }*/
            }

            void CopyEntityRecursive(Assimp.Node node, Assimp.Matrix4x4 parent)
            {
                Entity entity = null;

                Assimp.Matrix4x4 absoluteTransform = node.Transform * parent;
                absoluteTransform.Decompose(out Assimp.Vector3D s,
                    out Assimp.Quaternion r, out Assimp.Vector3D t);

                foreach (int meshIndex in node.MeshIndices)
                {
                    Assimp.Mesh assimpMesh = assimpScene.Meshes[meshIndex];
                    Assimp.Material assimpMaterial =
                        assimpScene.Materials[assimpMesh.MaterialIndex];

                    entity = scene.Add();

                    entity.Name = node.Name;
                    entity.Position = new Vector3(t.X, t.Y, t.Z);
                    entity.Scale = new Vector3(s.X, s.Y, s.Z);
                    entity.Rotation = new Quaternion(r.X, r.Y, r.Z, r.W);

                    entity.Set(EntityParameter.MeshData, ExtractMesh(
                        assimpMesh, assimpScene));

                    entity.Set(EntityParameter.Color,
                        ConvertAssimpColor4D(assimpMaterial.ColorDiffuse));
                    LoadTextureData(assimpMaterial.TextureDiffuse.FilePath,
                        entity, EntityParameter.TextureDataMain, true);
                    LoadTextureData(assimpMaterial.TextureSpecular.FilePath,
                        entity, EntityParameter.TextureDataEffect01, false);
                    LoadTextureData(assimpMaterial.TextureNormal.FilePath,
                        entity, EntityParameter.TextureDataEffect02, false);
                    LoadTextureData(assimpMaterial.TextureEmissive.FilePath,
                        entity, EntityParameter.TextureDataEffect03, false);

                    ApplyAssimpMetadataToEntity(node, entity);
                }

                if (entity == null)
                {
                    entity = scene.Add();
                    entity.Name = node.Name;
                    entity.Position = new Vector3(t.X, t.Y, t.Z);
                    entity.Scale = new Vector3(s.X, s.Y, s.Z);
                    entity.Rotation = new Quaternion(r.X, r.Y, r.Z, r.W);
                    ApplyAssimpMetadataToEntity(node, entity);
                }

                if (node.Name != null && lights.TryGetValue(node.Name,
                    out Light light))
                    entity.Set(EntityParameter.Light,
                        light.Moved(entity.Position));

                foreach (Assimp.Node childNode in node.Children)
                    CopyEntityRecursive(childNode, absoluteTransform);
            }

            CopyEntityRecursive(assimpScene.RootNode, 
                Assimp.Matrix4x4.Identity);

            return scene;
        }        

        private Assimp.Node FindNodeWithMesh(Assimp.Node node)
        {
            if (node.HasMeshes) return node;
            else 
            {
                if (node.HasChildren)
                {
                    foreach (Assimp.Node childNode in node.Children)
                    {
                        Assimp.Node meshNodeCandidate = 
                            FindNodeWithMesh(childNode);
                        if (meshNodeCandidate != null)
                            return meshNodeCandidate;
                    }                    
                }
                return null;
            }
        }

        private MeshData ExtractMesh(Assimp.Scene scene, string meshName,
            bool caseInsensitive)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));
            if (meshName == null)
                throw new ArgumentNullException(nameof(meshName));

            if (caseInsensitive) meshName = meshName.ToLowerInvariant().Trim();

            foreach (Assimp.Mesh mesh in scene.Meshes)
            {
                string meshCandidateName = mesh.Name.Trim();
                if (caseInsensitive)
                    meshCandidateName = meshCandidateName.ToLowerInvariant();

                //Covers the case "retrieve the first possible mesh"
                if (meshName == meshCandidateName ||
                    meshCandidateName.Length == 0) 
                    return ExtractMesh(mesh, scene);
            }

            throw new FileNotFoundException("No mesh with the specified " +
                "name was found in the imported file.");
        }

        private MeshData ExtractMesh(Assimp.Mesh mesh, Assimp.Scene scene)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));

            Vertex[] vertices = ExtractVertices(mesh);
            Face[] faces = ExtractFaces(mesh);
            Skeleton skeleton = ExtractSkeleton(mesh, scene, false)
                .ToReadOnly(false);

            return MeshData.Create(vertices, faces, skeleton);
        }

        private Vertex[] ExtractVertices(Assimp.Mesh mesh)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            //Retrieve the bones from the assimp mesh and store them so that 
            //they can be accessed via the vertex index they are bound to
            List<Tuple<byte, byte>>[] vertexDeformerAttachments = new
                List<Tuple<byte, byte>>[mesh.Vertices.Count];
            for (int i = 0; i < vertexDeformerAttachments.Length; i++)
                vertexDeformerAttachments[i] = new List<Tuple<byte, byte>>();

            for (int i = 0; i < mesh.Bones.Count; i++)
            {
                Assimp.Bone bone = mesh.Bones[i];
                foreach (Assimp.VertexWeight weight in bone.VertexWeights)
                {
                    if (weight.VertexID >= vertexDeformerAttachments.Length)
                        throw new FormatException("A bone referenced a " +
                            "vertex ID which exceeded the bounds of the " +
                            "available vertices.");

                    vertexDeformerAttachments[weight.VertexID].Add(
                        new Tuple<byte, byte>((byte)i, 
                        (byte)(weight.Weight * byte.MaxValue)));
                }
            }

            //Initialize and populate the vertex array by using the vertex,
            //normal and texture coordinate data from the mesh and the
            //previously stored bone/deformer attachments.
            Vertex[] vertices = new Vertex[mesh.Vertices.Count];

            for (int i = 0; i < vertices.Length; i++)
            {
                Assimp.Vector3D position,
                    normal = new Assimp.Vector3D(0, 0, 1),
                    textureCoordinate = new Assimp.Vector3D(0, 0, 0);

                position = mesh.Vertices[i];
                //The following statements cover the possibility of the mesh
                //having no normals/texture coordinates or just not enough
                //for all vertices - Assimp docs say the latter case shouldn't 
                //occur, but I have severe trust issues.
                if (i < mesh.Normals.Count) normal = mesh.Normals[i];
                if (mesh.HasTextureCoords(0) &&
                    i < mesh.TextureCoordinateChannels[0].Count)
                    textureCoordinate = mesh.TextureCoordinateChannels[0][i];

                //Convert the list of attachment tuples from the previously 
                //generated array into a DeformerAttachments instance for the
                //current vertex.
                VertexPropertyData deformerAttachment = 
                    VertexPropertyData.CreateAsDeformerAttachment(
                        vertexDeformerAttachments[i], true);

                //Compose all previously collected properties to a new vertex.
                vertices[i] = new Vertex(new Vector3(position.X, position.Y,
                    position.Z), new Vector3(normal.X, normal.Y, normal.Z),
                    new Vector2(textureCoordinate.X, textureCoordinate.Y),
                    deformerAttachment);
            }

            return vertices;
        }

        private Face[] ExtractFaces(Assimp.Mesh mesh)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            Face[] faces = new Face[mesh.Faces.Count];

            for (int i=0; i<faces.Length; i++)
            {
                Assimp.Face face = mesh.Faces[i];
                //To prevent the following exception, the scene must be 
                //triangulated either when it was exported from the 3D program
                //or during import by Assimp (*hihi* you said "ass"... )
                if (face.IndexCount != 3)
                    throw new FormatException("The mesh had an invalid " +
                        "or unsupported face index count - a face must have " +
                        "exactly 3 vertex indicies.");

                faces[i] = new Face((uint)face.Indices[0],
                    (uint)face.Indices[2], (uint)face.Indices[1]);
            }

            return faces;
        }

        private Dictionary<string, Timeline> ExtractAnimations(
            Assimp.Scene scene)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));

            Dictionary<string, Timeline> timelines = 
                new Dictionary<string, Timeline>();

            static TimeSpan MaxTimeSpan(params TimeSpan[] timeSpans)
            {
                TimeSpan val = timeSpans[0];
                for (int i = 1; i < timeSpans.Length; i++)
                    if (timeSpans[i] > val) val = timeSpans[i];
                return val;
            }

            foreach (Assimp.Animation animation in scene.Animations)
            {
                //The key of the dictionary is the name of the animated 
                //node/bone, which is amended by a suffix later. This is kinda 
                //"hacky", but it should do the job.
                Dictionary<string, List<Keyframe<Vector3>>> positionLayers =
                    new Dictionary<string, List<Keyframe<Vector3>>>();
                Dictionary<string, List<Keyframe<Vector3>>> scaleLayers =
                    new Dictionary<string, List<Keyframe<Vector3>>>();
                Dictionary<string, List<Keyframe<Quaternion>>> rotationLayers =
                    new Dictionary<string, List<Keyframe<Quaternion>>>();

                double ticksPerSecond = animation.TicksPerSecond;
                if (ticksPerSecond == 0) ticksPerSecond = 25;

                TimeSpan CreateTimeSpan(double ticks)
                {
                    return TimeSpan.FromSeconds(ticks / ticksPerSecond);
                }

                foreach (Assimp.NodeAnimationChannel channel in
                    animation.NodeAnimationChannels)
                {
                    TimeSpan firstPositionFramePosition =
                        channel.HasPositionKeys ?
                            CreateTimeSpan(channel.PositionKeys[0].Time)
                            : TimeSpan.Zero;
                    TimeSpan firstScaleFramePosition =
                        channel.HasScalingKeys ?
                            CreateTimeSpan(channel.ScalingKeys[0].Time)
                            : TimeSpan.Zero;
                    TimeSpan firstRotationFramePosition =
                        channel.HasRotationKeys ?
                            CreateTimeSpan(channel.RotationKeys[0].Time)
                            : TimeSpan.Zero;

                    void TransferVector3Keyframes(IList<Assimp.VectorKey> src,
                        List<Keyframe<Vector3>> target,
                        ref TimeSpan lastKeyframePosition)
                    {
                        foreach (Assimp.VectorKey key in src)
                        {
                            TimeSpan keyframePosition = 
                                CreateTimeSpan(key.Time);

                            target.Add(new Keyframe<Vector3>(
                                keyframePosition,
                                new Vector3(key.Value.X, key.Value.Y,
                                key.Value.Z)));

                            lastKeyframePosition = MaxTimeSpan(
                                lastKeyframePosition, keyframePosition);
                        }
                    }

                    void TransferQuaternionKeyframes(
                        IList<Assimp.QuaternionKey> src,
                        List<Keyframe<Quaternion>> target,
                        ref TimeSpan lastKeyframePosition)
                    {
                        foreach (Assimp.QuaternionKey key in src)
                        {
                            TimeSpan keyframePosition =
                                CreateTimeSpan(key.Time);

                            target.Add(new Keyframe<Quaternion>(
                                keyframePosition,
                                new Quaternion(key.Value.X, key.Value.Y,
                                key.Value.Z, key.Value.W)));

                            lastKeyframePosition = MaxTimeSpan(
                                lastKeyframePosition, keyframePosition);
                        }
                    }

                    //The NodeName is used instead of the animation name, as
                    //the NodeName defines the name of the current element.
                    if (!positionLayers.TryGetValue(channel.NodeName,
                        out List<Keyframe<Vector3>> positionKeyframes))
                        positionKeyframes = positionLayers[channel.NodeName] =
                            new List<Keyframe<Vector3>>();
                    if (!scaleLayers.TryGetValue(channel.NodeName,
                        out List<Keyframe<Vector3>> scaleKeyframes))
                        scaleKeyframes = scaleLayers[channel.NodeName] =
                            new List<Keyframe<Vector3>>();
                    if (!rotationLayers.TryGetValue(channel.NodeName,
                        out List<Keyframe<Quaternion>> rotationKeyframes))
                        rotationKeyframes = rotationLayers[channel.NodeName] =
                            new List<Keyframe<Quaternion>>();

                    TimeSpan animationEnd = TimeSpan.Zero;
                    TransferVector3Keyframes(channel.PositionKeys,
                        positionKeyframes, ref animationEnd);
                    TransferVector3Keyframes(channel.ScalingKeys,
                        scaleKeyframes, ref animationEnd);
                    TransferQuaternionKeyframes(channel.RotationKeys,
                        rotationKeyframes, ref animationEnd);
                }

                List<TimelineLayer> layers = new List<TimelineLayer>();

                //The keys for positionLayers, scaleLayers and rotationLayers 
                //are identical - therefore, it could also be iterated through 
                //the keys of the scale-/rotationLayers with the same results.
                foreach (string timelineName in positionLayers.Keys)
                {
                    List<TimelineParameter> channels = 
                        new List<TimelineParameter>();
                    channels.Add(new TimelineParameter<Vector3>(
                        ParameterIdentifier.Position, InterpolationMethod.Linear,
                        positionLayers[timelineName]));
                    channels.Add(new TimelineParameter<Vector3>(
                        ParameterIdentifier.Scale, InterpolationMethod.Linear,
                        scaleLayers[timelineName]));
                    channels.Add(new TimelineParameter<Quaternion>(
                        ParameterIdentifier.Rotation, InterpolationMethod.Linear,
                        rotationLayers[timelineName]));

                    layers.Add(new TimelineLayer(timelineName, channels));
                }

                timelines.Add(animation.Name, new Timeline(layers));
            }

            return timelines;
        }

        private Skeleton ExtractSkeleton(Assimp.Mesh mesh, Assimp.Scene scene,
            bool throwOnTooMuchBones)
        {
            static Matrix4x4 ConvertMatrix(Assimp.Matrix4x4 m)
            {
                return new Matrix4x4( m.A1, m.B1, m.C1, m.D1,
                                        m.A2, m.B2, m.C2, m.D2,
                                        m.A3, m.B3, m.C3, m.D3,
                                        m.A4, m.B4, m.C4, m.D4);
            }

#if DEBUG
            //This statement is just to test whether the conversion method
            //above (still) works.
            Matrix4x4 convertedIdentity =
                ConvertMatrix(Assimp.Matrix4x4.Identity);
            if (convertedIdentity != Matrix4x4.Identity)
                throw new ApplicationException("The Assimp API " +
                    "has changed and requires an update of the Matrix4x4 " +
                    "conversion functionality.");
#endif

            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            if (!mesh.HasBones) return Skeleton.Empty;

            //The "scene" contains the bones as (named) scene objects in the 
            //hierarchical kind of relationship which is required for the
            //Skeleton. The properties of the bones (the indicies of the 
            //attached vertices, the weight,...), however, are stored 
            //seperately by Assimp in the "mesh". In a "Skeleton", these
            //properties are stored alongside - this requires a reorganisation
            //of the data.

            //To create a new "Skeleton", the first step is to create a
            //flat dictionary, where the key is the name of the bone and the 
            //value the bone index.
            Dictionary<string, byte> bones = new Dictionary<string, byte>();
            try
            {
                for (int i = 0; i < mesh.Bones.Count; i++)
                    bones.Add(mesh.Bones[i].Name, (byte)i);
            }
            catch
            {
                throw new FormatException("The mesh contained one " +
                    "or more bones with the same name, which is not " +
                    "supported.");
            }

            //The second step is to traverse the scene graph and duplicate
            //the bone hierarchy in a new skeleton while inserting the bone
            //parameters stored in the previous step.
            Skeleton skeleton = null;
            Node<Bone> currentSkeletonParent = null;

            //Traverses the scene hierarchy depth-first while populating the
            //skeleton hierarchy.
            Stack <Assimp.Node> assimpStack = new Stack<Assimp.Node>();
            assimpStack.Push(scene.RootNode);
            Assimp.Node skeletonAssimpRoot = null;

            while (assimpStack.Count > 0)
            {
                Assimp.Node assimpNode = assimpStack.Pop();

                //Enqueues the child nodes to continue the traversal at
                //a lower hierarchy level.
                foreach (Assimp.Node child in assimpNode.Children)
                    assimpStack.Push(child);

                //Checks for each node if a bone with the same name exists -
                //if yes, create a new skeleton node with the parameters of
                //that bone. Otherwise, ignore that node and proceed.
                if (bones.TryGetValue(assimpNode.Name, out byte index))
                {
                    if (skeletonAssimpRoot == null)
                    {
                        skeletonAssimpRoot = assimpNode.Parent;
                        currentSkeletonParent = skeleton = new Skeleton(
                            ConvertMatrix(skeletonAssimpRoot.Transform));
                    }

                    Matrix4x4 offset = ConvertMatrix(
                        mesh.Bones[index].OffsetMatrix);
                    
                    Bone bone = new Bone(assimpNode.Name, index, offset);

                    while (!currentSkeletonParent.IsRoot &&
                        currentSkeletonParent.Value.Identifier !=
                        assimpNode.Parent.Name)
                        currentSkeletonParent = currentSkeletonParent.Parent;

                    currentSkeletonParent = 
                        currentSkeletonParent.AddChild(bone);
                }
            }

            return skeleton;
        }

        private Assimp.Scene ImportScene(FileSystemPath path)
        {
            if (!path.IsAbsolute)
                throw new ArgumentException("The specified path is " +
                    "not absolute.");

            string fileExtension = path.GetFileExtension();
            if (SupportsImport(fileExtension))
            {
                try
                {
                    return assimp.ImportFile(path.PathString,
                        Assimp.PostProcessSteps.Triangulate |
                        Assimp.PostProcessSteps.LimitBoneWeights |
                        Assimp.PostProcessSteps.MakeLeftHanded);
                }
                catch (Assimp.AssimpException exc)
                {
                    throw new FormatException("The specified " +
                        "file couldn't be opened.", exc);
                }
                catch (FileNotFoundException exc)
                {
                    throw new FileNotFoundException("The specified " +
                        "file wasn't found.", exc);
                }
                catch (IOException exc)
                {
                    throw new IOException("The specified file couldn't " +
                        "be accessed.", exc);
                }
            }
            else throw new ArgumentException("The format of the specified " +
              "file is not supported.");
        }
    }
}
