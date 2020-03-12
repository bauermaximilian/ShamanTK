/* 
 * Eterra Framework
 * A simple framework for creating multimedia applications.
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

using Eterra.Common;
using System;
using System.IO;

namespace Eterra.IO
{
    //TODO: Test the NativeFormatHandler class.

    /// <summary>
    /// Defines the various types of resources supported by the
    /// <see cref="NativeFormatHandler"/>.
    /// </summary>
    /// <remarks>
    /// The assigned letters are used to determine the right type for the
    /// importer without specifying a file extension.
    /// </remarks>
    internal enum ResourceType
    {
        Texture = 'T',
        Mesh = 'M',
        Timeline = 'A',
        SpriteFont = 'F',
        Scene = 'S'
    }

    /// <summary>
    /// Provides importing and exporting functionality for the native engine
    /// resource container files.
    /// </summary>
    internal sealed class NativeFormatHandler : IResourceFormatHandler
    {
        /// <summary>
        /// Defines the file extension of the native resource file format.
        /// </summary>
        public const string FileExtension = "eres";

        /// <summary>
        /// Gets the <see cref="ResourceQuery"/> which is used when no
        /// <see cref="ResourceQuery"/> is defined for import/export.
        /// Is used as default for 
        /// <see cref="ResourceType.Union"/>.
        /// </summary>
        public static ResourceQuery DefaultResourceEntry { get; }
            = new ResourceQuery("index");

        /// <summary>
        /// Gets the size of the header of a resource file entry.
        /// </summary>
        internal static uint HeaderSize { get; }
            = StreamExtension.GetStringFixedSize(MagicBytesResource)
                + sizeof(char);

        private const string MagicBytesResource = "ETERRA-";

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeFormatHandler"/>
        /// class.
        /// </summary>
        public NativeFormatHandler() { }

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
            return fileExtensionLowercase == FileExtension;
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
            return fileExtensionLowercase == FileExtension;
        }

        /// <summary>
        /// Opens a ZIP file system from another <see cref="IFileSystem"/>.
        /// </summary>
        /// <param name="fileSystem">
        /// The file system which contains the ZIP file.
        /// </param>
        /// <param name="path">
        /// The path to the ZIP file.
        /// </param>
        /// <param name="writable">
        /// <c>true</c> to open the ZIP file system with read and write access
        /// and create a new ZIP file if no file with the specified
        /// <paramref name="path"/> exists, <c>false</c> to open the ZIP file 
        /// system with read-only access and throw an exception when the
        /// file doesn't exist.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="ZipFileSystem"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fileSystem"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="IsEmpty"/> of the path is <c>true</c>, 
        /// <see cref="IsAbsolute"/> of the path is <c>false</c>,
        /// when <paramref name="requireDirectoryPath"/> is <c>true</c> but
        /// <see cref="IsFormattedAsDirectoryPath"/> of the path is 
        /// <c>false</c> or when <paramref name="requireDirectoryPath"/> is 
        /// <c>false</c> but <see cref="IsFormattedAsDirectoryPath"/> 
        /// of the path is <c>true</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when the file extension of the file in 
        /// <paramref name="path"/> isn't equal to <see cref="FileExtension"/>.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Is thrown when no file at the specified <paramref name="path"/> 
        /// existed and <paramref name="writable"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the specified ZIP file had an invalid format.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the specified ZIP file couldn't be accessed.
        /// </exception>
        private ZipFileSystem OpenResourceFile(IFileSystem fileSystem,
            FileSystemPath path, bool writable)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));
            path.Verify(false);
            if (path.GetFileExtension() != FileExtension)
                throw new NotSupportedException("The specified file type " +
                    "was not supported natively.");

            Stream fsStream = null;
            try
            {
                if (writable)
                {
                    if (fileSystem.ExistsFile(path))
                        fsStream = fileSystem.OpenFile(path, true);
                    else
                        fsStream = fileSystem.CreateFile(path, false);
                }
                else fsStream = fileSystem.CreateFile(path, false);
            }
            catch (FileNotFoundException exc)
            {
                throw new FileNotFoundException("The resource path " +
                    "couldn't be resolved into an existing resource.", exc);
            }
            catch (IOException) { throw; }

            try { return new ZipFileSystem(fsStream); }
            catch (Exception exc)
            {
                if (fsStream != null) fsStream.Dispose();

                if (exc is FormatException) throw exc;
                else throw new IOException("The resource file stream " +
                    "of the resource managers' file system was invalid.",
                    exc);
            }
        }

        /// <summary>
        /// Imports a resource.
        /// </summary>
        /// <param name="manager">
        /// The <see cref="ResourceManager"/> this handler should work with.
        /// </param>
        /// <param name="resourcePath">
        /// The path to the resource, which should be imported.
        /// </param>
        /// <returns>
        /// A new <see cref="object"/> instance of the imported resource.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="ResourcePath.IsAbsolute"/> of 
        /// <paramref name="resourcePath"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when the file type of the specified resource is 
        /// not supported.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Is thrown when the specified <paramref name="path"/> couldn't be
        /// resolved into an existing resource.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the specified file had an invalid format.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the specified file couldn't be accessed.
        /// </exception>
        public T Import<T>(ResourceManager manager, ResourcePath path)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            if (!path.IsAbsolute)
                throw new ArgumentException("The specified resource path " +
                    "isn't absolute.");

            string extension = path.Path.GetFileExtension();

            if (extension == FileExtension)
                return ImportNativeResource<T>(manager, path);
            else throw new NotSupportedException("The specified file was " +
                    "no native resource file.");
        }

        private T ImportNativeResource<T>(ResourceManager manager, 
            ResourcePath path)
        {
            ResourceQuery query = ResourceQuery.Empty;

            if (path.Query.IsEmpty) query = DefaultResourceEntry;
            else query = path.Query;

            FileSystemPath resourceEntryPath = FileSystemPath.Combine(
                    FileSystemPath.Root,
                    new FileSystemPath(query, true));

            try
            {
                using (ZipFileSystem resourceFileSystem = OpenResourceFile(
                    manager.FileSystem, path.Path, false))
                {
                    using (Stream stream = manager.FileSystem.OpenFile(
                        resourceEntryPath, false))
                    {
                        ResourceType type = ReadEntryHeader(stream);

                        switch (type)
                        {
                            case ResourceType.Mesh:
                                return (T)(object)MeshData.Load(
                                    stream, false);
                            case ResourceType.Texture:
                                return (T)(object)TextureData.Load(
                                    stream, false);
                            case ResourceType.Timeline:
                                return (T)(object)Timeline.Load(
                                    stream, false);
                            case ResourceType.SpriteFont:
                                return (T)(object)SpriteFontData.Load(
                                    stream, false);
                            //EXTENDHINT: Add new format importers here.
                            default:
                                throw new FormatException("The file format " +
                                "defined in the resource file header was " +
                                "valid, but not supported yet.");
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                if (exc is FileNotFoundException)
                    throw new FileNotFoundException("The resource " +
                        "query couldn't be resolved into an existing " +
                        "resource.", exc);
                else if (exc is InvalidCastException)
                    throw new FileNotFoundException("No resource with the " +
                        "specified path and type was found, and a resource " +
                        "with the specified path but a different type " +
                        "couldn't be converted into the requested type.");
                else if (exc is FormatException ||
                    exc is EndOfStreamException)
                    throw new FormatException("The resource file entry " +
                        "format was invalid.", exc);
                else throw new IOException("An error occurred while reading " +
                    "from the resource stream.", exc);
            }
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
        /// Is thrown when <see cref="ResourcePath.IsEmpty"/> of 
        /// <paramref name="resourcePath"/> is <c>true</c>, when
        /// <see cref="FileSystemPath.IsDirectoryPath"/> of
        /// <paramref name="resourcePath"/> is <c>true</c> or when 
        /// <see cref="ResourcePath.Path"/> doesn't specify a file extension
        /// or when <see cref="ResourcePath.Query"/> is not null or empty.
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
        /// <exception cref="IOException">
        /// Is thrown if the specified file couldn't be accessed.
        /// </exception>
        public void Export(object resource, ResourceManager manager,
            ResourcePath path, bool overwrite)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            ResourceQuery query = ResourceQuery.Empty;

            if (path.Query.IsEmpty) query = DefaultResourceEntry;
            else query = path.Query;

            FileSystemPath resourceEntryPath = FileSystemPath.Combine(
                    FileSystemPath.Root,
                    new FileSystemPath(query, true));

            try
            {
                using (ZipFileSystem resourceFileSystem = OpenResourceFile(
                    manager.FileSystem, path.Path, true))
                {
                    using (Stream stream = manager.FileSystem.CreateFile(
                        resourceEntryPath, overwrite))
                    {
                        if (resource is MeshData meshResource)
                            meshResource.Save(stream, true);
                        else if (resource is TextureData textureResource)
                            textureResource.Save(stream, true);
                        else if (resource is Timeline timelineResource)
                            timelineResource.Save(stream, true);
                        else if (resource is SpriteFontData
                            spriteFontDataResource)
                            spriteFontDataResource.Save(stream, true);
                        //EXTENDHINT: Add new format exporters here.
                        else throw new FormatException("The resource type " +
                              "was not supported natively.");
                    }
                }
            }
            catch (Exception exc)
            {
                if (exc is FileNotFoundException)
                    throw new FileNotFoundException("The resource " +
                        "query couldn't be resolved into an existing " +
                        "resource.", exc);
                else if (exc is FormatException ||
                    exc is EndOfStreamException)
                    throw new FormatException("The resource file entry " +
                        "format was invalid.", exc);
                else throw new IOException("An error occurred while reading " +
                    "from the resource stream.", exc);
            }
        }

        /// <summary>
        /// Writes a resource entry header to a stream and advances the 
        /// position of the stream by the amount of bytes defined by 
        /// <see cref="HeaderSize"/>.
        /// </summary>
        /// <param name="resourceType">
        /// The resource type.
        /// </param>
        /// <param name="stream">
        /// The writable target stream.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="StreamWrapper.CanWrite"/> of 
        /// <paramref name="stream"/> is <c>false</c> or when
        /// <paramref name="resourceType"/> is invalid.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <see cref="BaseStream"/> of 
        /// <paramref name="stream"/> was disposed.
        /// </exception>
        internal static void WriteEntryHeader(ResourceType resourceType,
            Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("The specified stream doesn't " +
                    "support writing.");
            if (Enum.IsDefined(typeof(ResourceType), resourceType))
                throw new ArgumentException("The value of "
                  + nameof(resourceType) + " is invalid.");

            stream.WriteStringFixed(MagicBytesResource);
            stream.WriteChar((char)resourceType);
        }

        /// <summary>
        /// Verifies a <see cref="FileSystemPath"/> for usage in a context
        /// where it specifies the file system path of a container file
        /// in the format which can be imported/exported by this
        /// <see cref="NativeFormatHandler"/>.
        /// If the path is valid, calling this method has no effect.
        /// Otherwise, an <see cref="ArgumentException"/> is thrown.
        /// </summary>
        /// <param name="path">
        /// The path to be verified.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when the path is empty, non-absolute, not a file path or 
        /// the file extension of the file doesn't match the file extension 
        /// of a native container file (as defined in 
        /// <see cref="FileExtension"/>).
        /// </exception>
        internal static void VerifyNativeContainerFilePath(FileSystemPath path)
        {
            if (path.IsEmpty)
                throw new ArgumentException("The specified native container " +
                    "file path is empty.");
            if (!path.IsAbsolute)
                throw new ArgumentException("The specified native container " +
                    "file path is not absolute and can't be used.");
            if (path.IsDirectoryPath)
                throw new ArgumentException("The specified native container " +
                    "file path is a directory path, not a file path.");
            if (path.GetFileExtension() !=
                NativeFormatHandler.FileExtension)
                throw new ArgumentException("The specified native container " +
                    "file path doesn't have the required file extension of " +
                    "native container files, which is '." +
                    NativeFormatHandler.FileExtension + "'.");
        }

        /// <summary>
        /// Reads a resource header (and the resource type) from a stream
        /// and advances the position of the stream by the amount of bytes
        /// defined by <see cref="HeaderSize"/>.
        /// </summary>
        /// <param name="stream">
        /// The readable source stream.
        /// </param>
        /// <returns>A <see cref="ResourceType"/> value.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="StreamWrapper.CanRead"/> of
        /// <paramref name="stream"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the header was invalid.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// header could be read completely.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <see cref="BaseStream"/> of 
        /// <paramref name="stream"/> was disposed.
        /// </exception>
        internal static ResourceType ReadEntryHeader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("The specified stream doesn't " +
                    "support reading.");

            string headerStart = stream.ReadStringFixed(
                StreamExtension.GetStringFixedSize(MagicBytesResource));
            char formatChar = stream.ReadChar();

            if (headerStart == MagicBytesResource)
            {
                ResourceType resourceType = (ResourceType)formatChar;
                if (Enum.IsDefined(typeof(ResourceType), resourceType))
                    return resourceType;
                else throw new FormatException("The format character " +
                    "doesn't specify a known entry format.");
            }
            else throw new FormatException("The header start was invalid.");
        }
    }
}
