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

namespace ShamanTK.IO
{
    /// <summary>
    /// Defines methods to import and export a variety of resource formats.
    /// </summary>
    /// <remarks>
    /// - Implementing support for scenes:
    /// It is highly advised that every <see cref="Logic.Entity"/> in an 
    /// imported <see cref="Logic.Scene"/> contains the actual data resource
    /// as parameter already - it shouldn't be expected that the engine
    /// or the <see cref="Logic.IDirector"/> instance loads these resources
    /// if only a path is specified.
    /// - Implementing support for (sprite) fonts:
    /// Sprite fonts can either be directly loaded from sprite font formats
    /// (like bmfont) or converted from a normal font like (like ttf or otf).
    /// A <see cref="ResourceManager"/> provides a dedicated method to do the 
    /// latter with fonts installed on the users' system - to implement this 
    /// functionality, see the <see cref="Common.FontRasterizationParameters"/> class.
    /// </remarks>
    public interface IResourceFormatHandler
    {
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
        bool SupportsImport(string fileExtensionLowercase);

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
        bool SupportsExport(string fileExtensionLowercase);

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
        T Import<T>(ResourceManager manager, ResourcePath path);

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
        /// Is thrown when the specified <paramref name="path"/> was invalid in
        /// the current context.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when the file type specified by the file extension in
        /// <paramref name="path"/> is not supported.
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
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the <see cref="FileSystem"/> of the specified
        /// <paramref name="manager"/> was disposed and can no longer be used
        /// to import or export resource files.
        /// </exception>
        void Export(object resource, ResourceManager manager, 
            ResourcePath path, bool overwrite);
    }
}
