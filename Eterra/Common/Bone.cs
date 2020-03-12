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
using System.Numerics;

namespace Eterra.Common
{
    /// <summary>
    /// Represents a single bone which defines how and where a mesh can 
    /// be deformed. Instances of this class are immutable.
    /// </summary>
    public class Bone : IEquatable<Bone>
    {
        /// <summary>
        /// Gets an empty <see cref="Bone"/> instance with both the
        /// <see cref="Identifier"/> and the <see cref="Index"/> set to null 
        /// and <see cref="Matrix4x4.Identity"/> as <see cref="Offset"/>.
        /// </summary>
        public static Bone Empty { get; } = new Bone(null, null);

        /// <summary>
        /// Gets the identifier of the bone, which can be used to associate
        /// the bone with an <see cref="AnimationPlayerLayer{T}"/>,
        /// or null.
        /// </summary>
        public string Identifier { get; private set; } = null;

        /// <summary>
        /// Gets the bone attachment index, which defines which of the
        /// <see cref="Vertex"/> in the target mesh are deformed by the
        /// current <see cref="Bone"/>.
        /// </summary>
        public byte? Index { get; private set; } = null;

        /// <summary>
        /// Gets the relative base offset of the bone.
        /// </summary>
        public ref readonly Matrix4x4 Offset => ref offset;
        private Matrix4x4 offset = Matrix4x4.Identity;

        /// <summary>
        /// Gets a value indicating whether the current Bone has an 
        /// identifier, which can be used to associate
        /// the bone with an <see cref="AnimationPlayerLayer{T}"/>
        /// (<c>true</c>) or if the bone shouldn't be associated with 
        /// anything and just have a static <see cref="Offset"/> 
        /// transformation. (<c>false</c>).
        /// </summary>
        public bool HasIdentifier => Identifier != null;

        /// <summary>
        /// Gets a value indicating whether the current <see cref="Bone"/>
        /// has an index, which defines which of the
        /// <see cref="Vertex"/> in the target mesh are deformed by the
        /// current <see cref="Bone"/> (<c>true</c>) or if no index is 
        /// defined and the transformation <see cref="Matrix4x4"/> will not
        /// be included in a <see cref="Deformer"/>.
        /// </summary>
        public bool HasIndex => Index.HasValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bone"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the bone or null.
        /// </param>
        /// <param name="index">
        /// The bone attachment index or null.
        /// </param>
        public Bone(string identifier, byte? index)
            : this(identifier, index, Matrix4x4.Identity) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bone"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the bone or null.
        /// </param>
        /// <param name="index">
        /// The bone attachment index or null.
        /// </param>
        /// <param name="offset">
        /// The relative offset of the bone.
        /// </param>
        public Bone(string identifier, byte? index, in Matrix4x4 offset)
        {
            Identifier = identifier;
            Index = index;
            this.offset = offset;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current object.
        /// </returns>
        public override string ToString()
        {
            string boneName = HasIdentifier ? "Bone \"" + Identifier + "\"" :
                "Unnamed bone";
            string boneIndex = HasIndex ? " (#" + Index + ")" : "";
            return boneName + boneIndex;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current 
        /// object.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with the current object.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified object is equal to the current object,
        /// <c>false</c> otherwise.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Bone);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current 
        /// object.
        /// </summary>
        /// <param name="other">
        /// The object to compare with the current object.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified object is equal to the current object,
        /// <c>false</c> otherwise.
        /// </returns>
        public bool Equals(Bone other)
        {
            return other != null &&
                   Identifier == other.Identifier &&
                   EqualityComparer<byte?>.Default.Equals(Index,
                    other.Index) &&
                   EqualityComparer<Matrix4x4>.Default.Equals(offset,
                    other.offset);
        }

        /// <summary>
        /// Calculates the hash of the current object instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            var hashCode = 1637427723;
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(Identifier);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<byte?>.Default.GetHashCode(Index);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<Matrix4x4>.Default.GetHashCode(offset);
            return hashCode;
        }

        public static bool operator ==(Bone bone1, Bone bone2)
        {
            return EqualityComparer<Bone>.Default.Equals(bone1, bone2);
        }

        public static bool operator !=(Bone bone1, Bone bone2)
        {
            return !(bone1 == bone2);
        }
    }
}
