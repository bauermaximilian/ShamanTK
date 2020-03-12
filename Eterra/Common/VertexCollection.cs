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
    /// Represents a fixed-size collection of <see cref="Vertex"/> instances.
    /// </summary>
    public class VertexCollection : IReadOnlyList<Vertex>
    {
        /// <summary>
        /// Gets or sets a <see cref="Vertex"/> in the current 
        /// <see cref="VertexCollection"/> instance.
        /// </summary>
        /// <param name="index">
        /// The index of the <see cref="Vertex"/> to be retrieved or modified.
        /// </param>
        /// <returns>The requested <see cref="Vertex"/> instance.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Is thrown when <paramref name="index"/> is less than 0 or
        /// greater than/equal to <see cref="Count"/>.
        /// </exception>
        public Vertex this[int index]
        {
            get => BaseData[index];
            set => BaseData[index] = value;
        }

        /// <summary>
        /// Gets the amount of <see cref="Vertex"/> instances the current
        /// <see cref="VertexCollection"/> holds.
        /// </summary>
        public int Count => BaseData.Length;

        /// <summary>
        /// Gets the base array, which contains the data of this
        /// <see cref="VertexCollection"/>.
        /// </summary>
        internal Vertex[] BaseData { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexCollection"/> 
        /// class.
        /// </summary>
        /// <param name="vertices">
        /// An array of <see cref="Vertex"/> instances to be used as base for 
        /// the current <see cref="VertexCollection"/>. Can be modified by the
        /// setter of <see cref="this[int]"/> if <paramref name="cloneSource"/>
        /// is <c>false</c>.
        /// </param>
        /// <param name="cloneSource">
        /// <c>true</c> to clone the <paramref name="vertices"/> array and 
        /// prevent modification of the base array through the setter of 
        /// <see cref="this[int]"/> or the modification of the 
        /// <see cref="Vertex"/> instances in the new 
        /// <see cref="VertexCollection"/> through changes in the 
        /// <paramref name="vertices"/> instance, or <c>false</c> to use the
        /// specified array instance and create a new 
        /// <see cref="VertexCollection"/> as "wrapper" around the 
        /// existing array.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="vertices"/> is null.
        /// </exception>
        public VertexCollection(Vertex[] vertices, bool cloneSource)
        {
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));

            if (cloneSource)
            {
                BaseData = new Vertex[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                    BaseData[i] = vertices[i];
            }
            else BaseData = vertices;
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{Vertex}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="IEnumerator{Vertex}"/> instance.
        /// </returns>
        public IEnumerator<Vertex> GetEnumerator()
        {
            return (IEnumerator<Vertex>)BaseData.GetEnumerator();
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator"/> instance.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return BaseData.GetEnumerator();
        }
    }
}
