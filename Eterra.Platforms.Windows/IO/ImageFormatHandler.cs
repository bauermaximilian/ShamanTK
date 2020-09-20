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

using Eterra.IO;
using System;
using System.IO;
using System.Linq;

namespace Eterra.Platforms.Windows.IO
{
    class ImageFormatHandler : IResourceFormatHandler
    {
        private readonly string[] supportedFormats = new string[]
        {
            "bmp", "gif", "jpg", "jpeg", "png", "tif", "tiff"
        };

        public bool SupportsExport(string fileExtensionLowercase)
        {
            /*if (fileExtensionLowercase == null)
                throw new ArgumentNullException(
                    nameof(fileExtensionLowercase));

            return supportedFormats.Contains(fileExtensionLowercase);*/
            return false;
        }

        public bool SupportsImport(string fileExtensionLowercase)
        {
            if (fileExtensionLowercase == null)
                throw new ArgumentNullException(
                    nameof(fileExtensionLowercase));

            return supportedFormats.Contains(fileExtensionLowercase);
        }

        public void Export(object resource, ResourceManager manager, 
            ResourcePath path, bool overwrite)
        {
            throw new NotImplementedException();
        }

        public T Import<T>(ResourceManager manager, ResourcePath path)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            if (!path.IsAbsolute)
                throw new ArgumentException("The specified resource path " +
                    "isn't absolute.");

            string extension = path.Path.GetFileExtension();
            if (SupportsImport(extension))
            {
                using Stream stream = manager.FileSystem.OpenFile(
                    path.Path, false);
                try
                {
                    return (T)(object)BitmapTextureData.FromStream(stream);
                }
                catch (InvalidCastException)
                {
                    throw new FormatException("The requested resource was " +
                        "found, but had a different type than requested " +
                        "and couldn't be converted into the requested type.");
                }
            }
            else throw new NotSupportedException("The specified image " +
                "file type is not supported.");
        }
    }
}
