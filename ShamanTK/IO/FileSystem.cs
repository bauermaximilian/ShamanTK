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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShamanTK.IO
{
    /// <summary>
    /// Provides functionality to read and modify files on the local 
    /// file system.
    /// </summary>
    /// <remarks>
    /// Even though this class implements the <see cref="IDisposable"/>
    /// interface, using the <see cref="Dispose"/> method of instances of this
    /// class has no effect, as no unmanaged resources are used directly by
    /// this class.
    /// The <see cref="Stream"/> instances created by this class
    /// must be disposed where they're used.
    /// </remarks>
    public class FileSystem : IFileSystem
    {
        /// <summary>
        /// Defines the path of the default program data directory, relative
        /// to the application location.
        /// </summary>
        public const string ProgramDataDirectory = "Assets";

        /// <summary>
        /// Gets the default <see cref="FileSystem"/> instance, which can be
        /// used to read application resources. For saving data of the current
        /// user, see <see cref="CreateUserDataFileSystem(string)"/>.
        /// </summary>
        public static FileSystem ProgramData { get; }
            = new FileSystem(ProgramDataDirectory, false);

        /// <summary>
        /// Gets a boolean which indicates whether the current file system
        /// supports creating, modifying or removing files or directories 
        /// (<c>true</c>) or not (<c>false</c>). Regardless of this properties 
        /// value, however, access to certain files can still be denied,
        /// depending on the requested action and restrictions by the OS.
        /// </summary>
        public bool IsWritable { get; }

        /// <summary>
        /// Gets the default exception which is thrown when a writing operation
        /// is attempted on a read-only file system.
        /// </summary>
        internal static NotSupportedException WritingNotSupportedException
            { get; } = new NotSupportedException("The file system is " +
                "read-only and can't be modified.");

        private readonly string root;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystem"/> class
        /// with access to all files and folders in a specific root directory.
        /// </summary>
        /// <param name="rootDirectoryPath">
        /// The path to the directory, which will be used as root of the
        /// new <see cref="FileSystem"/> and all paths used in later 
        /// operations on this instance. If the specified path is not
        /// absolute, it's resolved in relation to the path of the
        /// application executable.
        /// </param>
        /// <param name="isWritable">
        /// <c>true</c> to allow modification of files and folders,
        /// <c>false</c> to grant read-only access only.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="rootDirectoryPath"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="rootDirectoryPath"/> contains one 
        /// of the elements defined in <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        public FileSystem(string rootDirectoryPath, bool isWritable)
        {
            if (rootDirectoryPath == null)
                throw new ArgumentNullException(nameof(rootDirectoryPath));

            IsWritable = isWritable;

            try
            {
                if (!Path.IsPathRooted(rootDirectoryPath))
                {
                    root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        rootDirectoryPath);
                }
                else root = Path.Combine(rootDirectoryPath, "");
            }
            catch (Exception exc)
            {
                throw new ArgumentException("The specified root path " +
                    "was invalid!", exc);
            }
        }

        /// <summary>
        /// Creates a new <see cref="FileSystem"/> instance with its root in
        /// an application-owned folder in the application data directory of
        /// the current user profile, which can be used to read and write 
        /// user-specific settings, save data or likewise.
        /// </summary>
        /// <param name="applicationName">
        /// The name of the application, which will be used as name for the
        /// root folder in the application data directory.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="FileSystem"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="applicationName"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="applicationName"/> contains one 
        /// of the elements defined in <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        public static FileSystem CreateUserDataFileSystem(
            string applicationName)
        {
            if (applicationName == null)
                throw new ArgumentNullException(nameof(applicationName));

            string appDataRootPath = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            string userDataRootPath = Path.Combine(appDataRootPath,
                applicationName);

            return new FileSystem(userDataRootPath, true);
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
                throw WritingNotSupportedException;

            string platformPath;
            try { platformPath = GetPlatformPath(filePath, false, true); }
            catch (ArgumentException) { throw; }

            try
            {
                if (requestWriteAccess)
                    return File.Open(platformPath, FileMode.Open);
                else
                    return File.OpenRead(platformPath);
            }
            catch (Exception exc)
            {
                if (exc is FileNotFoundException || 
                    exc is DirectoryNotFoundException)
                    throw new FileNotFoundException("The specified file " +
                        "can't be found or the file path is invalid.", exc);
                else throw new IOException("The specified " +
                        "file can't be accessed.", exc);
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
            string platformPath;
            try { platformPath = GetPlatformPath(filePath, false, true); }
            catch (ArgumentException) { throw; }

            return File.Exists(platformPath);
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
            string platformPath;
            try { platformPath = GetPlatformPath(directoryPath, true, false); }
            catch (ArgumentException) { throw; }

            return Directory.Exists(platformPath);
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
        /// <exception cref="DirectoryNotFoundException">
        /// Is thrown when the specified <paramref name="directoryPath"/> 
        /// couldn't be resolved into an existing directory.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the underlying operating system failed to perform
        /// the requested action.
        /// </exception>
        public IEnumerable<FileSystemPath> Enumerate(
            FileSystemPath directoryPath)
        {
            string platformPath;
            try { platformPath = GetPlatformPath(directoryPath, true, false); }
            catch (ArgumentException) { throw; }

            try
            {
                return Directory.EnumerateFiles(platformPath)
                    .Select(p => GetFileSystemPath(p));
            }
            catch (DirectoryNotFoundException exc)
            {
                throw new DirectoryNotFoundException("The specified " +
                    "directory couldn't be found.", exc);
            }
            catch (Exception exc)
            {
                throw new IOException("The specified directory couldn't be " +
                    "enumerated.", exc);
            }
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
            if (!IsWritable)
                throw WritingNotSupportedException;

            string platformPath;
            try { platformPath = GetPlatformPath(directoryPath, true, false); }
            catch (ArgumentException) { throw; }

            try { Directory.CreateDirectory(platformPath); }
            catch (Exception exc)
            {
                throw new IOException("The specified " +
                    "directory can't be created.", exc);
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
            if (!IsWritable)
                throw WritingNotSupportedException;

            string platformPath, parentDirPlatformPath;
            try
            {
                platformPath = GetPlatformPath(filePath, false, true);
                FileSystemPath parentDirPath = 
                    filePath.GetParentDirectory();
                parentDirPlatformPath = GetPlatformPath(parentDirPath,
                    true, false);
            }
            catch (ArgumentException) { throw; }

            try
            {
                if (!Directory.Exists(parentDirPlatformPath))
                    Directory.CreateDirectory(parentDirPlatformPath);
            }
            catch (Exception exc)
            {
                throw new IOException("The parent directory of the specified " +
                    "file can't be created.", exc);
            }

            if (File.Exists(platformPath) && !overwrite)
                throw new InvalidOperationException("A file at the " +
                    "specified location already exists.");
            else try { return File.Create(platformPath); }
                catch (Exception exc)
                {
                    throw new IOException("The specified " +
                        "file can't be created.", exc);
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
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current file system is disposed and can't be
        /// used anymore.
        /// </exception>
        public void Delete(FileSystemPath path, bool recursive)
        {
            if (!IsWritable)
                throw WritingNotSupportedException;

            string platformPath;
            try { platformPath = GetPlatformPath(path, true, true); }
            catch (ArgumentException) { throw; }

            try
            {
                if (path.IsDirectoryPath)
                {
                    bool isEmpty = true;
                    foreach (string p in Directory.EnumerateFileSystemEntries(
                        platformPath))
                    {
                        isEmpty = false;
                        break;
                    }
                    if (!recursive && !isEmpty)
                        throw new InvalidOperationException("The specified " +
                            "directory was not empty and recursive deletion " +
                            "was forbidden!");

                    Directory.Delete(platformPath, recursive);
                }
                else File.Delete(platformPath);
            }
            catch (Exception exc)
            {
                if (exc is InvalidOperationException) throw;
                else if (exc is DirectoryNotFoundException ||
                    exc is FileNotFoundException) return;
                else throw new IOException("The specified resource " +
                    "couldn't be removed.", exc);
            }
        }

        /// <summary>
        /// Verifies and converts an absolute, non-empty 
        /// <see cref="FileSystemPath"/> into a string with the format defined 
        /// by the current operating system.
        /// </summary>
        /// <param name="path">The path to be converted.</param>
        /// <param name="requireDirectoryPath">
        /// <c>true</c> if the <paramref name="path"/> is expected to be
        /// formatted as directory path, <c>false</c> if a file path is 
        /// required. This can be used for verification if the specified path
        /// can be used in the given context.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="FileSystemPath.IsEmpty"/> of 
        /// <paramref name="filePath"/> is <c>true</c>, 
        /// <see cref="FileSystemPath.IsAbsolute"/> of 
        /// <paramref name="filePath"/> is <c>false</c>,
        /// when <paramref name="requireDirectoryPath"/> is <c>true</c> but
        /// <see cref="FileSystemPath.IsDirectoryPath"/> is
        /// <c>false</c> or when <paramref name="requireDirectoryPath"/>
        /// is <c>false</c> but 
        /// <see cref="FileSystemPath.IsDirectoryPath"/> is
        /// <c>true</c>.
        /// </exception>
        private string GetPlatformPath(FileSystemPath path,
            bool allowDirectoryPath, bool allowFilePath)
        {
            try { path.Verify(allowDirectoryPath, allowFilePath); }
            catch (ArgumentException) { throw; }

            string convertedPath = path.PathString.TrimStart(
                FileSystemPath.SeparatorPathElement).Replace(
                FileSystemPath.SeparatorPathElement,
                Path.DirectorySeparatorChar);

            //If no root is given to which all paths should be set relative to,
            //the file system path is assumed to be absolute - and the first
            //element is the volume. For the platform path to be valid in
            //Windows and MacOS, an additional volume separator char needs
            //to be added after the volume letter/name and the rest of the path
            if (string.IsNullOrWhiteSpace(root) &&
                Path.VolumeSeparatorChar != Path.DirectorySeparatorChar)
            {
                int psIndex = convertedPath.IndexOf(
                    Path.DirectorySeparatorChar, 1);
                string pathVolume = convertedPath.Substring(1, psIndex - 1);
                string pathTail = convertedPath.Substring(psIndex,
                    convertedPath.Length - psIndex);
                return pathVolume + Path.VolumeSeparatorChar + pathTail;
            }
            else return Path.Combine(root, convertedPath);
        }

        /// <summary>
        /// Converts a rooted platform path in the format of the current
        /// operating system into an absolute <see cref="FileSystemPath"/>.
        /// </summary>
        /// <param name="platformPath">
        /// The platform path to be converted.
        /// </param>
        /// <returns>A new <see cref="FileSystemPath"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="platformPath"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="platformPath"/> is not rooted.
        /// </exception>
        private static FileSystemPath GetFileSystemPath(string platformPath)
        {
            if (platformPath == null)
                throw new ArgumentNullException(nameof(platformPath));
            if (!Path.IsPathRooted(platformPath))
                throw new ArgumentException("The specified path is not " +
                    "rooted and can't be used!");

            string fsPath = platformPath.Replace(Path.DirectorySeparatorChar,
                FileSystemPath.SeparatorPathElement).Replace(
                Path.VolumeSeparatorChar.ToString(), "");

            return new FileSystemPath(fsPath, true);
        }

        /// <summary>
        /// Performs tasks associated with freeing, releasing, or resetting 
        /// unmanaged resources. This method has no effect in this 
        /// implementation of the <see cref="IFileSystem"/> interface.
        /// </summary>
        public void Dispose()
        {
            return;
        }
    }
}
