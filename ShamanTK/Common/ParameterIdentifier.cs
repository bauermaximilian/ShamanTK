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
using System.Numerics;
using ShamanTK.IO;

namespace ShamanTK.Common
{
    /// <summary>
    /// Represents an identifier for parameter with a type-constraint for the
    /// assigned value.
    /// </summary>
    /// <typeparam name="T">
    /// The type from which the value type of the associated channel must be 
    /// assignable from.
    /// </typeparam>
    /// <remarks>
    /// See the remarks of the <see cref="ParameterIdentifier"/> base class for
    /// more information.
    /// </remarks>
    public class ParameterIdentifier<T> : ParameterIdentifier
    {
        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="ParameterIdentifier{T}"/> class.
        /// </summary>
        /// <param name="name">
        /// The name of the identifier.
        /// </param>
        internal ParameterIdentifier(string name) : base(name, typeof(T)) { }
    }

    /// <summary>
    /// Represents an identifier for parameter with a type-constraint for the
    /// assigned value.
    /// </summary>
    /// <remarks>
    /// Instances of this class can be used as keys for hash maps/dictionaries 
    /// - however, as the <see cref="ValueTypeConstraint"/> is part of the hash
    /// value of each <see cref="ParameterIdentifier"/> instance, it is 
    /// possible to add parameters with the same identifier <see cref="Name"/> 
    /// but a different <see cref="ValueTypeConstraint"/>.
    /// If this behaviour is not wanted, the <see cref="Name"/> of each 
    /// <see cref="ParameterIdentifier"/> instance should be used as key 
    /// instead.
    /// </remarks>
    public class ParameterIdentifier : IEquatable<ParameterIdentifier>
    {
        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a local position as <see cref="Vector3"/>.
        /// </summary>
        public static ParameterIdentifier<Vector3> Position { get; }
            = Create<Vector3>(nameof(Position));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a local scale as <see cref="Vector3"/>.
        /// </summary>
        public static ParameterIdentifier<Vector3> Scale { get; }
            = Create<Vector3>(nameof(Scale));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a local rotation as 
        /// <see cref="Quaternion"/>.
        /// </summary>
        public static ParameterIdentifier<Quaternion> Rotation { get; }
            = Create<Quaternion>(nameof(Rotation));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a local transformation (with position,
        /// scale and rotation) as a <see cref="Matrix4x4"/>.
        /// </summary>
        public static ParameterIdentifier<Matrix4x4> Transformation { get; }
            = Create<Matrix4x4>(nameof(Transformation));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a global transformation (with position,
        /// scale and rotation) as a <see cref="Matrix4x4"/>.
        /// </summary>
        public static ParameterIdentifier<Matrix4x4> TransformationGlobal 
            { get; } = Create<Matrix4x4>(nameof(TransformationGlobal));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a <see cref="bool"/> value which defines 
        /// whether an object is visible or not.
        /// </summary>
        public static ParameterIdentifier<bool> Visible { get; }
            = Create<bool>(nameof(Visible));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a <see cref="Timeline"/> instance.
        /// </summary>
        public static ParameterIdentifier<Timeline> Timeline { get; }
            = Create<Timeline>(nameof(Timeline));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a <see cref="IO.MeshData"/> instance.
        /// </summary>
        public static ParameterIdentifier<MeshData> MeshData { get; }
            = Create<MeshData>(nameof(MeshData));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a <see cref="Color"/> to be used as
        /// the base color.
        /// </summary>
        public static ParameterIdentifier<Color> BaseColor { get; }
            = Create<Color>(nameof(BaseColor));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a <see cref="TextureData"/> instance to be 
        /// used as the base color map. Depending on the used shader, that map 
        /// could, for example, be used as surface color, diffuse map 
        /// (for phong) or albedo map (for PBR).
        /// </summary>
        public static ParameterIdentifier<TextureData> BaseColorMap { get; }
            = Create<TextureData>(nameof(BaseColorMap));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a <see cref="TextureData"/> to be used as
        /// a specular map.
        /// </summary>
        public static ParameterIdentifier<TextureData> SpecularMap { get; }
            = Create<TextureData>(nameof(SpecularMap));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a <see cref="TextureData"/> instance to be 
        /// used as a normal map.
        /// </summary>
        public static ParameterIdentifier<TextureData> NormalMap { get; }
            = Create<TextureData>(nameof(NormalMap));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a <see cref="TextureData"/> instance to be 
        /// used as an emissive map.
        /// </summary>
        public static ParameterIdentifier<TextureData> EmissiveMap { get; }
            = Create<TextureData>(nameof(EmissiveMap));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a <see cref="TextureData"/> instance to be 
        /// used as a metallic roughness map.
        /// </summary>
        public static ParameterIdentifier<TextureData> MetallicMap { get; }
            = Create<TextureData>(nameof(MetallicMap));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a <see cref="TextureData"/> instance to be 
        /// used as an occlusion map.
        /// </summary>
        public static ParameterIdentifier<TextureData> OcclusionMap { get; }
            = Create<TextureData>(nameof(OcclusionMap));

        /// <summary>
        /// Gets a <see cref="ParameterIdentifier{T}"/> instance for a 
        /// parameter that provides a <see cref="Common.Light"/> instance.
        /// </summary>
        public static ParameterIdentifier<Light> Light { get; }
            = Create<Light>(nameof(Light));

        /// <summary>
        /// Gets the identifier name of the current instance.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> from which the type of the
        /// associated parameter value must be assignable from, or 
        /// <see cref="object"/> if no specific type constraint is defined.
        /// </summary>
        public Type ValueTypeConstraint { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterIdentifier"/>
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
        internal ParameterIdentifier(string identifierName, 
            Type valueTypeConstraint)
        {
            Name = identifierName ??
                throw new ArgumentNullException(nameof(identifierName));
            ValueTypeConstraint = valueTypeConstraint ??
                throw new ArgumentNullException(nameof(valueTypeConstraint));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterIdentifier"/>
        /// class.
        /// </summary>
        /// <param name="identifierName">
        /// The name of the identifier.
        /// </param>
        /// <returns>A new <see cref="ParameterIdentifier"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifierName"/> is null.
        /// </exception>
        public static ParameterIdentifier Create(string identifierName)
        {
            return new ParameterIdentifier(identifierName, typeof(object));
        }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="ParameterIdentifier{T}"/> class.
        /// </summary>
        /// <typeparam name="T">
        /// The value type constraint of the new instance.
        /// </typeparam>
        /// <param name="identifierName">
        /// The name of the identifier.
        /// </param>
        /// <returns>
        /// A new <see cref="ParameterIdentifier{T}"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifierName"/> is null.
        /// </exception>
        public static ParameterIdentifier<T> Create<T>(string identifierName)
        {
            return new ParameterIdentifier<T>(identifierName);
        }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="ParameterIdentifier"/> class.
        /// </summary>
        /// <param name="identifierName">
        /// The name of the identifier.
        /// </param>
        /// <param name="valueTypeConstraint">
        /// The value type constraint of the new instance.
        /// </param>
        /// <returns>A new <see cref="ParameterIdentifier"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifierName"/> or 
        /// <paramref name="valueTypeConstraint"/> are null.
        /// </exception>
        public static ParameterIdentifier Create(string identifierName,
            Type valueTypeConstraint)
        {
            return new ParameterIdentifier(identifierName, valueTypeConstraint);
        }

        /// <summary>
        /// Checks whether a specific value type matches the type constraint
        /// of the current <see cref="ParameterIdentifier"/> instance.
        /// </summary>
        /// <param name="identifier">
        /// Another <see cref="ParameterIdentifier"/> instance, which 
        /// <see cref="ValueTypeConstraint"/> should be checked 
        /// whether it matches the current instances'
        /// <see cref="ValueTypeConstraint"/> or not.
        /// </param>
        /// <returns>
        /// <c>true</c> if the current <see cref="ValueTypeConstraint"/> is 
        /// assignable from the <see cref="ValueTypeConstraint"/> of the specified 
        /// <paramref name="identifier"/>, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="valueTypeConstraint"/> is null.
        /// </exception>
        public bool MatchesConstraint(ParameterIdentifier identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));
            return ValueTypeConstraint.IsAssignableFrom(
                identifier.ValueTypeConstraint);
        }

        /// <summary>
        /// Checks whether a specific value type matches the type constraint
        /// of the current <see cref="ParameterIdentifier"/> instance.
        /// </summary>
        /// <param name="valueTypeConstraint">
        /// The type of the value that should be checked whether it matches
        /// the current <see cref="ValueTypeConstraint"/> or not.
        /// </param>
        /// <returns>
        /// <c>true</c> if the current <see cref="ValueTypeConstraint"/> is 
        /// assignable from a <paramref name="valueTypeConstraint"/>,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="valueTypeConstraint"/> is null.
        /// </exception>
        public bool MatchesConstraint(Type valueTypeConstraint)
        {
            if (valueTypeConstraint == null)
                throw new ArgumentNullException(nameof(valueTypeConstraint));
            return ValueTypeConstraint.IsAssignableFrom(valueTypeConstraint);
        }

        /// <summary>
        /// Checks whether a specific value type matches the type constraint
        /// of the current <see cref="ParameterIdentifier"/> instance.
        /// </summary>
        /// <param name="value">
        /// The value, which type should be checked whether it matches
        /// the current <see cref="ValueTypeConstraint"/> or not.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <paramref name="value"/> matches with the 
        /// current <see cref="ValueTypeConstraint"/>, <c>false</c> otherwise
        /// (or when <paramref name="value"/> is null).
        /// </returns>
        public bool MatchesConstraint(object value)
        {
            if (value == null) return false;
            return ValueTypeConstraint.IsAssignableFrom(value.GetType());
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
            return Equals(obj as ParameterIdentifier);
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
        public bool Equals(ParameterIdentifier other)
        {
            return other != null &&
                   Name == other.Name &&
                   EqualityComparer<Type>.Default.Equals(ValueTypeConstraint, 
                   other.ValueTypeConstraint);
        }

        /// <summary>
        /// Calculates the hash of the current object instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, ValueTypeConstraint);
        }

        public static bool operator ==(ParameterIdentifier left, 
            ParameterIdentifier right)
        {
            return EqualityComparer<ParameterIdentifier>.Default.Equals(left, 
                right);
        }

        public static bool operator !=(ParameterIdentifier left, 
            ParameterIdentifier right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{Name}:{ValueTypeConstraint.Name}";
        }
    }
}
