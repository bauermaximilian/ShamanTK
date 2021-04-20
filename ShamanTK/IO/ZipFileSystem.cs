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
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ShamanTK.IO
{
    /// <summary>
    /// Provides functionality to read and modify files in a ZIP archive.
    /// </summary>
    /// <remarks>
    /// This file system is not thread safe - attempting to read or 
    /// especially write files simultaneously from different threads can
    /// corrupt the file system.
    /// </remarks>
    public class ZipFileSystem : DisposableBase, IFileSystem
    {
        /// <summary>
        /// Gets a boolean which indicates whether the current ZIP file system
        /// supports creating, modifying or removing files or directories 
        /// (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsWritable { get; }

        private readonly ZipArchive baseArchive;
        private readonly Stream baseArchiveStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipFileSystem"/>
        /// class.
        /// </summary>
        /// <param name="fileStream">
        /// The stream of the ZIP file, which must support reading. If the 
        /// stream supports writing too, the new <see cref="ZipFileSystem"/> 
        /// will be writable and <see cref="IsWritable"/> will be <c>true</c>.
        /// Will be disposed when the current <see cref="ZipFileSystem"/> is
        /// disposed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fileStream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="fileStream"/> is not readable.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when <paramref name="fileStream"/> is no valid ZIP file.
        /// </exception>
        public ZipFileSystem(Stream fileStream)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));

            ZipArchiveMode archiveMode;
            if (!fileStream.CanRead)
                throw new ArgumentException("The specified stream does not " +
                    "support reading operations!");
            if (fileStream.CanWrite) archiveMode = ZipArchiveMode.Update;
            else archiveMode = ZipArchiveMode.Read;

            IsWritable = fileStream.CanWrite;

            try
            {
                baseArchive = new ZipArchive(fileStream, archiveMode, false);
                baseArchiveStream = fileStream;
            }
            catch (Exception exc)
            {
                throw new ArgumentException("The specified stream was no " +
                    "valid ZIP file stream.", exc);
            }
        }

        /// <summary>
        /// Initializes a new <see cref="ZipFileSystem"/> from a ZIP file 
        /// contained in another <see cref="IFileSystem"/>.
        /// </summary>
        /// <param name="fileSystem">
        /// The parent file system where the new <see cref="ZipFileSystem"/>
        /// should be initialized in.
        /// </param>
        /// <param name="filePath">
        /// The path of the ZIP file, which will contain the data of the
        /// new <see cref="ZipFileSystem"/>.
        /// </param>
        /// <param name="overwrite">
        /// <c>true</c> to overwrite any existing files with the same
        /// <paramref name="filePath"/>, <c>false</c> to throw an 
        /// <see cref="InvalidOperationException"/> when a file with the
        /// same path already exists.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="ZipFileSystem"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fileSystem"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <see cref="IsAbsolute"/> of <paramref name="filePath"/> is 
        /// <c>false</c>, <see cref="IsFormattedAsDirectoryPath"/> of
        /// <paramref name="filePath"/> is <c>true</c>, or 
        /// <see cref="IsWritable"/> of <paramref name="fileSystem"/>
        /// is <c>false</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when a file with the specified 
        /// <paramref name="filePath"/> already exists, but
        /// <paramref name="overwrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the file couldn't be created due to an error in
        /// the specified <paramref name="fileSystem"/>.
        /// </exception>
        public static ZipFileSystem Initialize(IFileSystem fileSystem,
            FileSystemPath filePath, bool overwrite)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));
            filePath.Verify(false, true);
            if (!fileSystem.IsWritable)
                throw new ArgumentException("The specified file system " +
                    "doesn't support writing operations!");

            Stream fileStream;
            try { fileStream = fileSystem.CreateFile(filePath, overwrite); }
            catch (InvalidOperationException) { throw; }
            catch (Exception exc)
            {
                throw new IOException("The ZIP file couldn't be created in " +
                    "the specified file system.", exc);
            }

            return new ZipFileSystem(fileStream);
        }

        private static string GetConvertedPath(FileSystemPath path,
            bool requestDirectory)
        {
            return GetConvertedPath(path, requestDirectory, !requestDirectory);
        }

        private static string GetConvertedPath(FileSystemPath path,
            bool allowDirectory, bool allowFile)
        {
            path.Verify(allowDirectory, allowFile);
            return path.PathString[1..];
        }

        /// <summary>
        /// Opens a file stream.
        /// </summary>
        /// <param name="filePath">
        /// An absolute <see cref="FileSystemPath"/> specifying the file which
        /// should be opened.
        /// </param>
        /// <param name="requestWriteAccess">
        /// <c>true</c> to open the file with read-write access,
        /// <c>false</c> to open the file with read-only access.
        /// </param>
        /// <returns>The <see cref="Stream"/> to the file.</returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="FileSystemPath.IsEmpty"/> or
        /// <see cref="FileSystemPath.IsDirectoryPath"/> of 
        /// <paramref name="filePath"/> are <c>true</c> or if 
        /// <see cref="FileSystemPath.IsAbsolute"/> of 
        /// <paramref name="filePath"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Is thrown when no file at the specified 
        /// <paramref name="filePath"/> was found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <paramref name="requestWriteAccess"/> is 
        /// <c>true</c>, but <see cref="IsWritable"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the underlying operating system failed to perform
        /// the requested action.
        /// </exception>
        public Stream OpenFile(FileSystemPath filePath,
            bool requestWriteAccess)
        {
            if (requestWriteAccess && !IsWritable)
                throw FileSystem.WritingNotSupportedException;

            string path = GetConvertedPath(filePath, false);

            try
            {
                ZipArchiveEntry entry = baseArchive.GetEntry(path);
                if (entry != null)
                {
                    Stream stream = entry.Open();
                    if (!requestWriteAccess && stream.CanWrite)
                        return ReadOnlyFileSystem.CreateReadOnlyStream(stream);
                    else return stream;
                }
                else throw new FileNotFoundException("The specified file " +
                    "can't be found.");
            }
            catch (Exception exc)
            {
                if (exc is InvalidDataException)
                    throw new IOException("The ZIP file system is " +
                        "corrupted and can't be accessed.", exc);
                else throw new IOException("The file couldn't be accessed.",
                    exc);
            }
        }

        /// <summary>
        /// Checks if a file exists.
        /// </summary>
        /// <param name="filePath">
        /// An absolute <see cref="FileSystemPath"/> instance specifying the 
        /// file to be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if the file exists, <c>false</c> if the file
        /// doesn't exist.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="FileSystemPath.IsEmpty"/> or
        /// <see cref="FileSystemPath.IsDirectoryPath"/> of 
        /// <paramref name="filePath"/> are <c>true</c> or if 
        /// <see cref="FileSystemPath.IsAbsolute"/> of 
        /// <paramref name="filePath"/> is <c>false</c>.
        /// </exception>
        public bool ExistsFile(FileSystemPath filePath)
        {
            string path = GetConvertedPath(filePath, false);
            try { return baseArchive.GetEntry(path) != null; }
            catch { return false; }
        }

        /// <summary>
        /// Checks if a directory exists.
        /// </summary>
        /// <param name="directoryPath">
        /// An absolute <see cref="FileSystemPath"/> instance (formatted as
        /// directory path) specifying the directory to be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if the directory exists, <c>false</c> if the file
        /// doesn't exist.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when 
        /// <see cref="FileSystemPath.IsEmpty"/> of 
        /// <paramref name="directoryPath"/> is <c>true</c> or when
        /// <see cref="FileSystemPath.IsDirectoryPath"/> or
        /// <see cref="FileSystemPath.IsAbsolute"/> of 
        /// <paramref name="directoryPath"/> are <c>false</c>.
        /// </exception>
        public bool ExistsDirectory(FileSystemPath directoryPath)
        {
            string path = GetConvertedPath(directoryPath, true);

            if (path.Length == 0) return true;
            else
            {
                try { return baseArchive.GetEntry(path) != null; }
                catch { return false; }
            }
        }

        /// <summary>
        /// Returns an enumerable collection of files and folders.
        /// </summary>
        /// <param name="directoryPath">
        /// An absolute <see cref="FileSystemPath"/> instance (formatted as
        /// directory path) specifying the directory to be enumerated.
        /// </param>
        /// <returns>
        /// An enumerable collection of the <see cref="FileSystemPath"/> 
        /// instances of each file system entry in the directory specified by 
        /// <paramref name="directoryPath"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when 
        /// <see cref="FileSystemPath.IsEmpty"/> of 
        /// <paramref name="directoryPath"/> is <c>true</c> or when
        /// <see cref="FileSystemPath.IsDirectoryPath"/> or
        /// <see cref="FileSystemPath.IsAbsolute"/> of 
        /// <paramref name="directoryPath"/> are <c>false</c>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the underlying operating system failed to perform
        /// the requested action.
        /// </exception>
        public IEnumerable<FileSystemPath> Enumerate(
            FileSystemPath directoryPath)
        {
            directoryPath.Verify(true);
            return baseArchive.Entries
                .Select(e => ParseOrEmpty("/" + e.FullName))
                .Where(e => !e.IsEmpty && directoryPath.IsParentOf(e, false));
        }

        private static FileSystemPath ParseOrEmpty(string path)
        {
            try { return new FileSystemPath(path, false); }
            catch { return FileSystemPath.Empty; }
        }

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <param name="directoryPath">
        /// An absolute <see cref="FileSystemPath"/> instance (formatted as
        /// directory path) specifying the name and path of the new directory.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when 
        /// <see cref="FileSystemPath.IsEmpty"/> of 
        /// <paramref name="directoryPath"/> is <c>true</c> or when
        /// <see cref="FileSystemPath.IsDirectoryPath"/> or
        /// <see cref="FileSystemPath.IsAbsolute"/> of 
        /// <paramref name="directoryPath"/> are <c>false</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="IsWritable"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the underlying operating system failed to perform
        /// the requested action.
        /// </exception>
        public void CreateDirectory(FileSystemPath directoryPath)
        {
            if (!IsWritable) throw FileSystem.WritingNotSupportedException;

            string path = GetConvertedPath(directoryPath, true);

            //If the path is empty here, the specified directory path was 
            //FileSystemPath.Root. So, if the command is "create the
            //root directory" (which already exists), the request was
            //fulfilled without doing anything. Success!
            if (path.Length == 0) return;

            try { baseArchive.CreateEntry(path); }
            catch (Exception exc)
            {
                throw new IOException("The directory couldn't be created.",
                    exc);
            }
        }

        /// <summary>
        /// Create a new file and the directory it's contained in, if not 
        /// already existant.
        /// </summary>
        /// <param name="filePath">
        /// An absolute <see cref="FileSystemPath"/> specifying the name and
        /// path of the new file.
        /// </param>
        /// <param name="overwrite">
        /// <c>true</c> to overwrite existing resources, <c>false</c> to
        /// throw an exception if a resource with the same 
        /// <paramref name="filePath"/> already exists.
        /// </param>
        /// <returns>The <see cref="Stream"/> to the new file.</returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="FileSystemPath.IsEmpty"/> or
        /// <see cref="FileSystemPath.IsDirectoryPath"/> of 
        /// <paramref name="filePath"/> are <c>true</c> or if 
        /// <see cref="FileSystemPath.IsAbsolute"/> of 
        /// <paramref name="filePath"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when a resource at the specified 
        /// <paramref name="filePath"/> already exists and 
        /// <paramref name="overwrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="IsWritable"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the underlying operating system failed to perform
        /// the requested action.
        /// </exception>
        public Stream CreateFile(FileSystemPath filePath, bool overwrite)
        {
            if (!IsWritable) throw FileSystem.WritingNotSupportedException;

            string path = GetConvertedPath(filePath, false);

            try
            {
                ZipArchiveEntry existingEntry = baseArchive.GetEntry(path);
                if (existingEntry != null)
                {
                    if (overwrite)
                    {
                        try { existingEntry.Delete(); }
                        catch (Exception exc)
                        {
                            throw new IOException("The existing file " +
                                "couldn't be overwritten.", exc);
                        }
                    }
                    else throw new InvalidOperationException("A file at " +
                      "the specified location already exists.");
                }

                ZipArchiveEntry newEntry = baseArchive.CreateEntry(path);
                return newEntry.Open();
            }
            catch (InvalidOperationException) { throw; }
            catch (Exception exc)
            {
                if (exc is InvalidDataException)
                    throw new IOException("The ZIP file system is " +
                        "corrupted and can't be accessed.", exc);
                else throw new IOException("The file couldn't be accessed.",
                    exc);
            }
        }

        /// <summary>
        /// Deletes a file or directory.
        /// </summary>
        /// <param name="path">
        /// An absolute <see cref="FileSystemPath"/> specifying the file
        /// or directory to be removed.
        /// </param>
        /// <param name="recursive">
        /// When <paramref name="path"/> defines a directory, <c>true</c> 
        /// will remove the directory and all files and subdirectories 
        /// contained in the specified path. If this parameter is <c>false</c>,
        /// only files or empty directories are removed and attempts to 
        /// remove a non-empty directory will cause an
        /// <see cref="InvalidOperationException"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="FileSystemPath.IsEmpty"/> of 
        /// <paramref name="filePath"/> is <c>true</c> or if 
        /// <see cref="FileSystemPath.IsAbsolute"/> of 
        /// <paramref name="filePath"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <paramref name="recursive"/> is <c>false</c> and
        /// <paramref name="path"/> referred to a non-empty directory.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="IsWritable"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the underlying operating system failed to perform
        /// the requested action.
        /// </exception>
        public void Delete(FileSystemPath path, bool recursive)
        {
            if (!IsWritable) throw FileSystem.WritingNotSupportedException;

            path.Verify(true, true);

            ZipArchiveEntry entry;
            try
            {
                entry = baseArchive.GetEntry(GetConvertedPath(path, 
                    true, true));
            }
            catch { entry = null; }

            if (entry == null) return;
            else if (!path.IsDirectoryPath)
            {
                try { entry.Delete(); }
                catch (Exception exc)
                {
                    throw new IOException("The file couldn't be deleted.",
                        exc);
                }
            }
            else
            {
                if (!recursive)
                {
                    if (Enumerate(path).Count() > 0)
                        throw new InvalidOperationException("The directory " +
                            "couldn't been removed - it wasn't empty.");
                }

                try { entry.Delete(); }
                catch (Exception exc)
                {
                    throw new IOException("The directory couldn't be deleted.",
                        exc);
                }
            }
        }

        /// <summary>
        /// Disposes the underlying <see cref="ZipArchive"/>, if 
        /// <paramref name="disposing"/> is <c>true</c>.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to dispose the underlying <see cref="ZipArchive"/>,
        /// <c>false</c> to do nothing.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            baseArchiveStream.Flush();
            if (disposing) baseArchive.Dispose();
        }
    }
}
