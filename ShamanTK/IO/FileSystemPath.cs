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
using System.Text.RegularExpressions;

namespace ShamanTK.IO
{
    /// <summary>
    /// Represents a path to a file or directory in 
    /// a <see cref="IFileSystem"/>.
    /// </summary>
    public readonly struct FileSystemPath : IEquatable<FileSystemPath>
    {
        private static readonly HashSet<char> invalidChars = new HashSet<char>
        {
            '?', '%', '*', ':', '|', '\"', '<', '>', 
        };
        private static readonly char lowestValidCharacter = ' ';

        /// <summary>
        /// Defines the character which separates the individual path elements.
        /// This character is mandatory as first character of an absolute path
        /// and as last character of a directory path.
        /// </summary>
        public const char SeparatorPathElement = '/';

        /// <summary>
        /// Defines an alternative equivalent to 
        /// <see cref="SeparatorPathElement"/>, which is replaced with the
        /// <see cref="SeparatorPathElement"/> during initialisation of a
        /// <see cref="FileSystemPath"/>.
        /// </summary>
        public const char SeparatorPathElementAlternative = '\\';

        /// <summary>
        /// Defines the character which separates the file name from its 
        /// extension.
        /// </summary>
        public const char SeparatorFileExtension = '.';

        /// <summary>
        /// Defines the token which specifies the parent directory in a 
        /// relative path.
        /// </summary>
        public const string DirectoryReferenceParent = "..";

        /// <summary>
        /// Defines the token which specifies the current directory in a
        /// relative path.
        /// </summary>
        public const string DirectoryReferenceCurrent = ".";

        //Is used to determine if a path contains relative reference tokens
        private readonly static Regex relativeReferencesFormat = new Regex(
            @"(\/|^)\.?\.\/|\/\.\.?(\/|$)|^\.\.$|^\.$");

        //The position of these two definitions below is important -
        //the static constructor may fail if the properties are rearranged
        /// <summary>
        /// Gets an empty (relative) <see cref="FileSystemPath"/> instance.
        /// </summary>
        public static FileSystemPath Empty { get; }
            = new FileSystemPath(string.Empty);

        /// <summary>
        /// Gets a absolute <see cref="FileSystemPath"/> instance which 
        /// references the root directory.
        /// </summary>
        public static FileSystemPath Root { get; }
            = new FileSystemPath(SeparatorPathElement.ToString());

        /// <summary>
        /// Gets the string representation of the current 
        /// <see cref="FileSystemPath"/> instance. 
        /// To retrieve a string with all encoded characters decoded, use 
        /// <see cref="GetDecodedPathString"/>.
        /// </summary>
        public string PathString => pathString ?? "";
        private readonly string pathString;

        /// <summary>
        /// Gets a boolean which indicates whether the current path is empty
        /// (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsEmpty => PathString.Length == 0;

        /// <summary>
        /// Gets a boolean which indicates whether the current path is a
        /// non-empty, absolute, distinct path, starting with the 
        /// <see cref="SeparatorPathElement"/> and not containing any 
        /// occurences of a <see cref="DirectoryReferenceParent"/> or
        /// a <see cref="DirectoryReferenceCurrent"/> (<c>true</c>) or if
        /// the path is ambiguous (or empty) and may need to be made absolute
        /// with <see cref="ToAbsolute(FileSystemPath)"/> to unambigiously 
        /// refer to a file (<c>false</c>).
        /// </summary>
        public bool IsAbsolute => !IsEmpty 
            && (PathString[0] == SeparatorPathElement)
            && !ContainsRelativeReferences;

        /// <summary>
        /// Gets a boolean which indicates whether the current path ends with
        /// a <see cref="SeparatorPathElement"/>, a 
        /// <see cref="DirectoryReferenceCurrent"/> or a
        /// <see cref="DirectoryReferenceParent"/> and is therefore considered
        /// as a directory path (<c>true</c>) or not (<c>false</c>). This does
        /// not necessarily match with the actual situation in the file system 
        /// this path is used in - see the documentation for more information.
        /// </summary>
        /// <remarks>
        /// As files do not necessarily have a file extension and folder names
        /// can usually contain a <see cref="SeparatorFileExtension"/>, the 
        /// only way to determine the value of this property (without using a 
        /// file system) is checking if the path ends with a 
        /// <see cref="SeparatorPathElement"/>. This is required for consistent
        /// behaviour of other methods in this struct, but it needs to be 
        /// understood that a path might refer to an existing and valid
        /// directory, but isn't recognized as such by the file system
        /// if that final character is omitted.
        /// </remarks>
        public bool IsDirectoryPath => (PathString.Length > 0 && 
            (PathString[^1] == SeparatorPathElement)) || 
            PathString.EndsWith(DirectoryReferenceCurrent)
            || PathString.EndsWith(DirectoryReferenceParent);

        /// <summary>
        /// Gets a boolean which indicates if the current 
        /// <see cref="FileSystemPath"/> uses one or more 
        /// <see cref="DirectoryReferenceCurrent"/> or 
        /// <see cref="DirectoryReferenceParent"/> as path elements and 
        /// therefore can't be absolute (<c>true</c>) or if no such references
        /// are used (<c>false</c>).
        /// </summary>
        private bool ContainsRelativeReferences =>
            relativeReferencesFormat.IsMatch(PathString);

        /// <summary>
        /// Gets a boolean which indicates whether the current path is only
        /// a relative reference (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        private bool IsRelativeReference
        {
            get
            {
                string trimmedPath = PathString.Trim(SeparatorPathElement);
                return trimmedPath == DirectoryReferenceCurrent ||
                    trimmedPath == DirectoryReferenceParent;
            }
        }

        /// <summary>
        /// Gets the amount of parent directories of the referenced file
        /// or directory.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Initializes a new <see cref="FileSystemPath"/> instance.
        /// </summary>
        /// <param name="pathString">
        /// The string of the path. Unsupported characters are encoded.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="pathString"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="pathString"/> contains one or more
        /// invalid characters.
        /// </exception>
        public FileSystemPath(string pathString)
            : this(pathString, true) { }

        /// <summary>
        /// Initializes a new <see cref="FileSystemPath"/> instance.
        /// </summary>
        /// <param name="pathString">
        /// The string of the path. Unsupported characters are encoded.
        /// </param>
        /// <param name="allowDirectoryPath">
        /// <c>true</c> to allow the <paramref name="pathString"/> to be
        /// formatted as directory path (and therefore referencing a directory
        /// instead of a file), <c>false</c> to throw an 
        /// <see cref="ArgumentException"/> in that case instead.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="pathString"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="pathString"/> contains one or more
        /// invalid characters or when <paramref name="allowDirectoryPath"/> 
        /// is <c>false</c> but <paramref name="pathString"/> is a 
        /// directory path.
        /// </exception>
        public FileSystemPath(string pathString,
            bool allowDirectoryPath) : this()
        {
            if (pathString == null)
                throw new ArgumentNullException(nameof(pathString));

            string trimmedValue = pathString.Trim();

            if (!ContainsInvalidCharacters(pathString))
                this.pathString = trimmedValue;
            else throw new ArgumentException("The specified path string " +
                    "contains invalid characters.");

            if (!allowDirectoryPath && IsDirectoryPath)
                throw new ArgumentException("The path string defined a " +
                    "directory path, which is invalid in the given " +
                    "context.");

            Depth = GetDepth();
        }

        /// <summary>
        /// Checks whether a query string candidate contains invalid characters
        /// that would prevent creating a <see cref="ResourceQuery"/> with it.
        /// This method is used by the <see cref="ResourceQuery"/> constructor.
        /// </summary>
        /// <param name="pathString">The path string to verify.</param>
        /// <returns>
        /// <c>true</c> if the string is valid and can be used to create a
        /// <see cref="ResourceQuery"/>, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="pathString"/> is null.
        /// </exception>
        public static bool ContainsInvalidCharacters(string pathString)
        {
            if (pathString == null)
                throw new ArgumentNullException(nameof(pathString));

            foreach (char c in pathString)
            {
                if (c < lowestValidCharacter || invalidChars.Contains(c))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the index of the path element separator, which separates the
        /// path from its last element (e.g. the file path from the file name,
        /// the directory path from the directory name). If the path is
        /// <see cref="Root"/>, this method returns 0. If <see cref="IsEmpty"/>
        /// is true, this method returns -1.
        /// </summary>
        /// <returns>
        /// The zero-based index of the path element separator as 
        /// <see cref="int"/> or -1, if <see cref="IsEmpty"/> is <c>true</c>.
        /// </returns>
        private int GetFinalSeparatorIndex()
        {
            if (IsEmpty) return -1;
            else return PathString.LastIndexOf(SeparatorPathElement,
                PathString.Length -
                ((IsDirectoryPath && PathString.Length > 1)
                ? 2 : 1));
        }

        /// <summary>
        /// Gets the amount of parent directories of the referenced file
        /// or directory.
        /// </summary>
        private int GetDepth()
        {
            int depthRaw = 0;
            for (int i = 0; i < PathString.Length; i++)
                if (PathString[i] == SeparatorPathElement) depthRaw++;
            return IsDirectoryPath ? (depthRaw - 1) : depthRaw;
        }

        /// <summary>
        /// Gets the name of the file without its preceding path information.
        /// Requires a non-empty file path.
        /// </summary>
        /// <param name="excludeFileExtension">
        /// <c>true</c> to exclude the file extension (and the 
        /// <see cref="SeparatorFileExtension"/>) from the returned string,
        /// <c>false</c> to return the complete file name including its
        /// extension.
        /// </param>
        /// <returns>A new <see cref="string"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsEmpty"/> or 
        /// <see cref="IsDirectoryPath"/> is <c>true</c>.
        /// </exception>
        public string GetFileName(bool excludeFileExtension)
        {
            if (IsEmpty) throw new InvalidOperationException("The current " +
                "path is empty!");
            if (IsDirectoryPath)
                throw new InvalidOperationException("The current path " +
                    "references a directory, not a file!");

            int lastSeparatorIndex = GetFinalSeparatorIndex();
            string fileName = PathString.Substring(lastSeparatorIndex + 1,
                PathString.Length - lastSeparatorIndex - 1);

            if (excludeFileExtension)
            {
                if (IsRelativeReference) return fileName;

                int extensionSeparatorIndex = fileName.IndexOf(
                    SeparatorFileExtension);
                if (fileName.Length > 0 && extensionSeparatorIndex > 0
                    && extensionSeparatorIndex < (fileName.Length - 1))
                    return fileName.Substring(0, extensionSeparatorIndex);
                else return fileName;
            }
            else return fileName;
        }

        /// <summary>
        /// Gets the extension of the file without the preceding path 
        /// (the directory, file name and the the 
        /// <see cref="SeparatorFileExtension"/>) as lowercase string 
        /// or <see cref="string.Empty"/>, if the file has no extension.
        /// Requires a non-empty file path.
        /// </summary>
        /// <returns>A new <see cref="string"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsEmpty"/> or 
        /// <see cref="IsDirectoryPath"/> are <c>true</c>.
        /// </exception>
        public string GetFileExtension()
        {
            string fileName;
            try { fileName = GetFileName(false); }
            catch (InvalidOperationException) { throw; }

            int separatorIndex = fileName.LastIndexOf(SeparatorFileExtension);
            if (fileName.Length > 0 && separatorIndex >= 0
                && separatorIndex < (fileName.Length - 1))
                return fileName.Substring(separatorIndex + 1,
                        fileName.Length - separatorIndex - 1)
                        .ToLowerInvariant();
            else return string.Empty;
        }

        /// <summary>
        /// Gets the name of the directory without its preceding path 
        /// information or <see cref="string.Empty"/>, if the current path is
        /// <see cref="Root"/>. Requires a non-empty directory path with a
        /// depth greater than 0.
        /// </summary>
        /// <returns>A new <see cref="string"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsEmpty"/> is <c>true</c>,
        /// <see cref="IsDirectoryPath"/> is <c>false</c> or when 
        /// <see cref="Depth"/> is less than 1 (which indicates that the 
        /// file or folder has no parent directories).
        /// </exception>
        public string GetDirectoryName()
        {
            if (IsEmpty) throw new InvalidOperationException("The current " +
                "path is empty!");
            if (Depth < 1) throw new InvalidOperationException("The depth " +
                "of the current path is 0!");
            if (!IsDirectoryPath)
                throw new InvalidOperationException("The current path " +
                    "references a file, not a dictionary!");

            int lastSeparatorIndex = GetFinalSeparatorIndex();

            if (lastSeparatorIndex > 0)
                return PathString.Substring(lastSeparatorIndex + 1,
                    PathString.Length - lastSeparatorIndex - 1);
            else return string.Empty;
        }

        /// <summary>
        /// Gets the path of the directory in which the file (or 
        /// folder, if <see cref="IsDirectoryPath"/> is <c>true</c>)
        /// resides in as directory path (without the file name).
        /// Requires a non-empty path with a non-zero depth.
        /// </summary>
        /// <returns>A new <see cref="FileSystemPath"/> instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsEmpty"/> is <c>true</c> or when 
        /// <see cref="Depth"/> is less than 1 (which indicates that the 
        /// file or folder has no preceding path).
        /// </exception>
        public FileSystemPath GetParentDirectory()
        {
            if (IsEmpty) throw new InvalidOperationException("The current " +
                "path is empty!");
            if (Depth < 1) throw new InvalidOperationException("The depth " +
                "of the current path is 0!");

            int lastSeparatorIndex = GetFinalSeparatorIndex();
            return new FileSystemPath(PathString.Substring(0,
                    lastSeparatorIndex + 1));
        }

        /// <summary>
        /// Converts the current relative path instance into an absolute path, 
        /// using another absolute path the current is relative to.
        /// Requires the current path to be relative and not empty.
        /// </summary>
        /// <param name="rootPath">
        /// An absolute, non-empty path.
        /// </param>
        /// <returns>
        /// A new, absolute <see cref="FileSystemPath"/> instance.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="IsAbsolute"/> of 
        /// <paramref name="rootPath"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsAbsolute"/> is <c>true</c>, or when
        /// <see cref="IsEmpty"/> is <c>true</c>.
        /// </exception>
        public FileSystemPath ToAbsolute(FileSystemPath rootPath)
        {
            if (IsAbsolute) throw new InvalidOperationException("This path " +
                "is already absolute!");
            if (IsEmpty) throw new InvalidOperationException("This path " +
                "is empty and can't be made absolute.");
            if (rootPath.IsEmpty) throw new ArgumentException("The " +
                "specified root path is empty!");
            if (!rootPath.IsAbsolute) throw new ArgumentException(
                "The specified root path is not absolute!");

            if (!rootPath.IsDirectoryPath)
                rootPath = rootPath.GetParentDirectory();

            return Combine(rootPath, this);
        }

        /// <summary>
        /// Combines two or more <see cref="FileSystemPath"/> instances to
        /// one <see cref="FileSystemPath"/> instance.
        /// </summary>
        /// <param name="pathElements">
        /// A collection of <see cref="FileSystemPath"/> instances, where 
        /// absolute paths can only be used as first element, non-directory 
        /// paths can only be used as last element. Empty paths are ignored.
        /// </param>
        /// <returns>A new <see cref="FileSystemPath"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="pathElements"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when less than 2 path elements are specified, 
        /// an absolute <see cref="FileSystemPath"/> was specified as anything
        /// but the first parameter or a non-directory 
        /// <see cref="FileSystemPath"/> was specified as anything but the
        /// last parameter.
        /// </exception>
        public static FileSystemPath Combine(
            params FileSystemPath[] pathElements)
        {
            if (pathElements == null)
                throw new ArgumentNullException(nameof(pathElements));
            if (pathElements.Length < 2)
                throw new ArgumentException("At least 2 elements must be " +
                    "specified!");

            using StringWriter writer = new StringWriter();
            
            for (int i = 0; i < pathElements.Length; i++)
            {
                FileSystemPath pathElement = pathElements[i];
                if (pathElement.IsEmpty) continue;
                if (pathElement.IsAbsolute && i > 0)
                    throw new ArgumentException("Path element #" + i +
                        " was absolute - only the first element can be " +
                        "absolute.");
                if (!pathElement.IsDirectoryPath
                    && i != (pathElements.Length - 1))
                    throw new ArgumentException("Path element #" + i +
                        " was no directory path - only the last element " +
                        "is allowed to be a file path!");
                writer.Write(pathElement.PathString);
            }

            return new FileSystemPath(writer.ToString());
        }

        /// <summary>
        /// Combines a file name and an extension to a relative 
        /// <see cref="FileSystemPath"/> instance.
        /// </summary>
        /// <param name="fileName">The raw file name.</param>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fileName"/> or 
        /// <paramref name="fileExtension"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="pathString"/> contains one or more
        /// invalid characters.
        /// </exception>
        public static FileSystemPath CombineFileName(string fileName,
            string fileExtension)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            if (fileExtension == null)
                throw new ArgumentNullException(nameof(fileExtension));

            return new FileSystemPath(fileName.TrimEnd(SeparatorFileExtension) 
                + SeparatorFileExtension +
                fileExtension.TrimStart(SeparatorFileExtension));
        }

        /// <summary>
        /// Checks whether another (absolute) path is hierarchically directly 
        /// related as child to the current (absolute, non-empty) path.
        /// </summary>
        /// <example>
        /// The path "/directory/" would be a parent of "/directory/file.txt",
        /// just like any file would be a child of "/" (root).
        /// "/directory/subdirectory/" would be a child of "/directory/",
        /// "/file.txt" wouldn't.
        /// If <paramref name="caseSensitive"/> is <c>true</c>,
        /// "/Directory/test.txt" wouldn't be a child of "/directory/",
        /// whereas it would be if <paramref name="caseSensitive"/> was
        /// <c>false</c>.
        /// </example>
        /// <param name="childPath">
        /// The path of the file or directory which should be checked if it's
        /// subordinate to the current path.
        /// </param>
        /// <param name="caseSensitive">
        /// <c>true</c> to perform the check with 
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="childPath"/> is a child element of
        /// the current path, <c>false</c> if it's no child element of the
        /// current path or <see cref="IsEmpty"/> of 
        /// <paramref name="childPath"/> is <c>true</c>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="IsAbsolute"/> of
        /// <paramref name="childPath"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsEmpty"/> is <c>true</c> or when
        /// <see cref="IsDirectoryPath"/> or 
        /// <see cref="IsAbsolute"/> are <c>false</c>.
        /// </exception>
        public bool IsParentOf(FileSystemPath childPath, bool caseSensitive)
        {
            if (!childPath.IsAbsolute)
                throw new ArgumentException("The child path is not absolute.");

            try { Verify(true); }
            catch (Exception exc)
            {
                throw new InvalidOperationException(exc.Message);
            }

            if (caseSensitive)
                return childPath.PathString.StartsWith(PathString);
            else
                return childPath.PathString.ToLowerInvariant().StartsWith(
                    PathString.ToLowerInvariant());
        }

        /// <summary>
        /// Verifies the current path for a few common required conditions and
        /// throws an exception if one of them is not met. If all requirements
        /// are met, this method has no effect.
        /// </summary>
        /// <param name="requireDirectoryPath">
        /// <c>true</c> to require the current instance to be formatted as a
        /// directory path (<see cref="IsDirectoryPath"/> must be
        /// <c>true</c> then), <c>false</c> to require a file path
        /// (<see cref="IsDirectoryPath"/> must be <c>false</c>
        /// then).
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="IsEmpty"/> of the path is <c>true</c>, 
        /// <see cref="IsAbsolute"/> of the path is <c>false</c>,
        /// when <paramref name="requireDirectoryPath"/> is <c>true</c> but
        /// <see cref="IsDirectoryPath"/> of the path is 
        /// <c>false</c> or when <paramref name="requireDirectoryPath"/> is 
        /// <c>false</c> but <see cref="IsDirectoryPath"/> 
        /// of the path is <c>true</c>.
        /// </exception>
        public void Verify(bool requireDirectoryPath)
        {
            try { Verify(requireDirectoryPath, !requireDirectoryPath); }
            catch (ArgumentException) { throw; }
        }

        /// <summary>
        /// Verifies the current path for a few common required conditions and
        /// throws an exception if one of them is not met. If all requirements
        /// are met, this method has no effect.
        /// </summary>
        /// <param name="allowDirectoryPath">
        /// <c>true</c> to allow the current instance to be formatted as a
        /// directory path (<see cref="IsDirectoryPath"/> can be
        /// <c>true</c> then), <c>false</c> to forbid this.
        /// </param>
        /// <param name="allowFilePath">
        /// <c>true</c> to allow the current instance to be formatted as a file
        /// path (<see cref="IsDirectoryPath"/> can be 
        /// <c>false</c> then), <c>false</c> to forbid this.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="IsEmpty"/> of the path is <c>true</c>, 
        /// when <see cref="IsAbsolute"/> of the path is <c>false</c>, when 
        /// <paramref name="allowDirectoryPath"/> is <c>false</c> but 
        /// <see cref="IsDirectoryPath"/> of the path is <c>true</c>
        /// or when <paramref name="allowFilePath"/> is <c>false</c> but 
        /// <see cref="IsDirectoryPath"/> of the path 
        /// is <c>false</c>.
        /// </exception>
        public void Verify(bool allowDirectoryPath, bool allowFilePath)
        {
            try { Verify(allowDirectoryPath, allowFilePath, false); }
            catch (ArgumentException) { throw; }
        }

        /// <summary>
        /// Verifies the current path for a few common required conditions and
        /// throws an exception if one of them is not met. If all requirements
        /// are met, this method has no effect.
        /// </summary>
        /// <param name="allowDirectoryPath">
        /// <c>true</c> to allow the current instance to be formatted as a
        /// directory path (<see cref="IsDirectoryPath"/> can be
        /// <c>true</c> then), <c>false</c> to forbid this.
        /// </param>
        /// <param name="allowFilePath">
        /// <c>true</c> to allow the current instance to be formatted as a file
        /// path (<see cref="IsDirectoryPath"/> can be 
        /// <c>false</c> then), <c>false</c> to forbid this.
        /// </param>
        /// <param name="allowRelativePath">
        /// <c>true</c> to allow a non-absolute path (<see cref="IsAbsolute"/>
        /// can be <c>false</c> then), <c>false</c> to forbid this.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="IsEmpty"/> of the path is <c>true</c>, 
        /// when <paramref name="allowDirectoryPath"/> is <c>false</c> but 
        /// <see cref="IsDirectoryPath"/> of the path is 
        /// <c>true</c>, when <paramref name="allowFilePath"/> is <c>false</c> 
        /// but <see cref="IsDirectoryPath"/> of the path is 
        /// <c>false</c> or when <paramref name="allowRelativePath"/> is 
        /// <c>false</c> but <see cref="IsAbsolute"/> of the path is 
        /// <c>false</c>.
        /// </exception>
        public void Verify(bool allowDirectoryPath, bool allowFilePath,
            bool allowRelativePath)
        {
            if (IsEmpty) throw new ArgumentException("The path must not " +
                "be empty!");
            if (!allowRelativePath && !IsAbsolute) 
                throw new ArgumentException("The path must be absolute!");
            if (IsDirectoryPath && !allowDirectoryPath)
                throw new ArgumentException("The path must not be a " +
                    "directory path!");
            if (!IsDirectoryPath && !allowFilePath)
                throw new ArgumentException("The path must not be a " +
                    "file path!");
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
            return obj is FileSystemPath path && Equals(path);
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
        public bool Equals(FileSystemPath other)
        {
            return PathString == other.PathString;
        }

        /// <summary>
        /// Calculates the hash of the current instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return -401909281 + EqualityComparer<string>.Default.GetHashCode(
                PathString);
        }

        public static bool operator ==(FileSystemPath path1,
            FileSystemPath path2)
        {
            return path1.Equals(path2);
        }

        public static bool operator !=(FileSystemPath path1,
            FileSystemPath path2)
        {
            return !(path1 == path2);
        }

        public static implicit operator string(FileSystemPath path)
        {
            return path.PathString;
        }

        public static implicit operator FileSystemPath(ResourcePath path)
        {
            return path.Path;
        }

        public static implicit operator FileSystemPath(string path)
        {
            return new FileSystemPath(path ?? "");
        }
    }
}
