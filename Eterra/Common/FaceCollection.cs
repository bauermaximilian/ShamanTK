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
using System.Collections;
using System.Collections.Generic;

namespace Eterra.Common
{
    /// <summary>
    /// Represents a fixed-size collection of <see cref="Face"/> instances.
    /// </summary>
    public class FaceCollection : IReadOnlyList<Face>
    {
        /// <summary>
        /// Gets or sets a <see cref="Face"/> in the current 
        /// <see cref="FaceCollection"/> instance.
        /// </summary>
        /// <param name="index">
        /// The index of the <see cref="Face"/> to be retrieved or modified.
        /// </param>
        /// <returns>The requested <see cref="Face"/> instance.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Is thrown when <paramref name="index"/> is less than 0 or
        /// greater than/equal to <see cref="Count"/>.
        /// </exception>
        public Face this[int index]
        {
            get => BaseArray[index];
            set => BaseArray[index] = value;
        }

        /// <summary>
        /// Gets the amount of <see cref="Face"/> instances the current
        /// <see cref="FaceCollection"/> holds.
        /// </summary>
        public int Count => BaseArray.Length;

        /// <summary>
        /// Gets the base array, which contains the data of this
        /// <see cref="FaceCollection"/>.
        /// </summary>
        internal Face[] BaseArray { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FaceCollection"/> 
        /// class.
        /// </summary>
        /// <param name="faces">
        /// An array of <see cref="Face"/> instances to be used as base for the
        /// current <see cref="FaceCollection"/>. Can be modified by the
        /// setter of <see cref="this[int]"/> if <paramref name="cloneSource"/>
        /// is <c>false</c>.
        /// </param>
        /// <param name="cloneSource">
        /// <c>true</c> to clone the <paramref name="faces"/> array and prevent
        /// modification of the base array through the setter of 
        /// <see cref="this[int]"/> or the modification of the 
        /// <see cref="Face"/> instances in the new 
        /// <see cref="FaceCollection"/> through changes in the 
        /// <paramref name="faces"/> instance, or <c>false</c> to use the
        /// specified array instance and create a new 
        /// <see cref="FaceCollection"/> as "wrapper" around the 
        /// existing array.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="faces"/> is null.
        /// </exception>
        public FaceCollection(Face[] faces, bool cloneSource)
        {
            if (faces == null)
                throw new ArgumentNullException(nameof(faces));

            if (cloneSource)
            {
                BaseArray = new Face[faces.Length];
                for (int i = 0; i < faces.Length; i++)
                    BaseArray[i] = faces[i];
            }
            else BaseArray = faces;
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{Face}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{Face}"/> instance.</returns>
        public IEnumerator<Face> GetEnumerator()
        {
            return (IEnumerator<Face>)BaseArray.GetEnumerator();
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator"/> instance.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return BaseArray.GetEnumerator();
        }
    }
}
