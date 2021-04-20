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

namespace ShamanTK.Common
{
    /// <summary>
    /// Represents an angle. Supports implicit conversion from and to a
    /// radians angle value as <see cref="float"/>.
    /// </summary>
    public readonly struct Angle : IEquatable<Angle>
    {
        /// <summary>
        /// Gets an <see cref="Angle"/> with a value of 0.
        /// </summary>
        public static Angle Zero { get; } = new Angle(0, 0);

        /// <summary>
        /// Gets an <see cref="Angle"/> with a value of <see cref="Math.PI"/>,
        /// which is half a turn (180°).
        /// </summary>
        public static Angle Pi { get; } = new Angle((float)Math.PI, 0);

        /// <summary>
        /// Gets an <see cref="Angle"/> with a value of a full turn (360° or
        /// 2 * <see cref="Math.PI"/>), which is used as maximum for 
        /// normalizing angles (and thus never reached, but the turn starts
        /// again at <see cref="Zero"/>).
        /// </summary>
        public static Angle MaximumNormalized { get; } = 
            new Angle((float)Math.PI * 2, 0);

        /// <summary>
        /// Defines the factor which can be multiplied with an angle in degrees
        /// to get its equivalent in radians.
        /// </summary>
        private const float DegToRad = (float)(Math.PI / 180.0);

        /// <summary>
        /// Defines the factor which can be multiplied with an angle in radians
        /// to get its equivalent in degrees.
        /// </summary>
        private const float RadToDeg = (float)(180.0 / Math.PI);

        /// <summary>
        /// Gets the value of the current <see cref="Angle"/> in radians.
        /// </summary>
        public float Radians { get; }

        /// <summary>
        /// Gets the value of the current <see cref="Angle"/> in degrees.
        /// </summary>
        public float Degrees => Radians * RadToDeg;

        /// <summary>
        /// Gets a value indicating whether the current instance is normalized
        /// so that the angle value is greater or equal to <see cref="Zero"/>
        /// and less than <see cref="MaximumNormalized"/>.
        /// </summary>
        public bool IsNormalized => Radians < MaximumNormalized.Radians &&
            Radians > Zero.Radians;

        /// <summary>
        /// Gets the value of the current <see cref="Angle"/> as multiple of
        /// <see cref="Math.PI"/> in radians.
        /// </summary>
        public float PiRadians => (float)(Radians / Math.PI);

        private Angle(float radians, float maximum)
        {
            Radians = radians;

            if (maximum != 0) 
                Radians = MathHelper.BringToRange(radians, maximum);
        }

        /// <summary>
        /// Normalizes the current <see cref="Angle"/> instance value and 
        /// returns the result as new <see cref="Angle"/> instance
        /// </summary>
        /// <returns>A new <see cref="Angle"/> instance.</returns>
        public Angle ToNormalized()
        {
            return new Angle(Radians, MaximumNormalized.Radians);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Angle"/> struct
        /// with a angle value in degrees.
        /// </summary>
        /// <param name="radians">
        /// The angle value in degrees.
        /// </param>
        /// <param name="normalize">
        /// <c>true</c> to clamp the specified angle value to not exceed a
        /// full rotation and convert it to its positive equivalent, 
        /// <c>false</c> for the angle value to be stored as it is.
        /// </param>
        public static Angle Deg(float degrees, bool normalize = false)
        {
            return new Angle(degrees * DegToRad, 
                normalize ? MaximumNormalized.Radians : 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Angle"/> struct
        /// with a angle value in radians.
        /// </summary>
        /// <param name="radians">
        /// The angle value in radians.
        /// </param>
        /// <param name="normalize">
        /// <c>true</c> to clamp the specified angle value to not exceed a
        /// full rotation and convert it to its positive equivalent, 
        /// <c>false</c> for the angle value to be stored as it is.
        /// </param>
        public static Angle Rad(float radians, bool normalize = false)
        {
            return new Angle(radians, 
                normalize ? MaximumNormalized.Radians : 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Angle"/> struct
        /// with a angle value in radians as multiple of <see cref="Math.PI"/>.
        /// </summary>
        /// <param name="radians">
        /// The angle value in radians as multiple of <see cref="Math.Pi"/>.
        /// </param>
        /// <param name="normalize">
        /// <c>true</c> to clamp the specified angle value to not exceed a
        /// full rotation and convert it to its positive equivalent, 
        /// <c>false</c> for the angle value to be stored as it is.
        /// </param>
        public static Angle PiRad(float piRadians, bool normalize = false)
        {
            return new Angle((float)(Math.PI * piRadians),
                normalize ? MaximumNormalized.Radians : 0);
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
            return obj is Angle angle && Equals(angle);
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
        public bool Equals(Angle other)
        {
            return Radians == other.Radians;
        }

        /// <summary>
        /// Calculates the hash of the current object instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return 1530437289 + Radians.GetHashCode();
        }

        /// <summary>
        /// Returns the current <see cref="Angle"/> radians value as string.
        /// </summary>
        /// <returns>
        /// The string representation of <see cref="Radians"/>.
        /// </returns>
        public override string ToString()
        {
            return Radians.ToString();
        }

        public static bool operator ==(Angle angle1, Angle angle2)
        {
            return angle1.Equals(angle2);
        }

        public static bool operator !=(Angle angle1, Angle angle2)
        {
            return !(angle1 == angle2);
        }

        public static Angle operator +(Angle angle1, Angle angle2)
        {
            return new Angle(angle1.Radians + angle2.Radians, 0);
        }

        public static Angle operator -(Angle angle1, Angle angle2)
        {
            return new Angle(angle1.Radians - angle2.Radians, 0);
        }

        public static Angle operator *(float scalar, Angle angle)
        {
            return new Angle(angle.Radians * scalar, 0);
        }

        public static Angle operator *(double scalar, Angle angle)
        {
            return new Angle(angle.Radians * (float)scalar, 0);
        }

        public static Angle operator *(Angle angle, float scalar)
        {
            return new Angle(angle.Radians * scalar, 0);
        }

        public static Angle operator *(Angle angle, double scalar)
        {
            return new Angle(angle.Radians * (float)scalar, 0);
        }

        public static Angle operator /(Angle angle, float divisor)
        {
            return new Angle(angle.Radians / divisor, 0);
        }

        public static Angle operator /(Angle angle, double divisor)
        {
            return new Angle(angle.Radians / (float)divisor, 0);
        }

        public static implicit operator Angle(float radians)
        {
            return new Angle(radians, 0);
        }

        public static implicit operator float(Angle angle)
        {
            return angle.Radians;
        }
    }
}
