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
using System.Globalization;
using System.Text;

namespace Eterra.Common
{
    /// <summary>
    /// Provides parameters that describe how a object behaves in relation to
    /// collisions with other objects, gravity or other physical forces.
    /// Mind that for a object to be influenced by collisions with other
    /// physical objects, a collider (like <see cref="ColliderPrimitive"/>)
    /// is required.
    /// </summary>
    public readonly struct Dynamics : IEquatable<Dynamics>
    {
        private const string DynamicLiteral = "Dynamic";
        private const string StaticLiteral = "Static";

        /// <summary>
        /// Gets the default <see cref="Dynamics"/> instance for a static 
        /// object that is not influenced by collisions with other objects,
        /// forces or gravity.
        /// </summary>
        public static Dynamics Static { get; } = new Dynamics(false, 0, 0, 0);

        /// <summary>
        /// Gets the default <see cref="Dynamics"/> instance of a dynamic 
        /// object, that is influenced by collisions with other objects,
        /// forces or gravity.
        /// </summary>
        public static Dynamics DynamicDefault { get; } = new Dynamics(true, 
            1, 0, 0);

        /// <summary>
        /// Gets a value indicating whether the current <see cref="Dynamics"/>
        /// instance specifies a dynamic object, whose movement is influenced
        /// by collisions with other objects, gravity and other physical 
        /// forces (<c>true</c>), or if the current object is static and isn't 
        /// influenced by any physical forces (<c>false</c>).
        /// </summary>
        public bool IsDynamic { get; }

        /// <summary>
        /// Gets the mass of the current <see cref="Dynamics"/> instance.
        /// </summary>
        public float Mass { get; }

        /// <summary>
        /// Gets the friction (in terms of movement) of the current 
        /// <see cref="Dynamics"/> instance.
        /// </summary>
        public float Friction { get; }

        /// <summary>
        /// Gets the restitution (for collisions with other objects) of the
        /// current <see cref="Dynamics"/> instance.
        /// </summary>
        public float Restitution { get; }

        private Dynamics(bool isDynamic, float mass, float friction,
            float restitution)
        {
            if (mass < 0)
                throw new ArgumentOutOfRangeException(nameof(mass));
            if (friction < 0)
                throw new ArgumentOutOfRangeException(nameof(friction));
            if (restitution < 0)
                throw new ArgumentOutOfRangeException(nameof(restitution));

            IsDynamic = isDynamic;
            Mass = mass;
            Friction = friction;
            Restitution = restitution;
        }

        /// <summary>
        /// Initializes a new dynamic <see cref="Dynamics"/> instance.
        /// </summary>
        /// <param name="mass">The mass of the object.</param>
        /// <param name="friction">The friction of the object.</param>
        /// <param name="restitution">The restitution of the object.</param>
        /// <returns>A new <see cref="Dynamics"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when any of the provided values is less than 0.
        /// </exception>
        public static Dynamics CreateDynamic(float mass, float friction,
            float restitution)
        {
            return new Dynamics(true, mass, friction, restitution);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// This string can be parsed into a new 
        /// <see cref="Dynamics"/> instance with the same parameters
        /// with <see cref="TryParse(string, out Dynamics)"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current object.
        /// </returns>
        /// <remarks>
        /// The <see cref="CultureInfo.InvariantCulture"/> is used for the
        /// numbers of the <see cref="Dynamics"/>.
        /// </remarks>
        public override string ToString()
        {
            CultureInfo c = CultureInfo.InvariantCulture;

            StringBuilder stringBuilder = new StringBuilder();

            if (IsDynamic)
            {
                stringBuilder.Append(DynamicLiteral);
                stringBuilder.Append(" (");
                stringBuilder.Append(Mass.ToString(c));
                stringBuilder.Append(',');
                stringBuilder.Append(Friction.ToString(c));
                stringBuilder.Append(',');
                stringBuilder.Append(Restitution.ToString(c));
                stringBuilder.Append(')');
            }
            else stringBuilder.Append(StaticLiteral);

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the string representation of a 
        /// <see cref="Dynamics"/> into an instance of that type
        /// with the values from the string.
        /// </summary>
        /// <param name="s">
        /// The input string which should be parsed.
        /// </param>
        /// <returns>
        /// The new parsed <see cref="Dynamics"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="s"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when <paramref name="s"/> is no valid string 
        /// representation of a <see cref="Dynamics"/> instance.
        /// </exception>
        public static Dynamics Parse(string s)
        {
            if (TryParse(s, out Dynamics result)) return result;
            else throw new FormatException("The specified string was no " +
                "valid " + nameof(Dynamics) + " string representation.");
        }

        /// <summary>
        /// Converts the string representation of a 
        /// <see cref="Dynamics"/> into an instance of that type
        /// with the values from the string.
        /// </summary>
        /// <param name="s">
        /// The input string which should be parsed.
        /// </param>
        /// <param name="result">
        /// The new parsed <see cref="Dynamics"/> instance or
        /// <see cref="Static"/>, if the process failed.
        /// </param>
        /// <returns>
        /// <c>true</c> if the conversion was successful and 
        /// <paramref name="result"/> contains a valid 
        /// <see cref="Dynamics"/> instance, <c>false</c> when the
        /// conversion failed and <paramref name="result"/> is 
        /// <see cref="Static"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="s"/> is null.
        /// </exception>
        /// <remarks>
        /// The <see cref="CultureInfo.InvariantCulture"/> is used for the
        /// numbers of the <see cref="Dynamics"/>.
        /// </remarks>
        public static bool TryParse(string s, out Dynamics result)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            CultureInfo c = CultureInfo.InvariantCulture;
            NumberStyles ns = NumberStyles.Float;

            result = Static;

            string[] parts = s.Replace(" ", "").Trim(')').ToLowerInvariant()
                .Split('(', ',');

            if (parts[0] == DynamicLiteral.ToLowerInvariant())
            {
                //If certain components are not defined, the default values 
                //are assumed. If the components are defined, but invalid,
                //an invalid string is assumed and the method returns false.

                float mass = DynamicDefault.Mass,
                    friction = DynamicDefault.Friction,
                    restitution = DynamicDefault.Restitution;

                if (parts.Length > 1)
                {
                    if (!float.TryParse(parts[1], ns, c, out mass))
                        return false;
                }

                if (parts.Length > 2)
                {
                    if (!float.TryParse(parts[2], ns, c, out friction))
                        return false;
                }

                if (parts.Length > 3)
                {
                    if (!float.TryParse(parts[3], ns, c, out restitution))
                        return false;
                }

                result = new Dynamics(true, mass, friction, restitution);
                return true;
            }
            else if (parts[0] == StaticLiteral.ToLowerInvariant() &&
              (parts.Length == 1 ||
              (parts.Length == 2 && parts[1].Length == 0)))
            {
                //The result is already assigned to a default static dynamics 
                //instance above, so the method only needs to return true here.
                return true;
            }
            else return false;
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
            return obj is Dynamics && Equals((Dynamics)obj);
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
        public bool Equals(Dynamics other)
        {
            return IsDynamic == other.IsDynamic &&
                   Mass == other.Mass &&
                   Friction == other.Friction &&
                   Restitution == other.Restitution;
        }

        /// <summary>
        /// Calculates the hash of the current object instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            var hashCode = 1072672145;
            hashCode = hashCode * -1521134295 + IsDynamic.GetHashCode();
            hashCode = hashCode * -1521134295 + Mass.GetHashCode();
            hashCode = hashCode * -1521134295 + Friction.GetHashCode();
            hashCode = hashCode * -1521134295 + Restitution.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Dynamics dynamics1, Dynamics dynamics2)
        {
            return dynamics1.Equals(dynamics2);
        }

        public static bool operator !=(Dynamics dynamics1, Dynamics dynamics2)
        {
            return !(dynamics1 == dynamics2);
        }
    }
}
