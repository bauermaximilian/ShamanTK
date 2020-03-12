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
using System.Numerics;

namespace Eterra.Common
{
    /// <summary>
    /// Represents a read-only collection of <see cref="Matrix4x4"/> instances,
    /// which can be used to deform a mesh by applying each 
    /// <see cref="Matrix4x4"/> in this collection to every <see cref="Vertex"/>
    /// in the target mesh, which has the same deformer attachment index
    /// (element index in the <see cref="Deformer"/> instance) in its 
    /// <see cref="DeformerAttachments"/>.
    /// </summary>
    public class Deformer : IEnumerable<Matrix4x4>
    {
        /// <summary>
        /// Gets a <see cref="Deformer"/> instance with no elements.
        /// </summary>
        public static Deformer Empty { get; } = new Deformer(new Matrix4x4[0]);

        /// <summary>
        /// Defines the maximum amount of <see cref="Matrix4x4"/> instances
        /// one <see cref="Deformer"/> instance can hold.
        /// </summary>
        public const int MaximumSize = 128;

        /// <summary>
        /// Gets the amount of <see cref="Matrix4x4"/> instances in the current
        /// <see cref="Deformer"/> instance.
        /// </summary>
        public int Length => deformers.Length;

        /// <summary>
        /// Gets the reference of a single <see cref="Matrix4x4"/>
        /// from the current <see cref="Deformer"/> instance.
        /// </summary>
        /// <param name="deformerAttachment">
        /// The deformer attachment index of the <see cref="Matrix4x4"/> 
        /// deformer.
        /// </param>
        /// <returns>
        /// A reference to a <see cref="Matrix4x4"/> instance.
        /// </returns>
        public ref readonly Matrix4x4 this[int deformerAttachment]
        {
            get => ref deformers[deformerAttachment];
        }

        private readonly Matrix4x4[] deformers;

        /// <summary>
        /// Initializes a new instance of the <see cref="Deformer"/> class.
        /// </summary>
        /// <param name="deformers">
        /// An array of <see cref="Matrix4x4"/> instances.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="deformers"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the length of <paramref name="deformers"/> is 
        /// greater than <see cref="MaximumSize"/>.
        /// </exception>
        /// <exception cref="RankException">
        /// Is thrown when the rank of <paramref name="deformers"/> is not 1.
        /// </exception>
        private Deformer(Matrix4x4[] deformers)
        {
            VerifyDeformerArray(deformers);
            this.deformers = deformers;
        }

        /// <summary>
        /// Creates a new <see cref="Deformer"/> instance using a single
        /// array of <see cref="Matrix4x4"/>.
        /// </summary>
        /// <param name="deformers">
        /// An array of <see cref="Matrix4x4"/> instances.
        /// </param>
        /// <param name="cloneArray">
        /// <c>true</c> to clone the elements from <paramref name="deformers"/>
        /// into a new, internally stored structure, which uses more space and
        /// is slower, but prevents the data from being modified after the
        /// creation of this <see cref="Deformer"/> instance.
        /// <c>false</c> to just reference the <paramref name="deformers"/> in
        /// the new <see cref="Deformer"/> instance, with the risk of changes
        /// to the original <paramref name="deformers"/> array being forwarded 
        /// to this read-only instance which may have unexpected results or is
        /// not noticed by other objects using this new <see cref="Deformer"/>
        /// instance.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Deformer"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="deformers"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the length of <paramref name="deformers"/> is 
        /// greater than <see cref="MaximumSize"/>.
        /// </exception>
        /// <exception cref="RankException">
        /// Is thrown when the rank of <paramref name="deformers"/> is not 1.
        /// </exception>
        public static Deformer Create(Matrix4x4[] deformers, bool cloneArray)
        {
            VerifyDeformerArray(deformers);

            if (cloneArray)
            {
                Matrix4x4[] deformersClone = new Matrix4x4[deformers.Length];
                deformers.CopyTo(deformersClone, 0);
                return new Deformer(deformersClone);
            }
            else return new Deformer(deformers);
        }

        /// <summary>
        /// Verifies a <see cref="Matrix4x4"/> array for use in a 
        /// <see cref="Deformer"/> instance and throws an exception, if the
        /// array is invalid. If the array is valid, this method will have
        /// no effect.
        /// </summary>
        /// <param name="deformers">
        /// An array of <see cref="Matrix4x4"/> instances.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="deformers"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the length of <paramref name="deformers"/> is 
        /// greater than <see cref="MaximumSize"/>.
        /// </exception>
        /// <exception cref="RankException">
        /// Is thrown when the rank of <paramref name="deformers"/> is not 1.
        /// </exception>
        private static void VerifyDeformerArray(Matrix4x4[] deformers)
        {
            if (deformers == null)
                throw new ArgumentNullException(nameof(deformers));
            if (deformers.Rank != 1)
                throw new RankException("The deformer array must not have " +
                    "more than one dimension.");
            if (deformers.Length > MaximumSize)
                throw new ArgumentException("The deformer array is larger " +
                    "than the supported maximum size of " + MaximumSize + ".");
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{T}"/> instance.</returns>
        public IEnumerator<Matrix4x4> GetEnumerator()
        {
            return (IEnumerator<Matrix4x4>)deformers.GetEnumerator();
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator"/> instance.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return deformers.GetEnumerator();
        }
    }
}
