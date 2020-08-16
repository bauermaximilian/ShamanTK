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
            Skin skin = root.LogicalSkins.First();
            Accessor accessor = skin.GetInverseBindMatricesAccessor();
            var matrixArray = accessor.AsMatrix4x4Array();
            
            //root.LogicalAnimations.First().FindTranslationSampler()
            throw new NotImplementedException();
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
