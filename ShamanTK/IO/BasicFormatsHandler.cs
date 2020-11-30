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
using System.IO;
using System.Linq;

namespace ShamanTK.IO
{
    /// <summary>
    /// Provides importing and exporting functionality for resource files in a 
    /// simple non-native format. Supported are image files in the PPM format,
    /// mesh files in the Wavefront OBJ format and sound files in the WAV
    /// format (windows-standard signed 16-bit PCM).
    /// This class is not completely implemented yet and only supports 
    /// importing WAV files now.
    /// </summary>
    internal sealed class BasicFormatsHandler : IResourceFormatHandler
    {
        private readonly string[] wavFileExtensions = new string[] { "wav" };

        private readonly string[] ppmFileExtensions = 
            new string[] { /*"pbm", "pgm", "ppm"*/ };

        private readonly string[] wavefrontFileExtensions =
            new string[] { /*"obj"*/ };

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicFormatsHandler"/>
        /// class.
        /// </summary>
        public BasicFormatsHandler() { }

        /// <summary>
        /// Checks whether a specific file type can be imported by the current
        /// format handle or not.
        /// </summary>
        /// <param name="fileExtensionLowercase">
        /// The extension of the file as lowercase string (without any 
        /// preceding periods, just like 
        /// <see cref="FileSystemPath.GetFileExtension"/>).
        /// </param>
        /// <returns>
        /// <c>true</c> if files of the specified format can be imported with
        /// <see cref="Import(ResourceManager, FileSystemPath, string)"/>,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fileExtensionLowercase"/> is null.
        /// </exception>
        public bool SupportsImport(string fileExtensionLowercase)
        {
            if (fileExtensionLowercase == null)
                throw new ArgumentNullException(
                    nameof(fileExtensionLowercase));
            return wavFileExtensions.Contains(fileExtensionLowercase) ||
                ppmFileExtensions.Contains(fileExtensionLowercase) ||
                wavefrontFileExtensions.Contains(fileExtensionLowercase);
        }

        /// <summary>
        /// Checks whether a specific file type can be exported by the current
        /// format handler or not.
        /// </summary>
        /// <param name="fileExtensionLowercase">
        /// The extension of the file as lowercase string (without any 
        /// preceding periods, just like 
        /// <see cref="FileSystemPath.GetFileExtension"/>).
        /// </param>
        /// <returns>
        /// <c>true</c> if files of the specified format can be exported with
        /// <see cref="Export(object, ResourceManager, FileSystemPath, 
        /// string)"/>, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fileExtensionLowercase"/> is null.
        /// </exception>
        public bool SupportsExport(string fileExtensionLowercase)
        {
            if (fileExtensionLowercase == null)
                throw new ArgumentNullException(
                    nameof(fileExtensionLowercase));
            return /*wavFileExtensions.Contains(fileExtensionLowercase) ||*/
                ppmFileExtensions.Contains(fileExtensionLowercase) ||
                wavefrontFileExtensions.Contains(fileExtensionLowercase);
        }

