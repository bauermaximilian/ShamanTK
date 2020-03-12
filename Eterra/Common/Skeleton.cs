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

using Eterra.IO;
using System;
using System.IO;
using System.Numerics;

namespace Eterra.Common
{
    /// <summary>
    /// Represents a hierarchy of <see cref="Bone"/> instances which define
    /// where and how a mesh is deformed.
    /// </summary>
    public class Skeleton : Node<Bone>
    {
        /// <summary>
        /// Defines the name of the root bone of every armature.
        /// </summary>
        public const string RootBoneName = "_root";

        /// <summary>
        /// Gets a read-only <see cref="Skeleton"/> instance with one
        /// empty root bone.
        /// </summary>
        public static Skeleton Empty { get; }
            = new Skeleton(Matrix4x4.Identity).ToReadOnly(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="Skeleton"/> class.
        /// </summary>
        public Skeleton() : this(Matrix4x4.Identity) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Skeleton"/> class.
        /// </summary>
        /// <param name="rootTransformation">
        /// The root transformation of the skeleton.
        /// </param>
        public Skeleton(in Matrix4x4 rootTransformation) 
            : base(new Bone(RootBoneName, null, rootTransformation)) { }

        private Skeleton(Skeleton otherSkeleton,
            bool clone) : base(otherSkeleton, clone) { }

        /// <summary>
        /// Creates a read-only view or copy of the current 
        /// <see cref="Skeleton"/> and all <see cref="Children"/>.
        /// </summary>
        /// <param name="clone">
        /// <c>true</c> to create a deep copy of this
        /// <see cref="Skeleton"/> and all <see cref="Children"/> only
        /// with the values of <typeparamref name="T"/> not being
        /// cloned, <c>false</c> to create a "view" of the current
        /// <see cref="Skeleton"/> which changes with the original instance.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Skeleton"/> class.
        /// </returns>
        public new Skeleton ToReadOnly(bool clone)
        {
            return new Skeleton(this, clone);
        }

        /// <summary>
        /// Exports the current <see cref="Skeleton"/> and all its
        /// <see cref="Children"/> to a <see cref="byte"/> buffer.
        /// </summary>
        public byte[] ToBuffer()
        {
            return ToBuffer(delegate (Bone bone)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    stream.WriteBool(bone.HasIdentifier);
                    if (bone.HasIdentifier)
                        stream.WriteString(bone.Identifier);
                    stream.WriteBool(bone.HasIndex);
                    if (bone.HasIndex)
                        stream.WriteByte(bone.Index.Value);
                    stream.Write(bone.Offset);

                    return stream.GetBuffer();
                }
            });
        }

        /// <summary>
        /// Imports a <see cref="Skeleton"/> hierarchy from a 
        /// <see cref="byte"/> buffer.
        /// </summary>
        /// <param name="buffer">
        /// The source buffer to read the data from.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Skeleton"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the data in the stream had an invalid format or
        /// length.
        /// </exception>
        public static Skeleton FromBuffer(byte[] buffer)
        {
            //TODO: Make sure that the root transformation gets saved/read.
            Skeleton hierarchy = new Skeleton();
            hierarchy.ImportBuffer(buffer, delegate(byte[] elementBuffer)
            {
                using (MemoryStream elementStream = new MemoryStream(
                    elementBuffer, false))
                {
                    bool hasIdentifier = elementStream.ReadBool();
                    string boneIdentifier = hasIdentifier ?
                        elementStream.ReadString() : null;

                    bool hasIndex = elementStream.ReadBool();
                    byte? boneIndex = hasIndex ?
                        elementStream.ReadSingleByte() : new byte?();

                    Matrix4x4 boneOffset = elementStream.Read<Matrix4x4>();

                    return new Bone(boneIdentifier, boneIndex, boneOffset);
                }
            });
            return hierarchy;
        }        
    }
}