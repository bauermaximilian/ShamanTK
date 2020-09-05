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
    /// Represents a type-constrained identifier for channels.
    /// </summary>
    /// <typeparam name="T">
    /// The type from which the value type of the associated channel must be 
    /// assignable from.
    /// </typeparam>
    public class ChannelIdentifier<T> : ChannelIdentifier
    {
        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="ChannelIdentifier{T}"/> class.
        /// </summary>
        /// <param name="name">
        /// The name of the identifier.
        /// </param>
        internal ChannelIdentifier(string name) : base(name, typeof(T)) { }
    }

    /// <summary>
    /// Represents an identifier for channels.
    /// </summary>
    public class ChannelIdentifier : IEquatable<ChannelIdentifier>
    {
        /// <summary>
        /// Gets a typed <see cref="ChannelIdentifier{T}"/> instance for a 
        /// channel that provides an animated position through a
        /// <see cref="Vector3"/>.
        /// </summary>
        public static ChannelIdentifier<Vector3> Position { get; }
            = Create<Vector3>("Position");

        /// <summary>
        /// Gets a typed <see cref="ChannelIdentifier{T}"/> instance for a
        /// channel that provides an animated scale through a
        /// <see cref="Vector3"/>.
        /// </summary>
        public static ChannelIdentifier<Vector3> Scale { get; }
            = Create<Vector3>("Scale");

        /// <summary>
        /// Gets a typed <see cref="ChannelIdentifier{T}"/> instance for a
        /// channel that provides an animated rotation through a 
        /// <see cref="Quaternion"/>.
        /// </summary>
        public static ChannelIdentifier<Quaternion> Rotation { get; }
            = Create<Quaternion>("Rotation");

        /// <summary>
        /// Gets a typed <see cref="ChannelIdentifier{T}"/> instance for a 
        /// channel that provides an animated transformation (with position,
        /// scale and rotation) through a <see cref="Matrix4x4"/>.
        /// </summary>
        public static ChannelIdentifier<Matrix4x4> Transformation { get; }
            = Create<Matrix4x4>("Transformation");

        /// <summary>
        /// Gets the identifier name of the current instance.
        /// Needs to be unique within the parent collection of layers this 
        /// new instance will be assigned to.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> from which the value type of the
        /// associated channel must be assignable from or the type 
        /// <see cref="object"/> if no specific type constraint is required.
        /// </summary>
        public Type ValueTypeConstraint { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelIdentifier"/>
        /// class.
        /// </summary>
        /// <param name="identifierName">
        /// The name of the identifier.
        /// </param>
        /// <param name="valueTypeConstraint">
        /// The value type constraint (or <see cref="object"/> to use none).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifierName"/> or
        /// <paramref name="valueTypeConstraint"/> are null.
        /// </exception>
        internal ChannelIdentifier(string identifierName, 
            Type valueTypeConstraint)
        {
            Identifier = identifierName ??
                throw new ArgumentNullException(nameof(identifierName));
            ValueTypeConstraint = valueTypeConstraint ??
                throw new ArgumentNullException(nameof(valueTypeConstraint));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelIdentifier"/>
        /// class.
        /// </summary>
        /// <param name="identifierName">
        /// The name of the new identifier, which needs to be unique within
        /// the parent collection of layers this new instance will be 
        /// assigned to.
        /// </param>
        /// <returns>A new <see cref="ChannelIdentifier"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifierName"/> is null.
        /// </exception>
        public static ChannelIdentifier Create(string identifierName)
        {
            return new ChannelIdentifier(identifierName, typeof(object));
        }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="ChannelIdentifier{T}"/> class.
        /// </summary>
        /// <typeparam name="T">
        /// The value type constraint of the new instance, which defines, which
        /// type the values of the channel this identifier will be assigned to
        /// can have.
        /// </typeparam>
        /// <param name="identifierName">
        /// The name of the new identifier, which needs to be unique within
        /// the parent collection of layers this new instance will be 
        /// assigned to.
        /// </param>
        /// <returns>
        /// A new <see cref="ChannelIdentifier{T}"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifierName"/> is null.
        /// </exception>
        public static ChannelIdentifier<T> Create<T>(string identifierName)
        {
            return new ChannelIdentifier<T>(identifierName);
        }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="ChannelIdentifier"/> class.
        /// </summary>
        /// <param name="identifierName">
        /// The name of the new identifier, which needs to be unique within
        /// the parent collection of layers this new instance will be 
        /// assigned to.
        /// </param>
        /// <param name="valueTypeConstraint">
        /// The value type constraint of the new instance, which defines, which
        /// type the values of the channel this identifier will be assigned to
        /// can have.
        /// </param>
        /// <returns>A new <see cref="ChannelIdentifier"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifierName"/> or 
        /// <paramref name="valueTypeConstraint"/> are null.
        /// </exception>
        public static ChannelIdentifier Create(string identifierName,
            Type valueTypeConstraint)
        {
            return new ChannelIdentifier(identifierName, valueTypeConstraint);
        }

        public bool MatchesConstraint(ChannelIdentifier identifier)
        {
            return ValueTypeConstraint.IsAssignableFrom(
                identifier.ValueTypeConstraint);
        }

        public bool MatchesConstraint(Type valueTypeConstraint)
        {
            return ValueTypeConstraint.IsAssignableFrom(valueTypeConstraint);
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
            return Equals(obj as ChannelIdentifier);
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
        public bool Equals(ChannelIdentifier other)
        {
            return other != null &&
                   Identifier == other.Identifier &&
                   EqualityComparer<Type>.Default.Equals(ValueTypeConstraint, 
                   other.ValueTypeConstraint);
        }

        /// <summary>
        /// Calculates the hash of the current object instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hashCode = -2075048185;
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<string>.Default.GetHashCode(Identifier);
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<Type>.Default.GetHashCode(ValueTypeConstraint);
            return hashCode;
        }

        public static bool operator ==(ChannelIdentifier left, 
            ChannelIdentifier right)
        {
            return EqualityComparer<ChannelIdentifier>.Default.Equals(left, 
                right);
        }

        public static bool operator !=(ChannelIdentifier left, 
            ChannelIdentifier right)
        {
            return !(left == right);
        }
    }
}
