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

namespace ShamanTK
{
    /// <summary>
    /// Provides functionality to initialize the platform components used for
    /// a <see cref="ShamanApp"/>.
    /// </summary>
    public interface IPlatformProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformComponents"/>
        /// class.
        /// </summary>
        /// <returns>
        /// A new instance of the <see cref="PlatformComponents"/> class.
        /// </returns>
        /// <exception cref="System.SystemException">
        /// Is thrown when a platform-specific exception occurred which
        /// prevents the <see cref="PlatformComponents"/> instance to be
        /// initialized (e.g. missing libraries, driver issues, etc.).
        /// </exception>
        PlatformComponents Initialize();
    }
}
