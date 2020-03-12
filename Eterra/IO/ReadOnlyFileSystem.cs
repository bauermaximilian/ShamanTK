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
    /// Provides extension methods for all <see cref="IFileSystem"/> 
    /// implementations.
    /// </summary>
    public static class FileSystemExtension
    {
        /// <summary>
        /// Creates a read-only wrapper around the current file system.
        /// </summary>
        /// <param name="fs">
        /// The base file system.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="ReadOnlyFileSystem"/> class.
        /// </returns>
        public static ReadOnlyFileSystem ToReadOnly(this IFileSystem fs)
        {
            try { return new ReadOnlyFileSystem(fs); }
            catch (ArgumentNullException)
            { throw new ArgumentNullException(nameof(fs)); }
        }
    }

    /// <summary>
    /// Provides a read-only wrapper for a <see cref="IFileSystem"/> instance.
    /// </summary>
    public class ReadOnlyFileSystem : IFileSystem
    {
        private class ReadOnlyStream : Stream
        {
            public override bool CanRead => baseStream.CanRead;

            public override bool CanSeek => baseStream.CanSeek;

            public override bool CanWrite => false;

            public override long Length => baseStream.Length;

            public override bool CanTimeout => baseStream.CanTimeout;

            public override int WriteTimeout
            {
                get => baseStream.WriteTimeout;
                set => baseStream.WriteTimeout = value;
            }

            public override int ReadTimeout
            {
                get => baseStream.ReadTimeout;
                set => baseStream.ReadTimeout = value;
            }

            public override long Position
            {
                get => baseStream.Position;
                set => baseStream.Position = value;
            }

            private readonly Stream baseStream;

            public ReadOnlyStream(Stream baseStream)
            {
                this.baseStream = baseStream ??
                    throw new ArgumentNullException(nameof(baseStream));
            }

            public override void Flush()
            {
                baseStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return baseStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return baseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException("The stream does not support" +
                    "writing.");
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException("The stream does not support" +
                    "writing.");
            }

            public override void Close()
            {
                baseStream.Close();
            }
        }

        /// <summary>
        /// Gets a boolean which indicates whether the current file system
        /// supports creating, modifying or removing files or directories 
        /// (<c>true</c>) or not (<c>false</c>).
        /// In this implementation of <see cref="IFileSystem"/>, this value
        /// is always <c>false</c>.
        /// </summary>
        public bool IsWritable => false;

        private readonly IFileSystem baseFileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyFileSystem"/>
        /// wrapper class around an existing <see cref="IFileSystem"/>
        /// instance.
        /// </summary>
        /// <param name="baseFileSystem">
        /// The base <see cref="IFileSystem"/> instance, for which a 
        /// read-only wrapper should be created.
        /// </param>
        public ReadOnlyFileSystem(IFileSystem baseFileSystem)
        {
            this.baseFileSystem = baseFileSystem ??
                throw new ArgumentNullException(nameof(baseFileSystem));
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
        /// If this parameter is <c>true</c>, the method will always
        /// throw a <see cref="NotSupportedException"/>.
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
        public Stream OpenFile(FileSystemPath filePath,
            bool requestWriteAccess)
        {
            if (requestWriteAccess)
                throw FileSystem.WritingNotSupportedException;

            Stream stream = baseFileSystem.OpenFile(filePath, false);
            if (stream == null)
                throw new IOException("The base file system returned a " +
                    "null stream.");
            if (stream.CanWrite)
                return CreateReadOnlyStream(stream);
            else return stream;
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
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current file system is disposed and can't be
        /// used anymore.
        /// </exception>
        public bool ExistsFile(FileSystemPath filePath)
        {
            return baseFileSystem.ExistsFile(filePath);
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
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current file system is disposed and can't be
        /// used anymore.
        /// </exception>
        public bool ExistsDirectory(FileSystemPath directoryPath)
        {
            return baseFileSystem.ExistsDirectory(directoryPath);
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
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current file system is disposed and can't be
        /// used anymore.
        /// </exception>
        public IEnumerable<FileSystemPath> Enumerate(
            FileSystemPath directoryPath)
        {
            return baseFileSystem.Enumerate(directoryPath);
        }      

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <param name="directoryPath">
        /// An absolute <see cref="FileSystemPath"/> instance (formatted as
        /// directory path) specifying the name and path of the new directory.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// Is always thrown when this method is called, as
        /// <see cref="IsWritable"/> is always <c>false</c>.
        /// </exception>
        public void CreateDirectory(FileSystemPath directoryPath)
        {
            throw FileSystem.WritingNotSupportedException;
        }

        /// <summary>
        /// Create a new file and the directory it's contained in, if not 
        /// already existant. This method is not supported by this 
        /// <see cref="IFileSystem"/> implementation and will always throw a
        /// <see cref="NotSupportedException"/>.
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
        /// <exception cref="NotSupportedException">
        /// Is always thrown when this method is called, as
        /// <see cref="IsWritable"/> is always <c>false</c>.
        /// </exception>
        public Stream CreateFile(FileSystemPath filePath, bool overwrite)
        {
            throw FileSystem.WritingNotSupportedException;
        }

        /// <summary>
        /// Deletes a file or directory.
        /// This method is not supported by this <see cref="IFileSystem"/>
        /// implementation and will always throw a
        /// <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="path">
        /// An absolute <see cref="FileSystemPath"/> specifying the file
        /// or directory to be removed.
        /// </param>
        /// <param name="recursive">
        /// When <paramref name="path"/> defines a directory, <c>true</c> 
        /// will remove the directory and all files and subdirectories 
        /// contained in the specified path. If this parameter is <c>false</c>,
        /// only files or empty directories are removed.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// Is always thrown when this method is called, as
        /// <see cref="IsWritable"/> is always <c>false</c>.
        /// </exception>
        public void Delete(FileSystemPath path, bool recursive)
        {
            throw FileSystem.WritingNotSupportedException;
        }

        /// <summary>
        /// Releases unmanaged resources used by this file system.
        /// </summary>
        public void Dispose()
        {
            baseFileSystem.Dispose();
        }

        /// <summary>
        /// Creates a read-only wrapper around a stream.
        /// </summary>
        /// <param name="baseStream">
        /// The base stream to be made read-only.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Stream"/>, encapsulating the
        /// existing stream.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="baseStream"/> is null.
        /// </exception>
        internal static Stream CreateReadOnlyStream(Stream baseStream)
        {
            try { return new ReadOnlyStream(baseStream); }
            catch(ArgumentNullException) { throw; }
        }
    }
}