        /// <summary>
        /// Imports a resource.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the resource to be imported. 
        /// If the specified <paramref name="path"/> is ambigous (e.g. because
        /// it doesn't contain a <see cref="ResourcePath.Query"/>), the first 
        /// resource matching the specified <typeparamref name="T"/> is 
        /// returned.
        /// </typeparam>
        /// <param name="manager">
        /// The <see cref="ResourceManager"/> this handler should work with.
        /// </param>
        /// <param name="path">
        /// The path to the resource which should be imported.
        /// If the specified <paramref name="path"/> is ambigous (e.g. because
        /// it doesn't contain a <see cref="ResourcePath.Query"/>), the first 
        /// resource matching the specified <typeparamref name="T"/> is 
        /// returned.
        /// </param>
        /// <returns>
        /// The requested resource as <typeparamref name="T"/>.
        /// If the specified <paramref name="path"/> is ambigous (e.g. because
        /// it doesn't contain a <see cref="ResourcePath.Query"/>), the first 
        /// resource matching the specified <typeparamref name="T"/> is 
        /// returned.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="path"/> is not absolute.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when the file type specified by the file extension in
        /// <paramref name="path"/> is not supported.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Is thrown when the <paramref name="path"/> is valid, but couldn't
        /// be resolved into an existing resource of type
        /// <typeparamref name="T"/>. This exception is also thrown when
        /// a resource with the specified <paramref name="path"/> exists,
        /// but has a different <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the specified resource file had an invalid format.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the specified resource file couldn't be accessed
        /// due to an error in the file system.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the <see cref="FileSystem"/> of the specified
        /// <paramref name="manager"/> was disposed and can no longer be used
        /// to import or export resource files.
        /// </exception>
        public T Import<T>(ResourceManager manager, ResourcePath path)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            if (!path.IsAbsolute)
                throw new ArgumentException("The specified resource path " +
                    "isn't absolute.");

            string extension = path.Path.GetFileExtension();

            if (wavFileExtensions.Contains(extension))
            {
                Stream stream = manager.FileSystem.OpenFile(path.Path, false);
                if (typeof(T) == typeof(SoundDataStream))
                    return (T)(object)SoundDataStream.OpenWave(stream, true);
                else throw new FileNotFoundException("A resource with the " +
                    "specified path was found, but wasn't of type SoundData.");
            }
            else throw new NotSupportedException("The specified file was " +
                "no WAV sound file.");
        }

        /// <summary>
        /// Exports a resource.
        /// </summary>
        /// <param name="resource">
        /// The resource instance to be exported.
        /// </param>
        /// <param name="manager">
        /// The <see cref="ResourceManager"/> this handler should work with.
        /// </param>
        /// <param name="path">
        /// The path to the resource which should be imported.
        /// </param>
        /// <param name="overwrite">
        /// <c>true</c> to overwrite an existing resource with the same 
        /// <paramref name="path"/> (other resources in the same file are 
        /// retained and adding a new resource to a file isn't influenced
        /// by this parameter value), <c>false</c> to throw an 
        /// <see cref="InvalidOperationException"/> in that case instead.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="resource"/> or 
        /// <paramref name="manager"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="ResourcePath.IsAbsolute"/> of 
        /// <paramref name="resourcePath"/> is <c>false</c>, when
        /// <see cref="ResourcePath.Path"/> doesn't specify a file extension
        /// or when <paramref name="resource"/> isn't derived from the type
        /// <see cref="SoundData"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when the file format specified by
        /// <see cref="ResourcePath.Path"/> of <paramref name="path"/> 
        /// is not supported.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <paramref name="overwrite"/> is <c>false</c> and
        /// a resource with the specified <paramref name="path"/> already
        /// exists.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when a file with the <see cref="ResourcePath.Path"/> of
        /// <paramref name="path"/> already exists, but is invalid and can't
        /// be used to add or overwrite resources.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// Is thrown if the specified file couldn't be accessed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the <see cref="FileSystem"/> of the specified
        /// <paramref name="manager"/> was disposed and can no longer be used
        /// to import or export resource files.
        /// </exception>
        public void Export(object resource, ResourceManager manager, 
            ResourcePath path, bool overwrite)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            throw new NotSupportedException("Exporting WAV files is not " +
                "supported yet.");

            /*
            if (!path.IsAbsolute)
                throw new ArgumentException("The specified resource path " +
                    "isn't absolute.");

            string extension = path.Path.GetFileExtension();

            if (wavFileExtensions.Contains(extension)) {
                if (resource is SoundData resourceSoundData)
                    resourceSoundData.SaveWAV(
                        manager.FileSystem.CreateFile(path.Path, overwrite));
                else throw new ArgumentException("The specified resource " +
                    "is no sound data instance.");
            }
            else throw new NotSupportedException("The specified resource " +
                "path has an invalid file extension for a WAV sound file.");
            */
        }
    }
}
