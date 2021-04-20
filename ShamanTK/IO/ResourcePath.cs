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

namespace ShamanTK.IO
{
    /// <summary>
    /// Represents a path to a resource, such as a <see cref="MeshData"/> or
    /// <see cref="TextureData"/>, which can be imported with a
    /// <see cref="ResourceManager"/>.
    /// </summary>
    public readonly struct ResourcePath : IEquatable<ResourcePath>
    {
        /// <summary>
        /// Defines the character which separates the <see cref="Path"/>
        /// from the <see cref="Query"/> in the <see cref="PathString"/>.
        /// This character may only occur once in a valid 
        /// <see cref="PathString"/>.
        /// </summary>
        public const char ElementSeparator = '?';

        /// <summary>
        /// Gets an empty (relative) <see cref="ResourcePath"/> instance.
        /// </summary>
        public static ResourcePath Empty { get; }
            = new ResourcePath(FileSystemPath.Empty, ResourceQuery.Empty);

        /// <summary>
        /// Gets the path to the resource file. Is always formatted as file
        /// (not directory) path, but may be empty or relative.
        /// </summary>
        public FileSystemPath Path { get; }

        /// <summary>
        /// Gets the name of the resource in the resource file.
        /// May be empty.
        /// </summary>
        public ResourceQuery Query { get; }

        /// <summary>
        /// Gets the full resource path as <see cref="string"/>.
        /// </summary>
        public string PathString => Path
                + (!Query.IsEmpty ? (ElementSeparator + Query) : "");

        /// <summary>
        /// Gets a boolean which indicates whether the current resource path 
        /// with both the <see cref="Path"/> and the <see cref="Query"/> is 
        /// empty (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsEmpty => Path.IsEmpty && Query.IsEmpty;

        /// <summary>
        /// Gets a boolean indicating whether the <see cref="Path"/> (and 
        /// therefore this <see cref="ResourcePath"/> instance) is absolute 
        /// and not empty (<c>true</c>) or not absolute or empty 
        /// (<c>false</c>).
        /// </summary>
        public bool IsAbsolute => Path.IsAbsolute;

        /// <summary>
        /// Initializes a new <see cref="ResourcePath"/> instance.
        /// </summary>
        /// <param name="resourcePathString">
        /// A valid <see cref="ResourcePath"/> string.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="resourcePathString"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the format of the 
        /// <paramref name="resourcePathString"/> is invalid or the 
        /// <see cref="FileSystemPath"/> part of it is a directory path.
        /// </exception>
        public ResourcePath(string resourcePathString)
        {
            if (resourcePathString == null)
                throw new ArgumentNullException(nameof(resourcePathString));

            resourcePathString = resourcePathString.Trim();

            int separatorIndex = resourcePathString.IndexOf(ElementSeparator);
            if (separatorIndex != resourcePathString.LastIndexOf(
                ElementSeparator))
                throw new ArgumentException("The path string contained " +
                    "multiple resource path element separators.");

            string fileSystemPathPart, resourceQueryPart;
            //If the element separator doesn't exist, the path string is 
            //interpreted as file system path without a query
            if (separatorIndex < 0)
            {
                fileSystemPathPart = resourcePathString;
                resourceQueryPart = string.Empty;
            }
            //If the element separator is the first character in the path
            //string, it's intepreted as a query without a file system path
            else if (separatorIndex == 0)
            {
                fileSystemPathPart = string.Empty;
                resourceQueryPart = resourcePathString.TrimStart(
                    ElementSeparator);
            }
            //Otherwise, the path string is split at the element separator -
            //the first part is the file system path, the second one the query
            else
            {
                fileSystemPathPart = resourcePathString.Substring(0, 
                    separatorIndex);
                resourceQueryPart = resourcePathString.Substring(
                    separatorIndex + 1, resourcePathString.Length 
                    - separatorIndex - 1);
            }

            try
            {
                Path = new FileSystemPath(fileSystemPathPart, false);
            }
            catch (ArgumentException exc)
            {
                throw new ArgumentException("The file system path " +
                    "part in the resource path string was invalid.",
                    exc);
            }

            try { Query = new ResourceQuery(resourceQueryPart, true); }
            catch (ArgumentException exc)
            {
                throw new ArgumentException("The resource query " +
                    "section in the resource path string was invalid.",
                    exc);
            }
        }

        /// <summary>
        /// Initializes a new <see cref="ResourcePath"/> instance.
        /// </summary>
        /// <param name="filePath">
        /// The path to the resource file.
        /// </param>
        /// <param name="query">
        /// The name of the resource in the resource file.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <paramref name="filePath"/> is a directory path.
        /// </exception>
        public ResourcePath(FileSystemPath filePath, ResourceQuery query)
        {
            Path = filePath;

            if (Path.IsDirectoryPath)
                throw new ArgumentException("The file system path " +
                    "references a directory, not a file.");

            Query = query;
        }

        /// <summary>
        /// Initializes a new <see cref="ResourcePath"/> instance
        /// with an empty <see cref="Query"/>.
        /// </summary>
        /// <param name="filePath">
        /// The path to the resource file.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <paramref name="filePath"/> is a directory path.
        /// </exception>
        public ResourcePath(FileSystemPath filePath)
        {
            Path = filePath;

            if (Path.IsDirectoryPath)
                throw new ArgumentException("The file system path " +
                    "references a directory, not a file.");

            Query = ResourceQuery.Empty;
        }

        /// <summary>
        /// Initializes a new <see cref="ResourcePath"/> instance
        /// with an empty <see cref="Path"/>.
        /// </summary>
        /// <param name="query">
        /// The name of the resource in the resource file.
        /// </param>
        public ResourcePath(ResourceQuery query)
        {
            Path = FileSystemPath.Empty;
            Query = query;
        }

        /// <summary>
        /// Converts the current relative resource path instance into an 
        /// resource path instance with an absolute <see cref="Path"/> and the 
        /// current <see cref="Query"/>. Requires the current 
        /// <see cref="Path"/> to be relative.
        /// </summary>
        /// <param name="pathRoot">
        /// An absolute <see cref="FileSystemPath"/> instance.
        /// If the instance is not a directory path, the 
        /// <see cref="FileSystemPath.GetParentDirectory"/> of the
        /// <paramref name="pathRoot"/> is used.
        /// </param>
        /// <returns>
        /// A new <see cref="ResourcePath"/> instance with an absolute
        /// <see cref="Path"/> and the <see cref="Query"/> of this instance.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="IsAbsolute"/> of 
        /// <paramref name="pathRoot"/> is <c>false</c> or 
        /// <see cref="IsFormattedAsDirectoryPath"/> of 
        /// <paramref name="pathRoot"/> are <c>false</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsAbsolute"/> of <see cref="Path"/>
        /// is <c>true</c>.
        /// </exception>
        public ResourcePath ToAbsolute(FileSystemPath pathRoot)
        {
            if (!pathRoot.IsAbsolute)
                throw new ArgumentException("The root file path must " +
                    "be absolute.");

            try
            {
                FileSystemPath absolutePath = Path.IsAbsolute ? Path : 
                    (Path.IsEmpty ? pathRoot : Path.ToAbsolute(pathRoot));
                return new ResourcePath(absolutePath, Query);
            }
            catch (Exception exc)
            {
                throw new ArgumentException("The current file system path " +
                    "can't be made absolute with the specified root " +
                    "resource path.", exc);
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return PathString;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current 
        /// object. 
        /// </summary>
        /// <param name="obj">
        /// The object to compare with the current object.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified object is equal to the current object;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is ResourcePath path && Equals(path);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object 
        /// of the same type. 
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <c>true</c> if the current object is equal to the 
        /// <paramref name="other"/> parameter; otherwise, <c>false</c>. 
        /// </returns>
        public bool Equals(ResourcePath other)
        {
            return PathString == other.PathString;
        }

        /// <summary>
        /// Calculates the hash of the current instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return -1287036097 + EqualityComparer<string>.Default.GetHashCode(
                PathString);
        }

        public static bool operator ==(ResourcePath path1, ResourcePath path2)
        {
            return path1.Equals(path2);
        }

        public static bool operator !=(ResourcePath path1, ResourcePath path2)
        {
            return !(path1 == path2);
        }

        public static implicit operator string(ResourcePath path)
        {
            return path.PathString;
        }

        public static implicit operator ResourcePath(FileSystemPath path)
        {
            return new ResourcePath(path);
        }

        public static implicit operator ResourcePath(string path)
        {
            return new ResourcePath(path);
        }
    }
}
