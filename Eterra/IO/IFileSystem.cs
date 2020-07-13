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

using System;
using System.Collections.Generic;
using System.IO;

namespace Eterra.IO
{
    /// <summary>
    /// Provides functionality to read and modify files.
    /// </summary>
    public interface IFileSystem : IDisposable
    {
        /// <summary>
        /// Gets a boolean which indicates whether the current file system
        /// supports creating, modifying or removing files or directories 
        /// (<c>true</c>) or not (<c>false</c>). Regardless of this properties 
        /// value, however, access to certain files can still be denied,
        /// depending on the requested action and restrictions by the OS.
        /// </summary>
        bool IsWritable { get; }

        /// <summary>
        /// Opens an existing file.
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
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current file system is disposed and can't be
        /// used anymore.
        /// </exception>
        Stream OpenFile(FileSystemPath filePath, bool requestWriteAccess);

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
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current file system is disposed and can't be
        /// used anymore.
        /// </exception>
        bool ExistsFile(FileSystemPath filePath);

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
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current file system is disposed and can't be
        /// used anymore.
        /// </exception>
        bool ExistsDirectory(FileSystemPath directoryPath);

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
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current file system is disposed and can't be
        /// used anymore.
        /// </exception>
        IEnumerable<FileSystemPath> Enumerate(FileSystemPath directoryPath);

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
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current file system is disposed and can't be
        /// used anymore.
        /// </exception>
        void CreateDirectory(FileSystemPath directoryPath);

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
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current file system is disposed and can't be
        /// used anymore.
        /// </exception>
        Stream CreateFile(FileSystemPath filePath, bool overwrite);

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
        void Delete(FileSystemPath path, bool recursive);
    }
}
