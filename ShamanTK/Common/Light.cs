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
using System.Text;

namespace ShamanTK.Common
{
    /// <summary>
    /// Defines the possible types of a <see cref="Light"/>.
    /// </summary>
    public enum LightType : byte
    {
        /// <summary>
        /// A disabled light which has no effect on the scene rendering.
        /// </summary>
        Disabled,
        /// <summary>
        /// A directional light source without attenuation through distance.
        /// </summary>
        Ambient,
        /// <summary>
        /// An omnidirectional light source, which attenuates over distance.
        /// </summary>
        Point,
        /// <summary>
        /// A directional light source with a "light cone", which attenuates
        /// over distance.
        /// </summary>
        Spot
    }

    /// <summary>
    /// Represents a light source.
    /// </summary>
    public readonly struct Light : IEquatable<Light>
    {
        /// <summary>
        /// Gets a <see cref="Light"/> instance with of <see cref="Type"/>
        /// <see cref="LightType.Disabled"/>, which will have no effect
        /// when rendering a scene.
        /// </summary>
        public static Light Disabled { get; } = new Light();

        /// <summary>
        /// Gets the type of the light source.
        /// </summary>
        public LightType Type { get; }

        /// <summary>
        /// Gets the <see cref="Color"/> of the light source.
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// Gets the position of the light, if <see cref="Type"/> is
        /// <see cref="LightType.Point"/> or <see cref="LightType.Spot"/>.
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// Gets the direction of the light, if <see cref="Type"/> is
        /// <see cref="LightType.Ambient"/> or <see cref="LightType.Spot"/>.
        /// </summary>
        public Vector3 Direction { get; }

        /// <summary>
        /// Gets the radius (distance) of the light source, if 
        /// <see cref="Type"/> is <see cref="LightType.Point"/> or
        /// <see cref="LightType.Spot"/>.
        /// </summary>
        public float Radius { get; }

        /// <summary>
        /// Gets the cutoff angle, which defines the size of the spotlight 
        /// cone, if <see cref="Type"/> is <see cref="LightType.Spot"/>.
        /// Used in combination with <see cref="CutoffWidth"/>.
        /// </summary>
        public Angle Cutoff { get; }

        /// <summary>
        /// Gets the width of the cutoff, which defines whether the edge of
        /// the cutoff is hard (lower value) or smooth/soft (higher value),
        /// if <see cref="Type"/> is <see cref="LightType.Spot"/>.
        /// </summary>
        public Angle CutoffWidth { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Light"/> class
        /// as <see cref="LightType.Ambient"/>.
        /// </summary>
        /// <param name="color">The color of the light.</param>
        /// <param name="direction">The direction of the light.</param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="direction"/> has a 
        /// <see cref="Vector3.Length"/> of 0, which indicates that it 
        /// doesn't "point" anywhere.
        /// </exception>
        public Light(Color color, Vector3 direction)
        {
            if (direction.Length() == 0)
                throw new ArgumentException("The specified direction vector " +
                    "has a length of 0 and doesn't point anywhere.");

            Color = color;
            Direction = Vector3.Normalize(direction);
            Position = Vector3.Zero;
            Radius = 0;
            Cutoff = Angle.Deg(0);
            CutoffWidth = Angle.Deg(0);

            Type = LightType.Ambient;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Light"/> class
        /// as <see cref="LightType.Point"/>.
        /// </summary>
        /// <param name="color">The color of the light.</param>
        /// <param name="position">The position of the light.</param>
        /// <param name="radius">The radius of the light.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="radius"/> is less than 0.
        /// </exception>
        public Light(Color color, Vector3 position, float radius)
        {
            if (radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius));

            Color = color;
            Direction = Vector3.Zero;
            Position = position;
            Radius = radius;
            Cutoff = Angle.Deg(0);
            CutoffWidth = Angle.Deg(0);

            Type = LightType.Point;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Light"/> class
        /// as <see cref="LightType.Spot"/>.
        /// </summary>
        /// <param name="color">The color of the light.</param>
        /// <param name="position">The position of the light.</param>
        /// <param name="radius">The radius of the light.</param>
        /// <param name="direction">The direction of the light.</param>
        /// <param name="cutoff">
        /// The cutoff angle, which defines the size of the light cone.
        /// </param>
        /// <param name="cutoffWidth">
        /// The width as angle, which defines the size of the cutoff edges and,
        /// with that, how hard or smooth the transition between light and 
        /// dark will be.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="radius"/> is less than 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="cutoff"/> or 
        /// <paramref name="cutoffWidth"/> aren't normalized, or when
        /// <paramref name="direction"/> has a <see cref="Vector3.Length"/> 
        /// of 0, which indicates that it doesn't "point" anywhere.
        /// </exception>
        public Light(Color color, Vector3 position, float radius,
            Vector3 direction, Angle cutoff, Angle cutoffWidth)
        {
            if (direction.Length() == 0)
                throw new ArgumentException("The specified direction vector " +
                    "has a length of 0 and doesn't point anywhere.");
            if (radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (!cutoff.IsNormalized)
                throw new ArgumentException("The cutoff angle is not " +
                    "normalized!");
            if (!cutoffWidth.IsNormalized)
                throw new ArgumentException("The cutoff width angle is not " +
                    "normalized!");

            Color = color;
            Position = position;
            Radius = radius;
            Direction = Vector3.Normalize(direction);
            Cutoff = cutoff;
            CutoffWidth = cutoffWidth;

            Type = LightType.Spot;
        }

        private Light(Light light, Vector3 position)
        {
            Color = light.Color;
            Position = position;
            Radius = light.Radius;
            Direction = light.Direction;
            Cutoff = light.Cutoff;
            CutoffWidth = light.CutoffWidth;
            Type = light.Type;
        }

        /// <summary>
        /// Creates a new <see cref="Light"/> instance with the parameters
        /// of the current instance, but with another <see cref="Position"/>.
        /// </summary>
        /// <param name="direction">
        /// The direction <see cref="Vector3"/>, which is added to the
        /// <see cref="Position"/> of the new <see cref="Light"/> instance.
        /// </param>
        /// <returns>A new <see cref="Light"/> instance.</returns>
        public Light Moved(Vector3 direction)
        {
            return new Light(this, Position + direction);
        }

        /// <summary>
        /// Creates a new <see cref="Light"/> instance with the parameters
        /// of the current instance, but with another <see cref="Position"/>.
        /// </summary>
        /// <param name="position">
        /// The position of the new <see cref="Light"/> instance.
        /// </param>
        /// <returns>A new <see cref="Light"/> instance.</returns>
        public Light MovedTo(Vector3 position)
        {
            return new Light(this, position);
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
            return obj is Light && Equals((Light)obj);
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
        public bool Equals(Light other)
        {
            return Type == other.Type &&
                   Color.Equals(other.Color) &&
                   EqualityComparer<Vector3>.Default.Equals(Position, 
                    other.Position) &&
                   EqualityComparer<Vector3>.Default.Equals(Direction, 
                    other.Direction) &&
                   Radius == other.Radius &&
                   Cutoff.Equals(other.Cutoff) &&
                   CutoffWidth.Equals(other.CutoffWidth);
        }

        /// <summary>
        /// Calculates the hash of the current object instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            var hashCode = 774473416;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<Color>.Default.GetHashCode(Color);
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<Vector3>.Default.GetHashCode(Position);
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<Vector3>.Default.GetHashCode(Direction);
            hashCode = hashCode * -1521134295 + Radius.GetHashCode();
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<Angle>.Default.GetHashCode(Cutoff);
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<Angle>.Default.GetHashCode(CutoffWidth);
            return hashCode;
        }

        public static bool operator ==(Light light1, Light light2)
        {
            return light1.Equals(light2);
        }

        public static bool operator !=(Light light1, Light light2)
        {
            return !(light1 == light2);
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(Type.ToString());

            return stringBuilder.ToString();
        }
    }
}
