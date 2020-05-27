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

namespace Eterra.Common
{
    /// <summary>
    /// Represents an angle. Supports implicit conversion from and to a
    /// radians angle value as <see cref="float"/>.
    /// </summary>
    public readonly struct Angle : IEquatable<Angle>
    {
        /// <summary>
        /// Gets a <see cref="Angle"/> with a value of 0.
        /// </summary>
        public static Angle Zero { get; } = new Angle(0, false);

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
        /// so that the angle value is positive and doesn't exceed a full
        /// rotation.
        /// </summary>
        public bool IsNormalized => Radians < Math.PI && Radians >= 0;

        /// <summary>
        /// Gets the value of the current <see cref="Angle"/> as multiple of
        /// <see cref="Math.PI"/> in radians.
        /// </summary>
        public float PiRadians => (float)(Radians / Math.PI);

        private Angle(float radians, bool normalize)
        {
            if (normalize) Radians = NormalizeRad(radians);
            else Radians = radians;
        }

        private static float NormalizeRad(float radians)
        {
            radians = (float)(radians % Math.PI);
            if (radians < 0) return (float)(Math.PI + radians);
            else return radians;
        }

        /// <summary>
        /// Normalizes the current <see cref="Angle"/> instance value and 
        /// returns the result as new <see cref="Angle"/> instance
        /// </summary>
        /// <returns>A new <see cref="Angle"/> instance.</returns>
        public Angle ToNormalized()
        {
            return new Angle(Radians, true);
        }

        /// <summary>
        /// Limits the current <see cref="Angle"/> instance value to a specific
        /// range (where values exceeding the maximum will start either at 0
        /// or -<paramref name="maximum"/> again and vice versa) and returns 
        /// the result as new <see cref="Angle"/> instance.
        /// </summary>
        /// <param name="maximum">
        /// The maximum value of the range the returned <see cref="Angle"/> 
        /// should be in.
        /// </param>
        /// <param name="allowNegative">
        /// <c>true</c> to limit the value to a range between 
        /// -<paramref name="maximum"/> and <paramref name="maximum"/>,
        /// <c>false</c> to limit the value to a range between 0 and
        /// <paramref name="maximum"/>.
        /// </param>
        /// <param name="turns">
        /// The amount of turns of the limited <see cref="Angle"/> value to
        /// be in the limited range. This value can be positive or negative.
        /// </param>
        /// <returns>A new <see cref="Angle"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="maximum"/> is negative or 0.
        /// </exception>
        public Angle TurnToRange(Angle maximum, bool allowNegative)
        {
            return TurnToRange(maximum, allowNegative, out _);
        }

        /// <summary>
        /// Limits the current <see cref="Angle"/> instance value to a specific
        /// range (where values exceeding the maximum will start either at 0
        /// or -<paramref name="maximum"/> again and vice versa) and returns 
        /// the result as new <see cref="Angle"/> instance.
        /// </summary>
        /// <param name="maximum">
        /// The maximum value of the range the returned <see cref="Angle"/> 
        /// should be in.
        /// </param>
        /// <param name="allowNegative">
        /// <c>true</c> to limit the value to a range between 
        /// -<paramref name="maximum"/> and <paramref name="maximum"/>,
        /// <c>false</c> to limit the value to a range between 0 and
        /// <paramref name="maximum"/>.
        /// </param>
        /// <param name="turns">
        /// The amount of turns of the limited <see cref="Angle"/> value to
        /// be in the limited range. This value can be positive or negative.
        /// </param>
        /// <returns>A new <see cref="Angle"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="maximum"/> is negative or 0.
        /// </exception>
        public Angle TurnToRange(Angle maximum, bool allowNegative, 
            out float turns)
        {
            if (maximum.Radians <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximum));

            //Doubles for more precision - and it's a nice relief, right? 
            double valueInRange = Radians;
            double range = maximum;
            turns = Radians / maximum;

            //The idea behind these two clauses (this one and the one below)
            //is to "move" the range between -maximum and +maximum to 
            //0 and 2*maximum, so that the algorithm below can be used for this
            //case as well without any modifications.
            if (allowNegative)
            {
                valueInRange += maximum;
                range *= 2;
            }

            //The amount of turns specifies how many times the "range" between
            //0 and the maximum was "left" (exceeded) on the "right" side and
            //re-entered on the left side.
            double rangeTurns = valueInRange / range;
            double turn;
            if (rangeTurns != 0 && rangeTurns - (int)rangeTurns == 0)
                turn = (valueInRange < 0 ? -1 : 1);
            else turn = rangeTurns % 1;

            //The previously calculated amount of turns is used here to create
            //a value which is in the specified limits. Mind that the behaviour
            //of this algorithm is different to a "normal" modulo as the 
            //maximum is included in the range - which means a value of
            //90° will be returned as 90° even if the maximum is 90°.
            //In the different use cases of this method, results with that 
            //rule did make more sense to me. But I'm not a mathematican, so...
            double limitedValue;
            if (rangeTurns < 0 && turn != -1)
                limitedValue = range - (Math.Abs(turn) * range);
            else limitedValue = Math.Abs(turn) * range;

            //This might not be the best way to solve this problem, but I am
            //terrible at maths and am glad I came up with that algorithm...
            //and I suppose I won't know how I did that two weeks from now.
            if (allowNegative)
            {
                limitedValue -= maximum;
                //rangeTurns = Radians / maximum;
            }

            return (float)limitedValue;
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
            return new Angle(degrees * DegToRad, normalize);
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
            return new Angle(radians, normalize);
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
            return new Angle((float)(Math.PI * piRadians), normalize);
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
            return obj is Angle && Equals((Angle)obj);
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
            return new Angle(angle1.Radians + angle2.Radians, false);
        }

        public static Angle operator -(Angle angle1, Angle angle2)
        {
            return new Angle(angle1.Radians - angle2.Radians, false);
        }

        public static Angle operator *(float scalar, Angle angle)
        {
            return new Angle(angle.Radians * scalar, false);
        }

        public static Angle operator *(double scalar, Angle angle)
        {
            return new Angle(angle.Radians * (float)scalar, false);
        }

        public static Angle operator *(Angle angle, float scalar)
        {
            return new Angle(angle.Radians * scalar, false);
        }

        public static Angle operator *(Angle angle, double scalar)
        {
            return new Angle(angle.Radians * (float)scalar, false);
        }

        public static Angle operator /(Angle angle, float divisor)
        {
            return new Angle(angle.Radians / divisor, false);
        }

        public static Angle operator /(Angle angle, double divisor)
        {
            return new Angle(angle.Radians / (float)divisor, false);
        }

        public static implicit operator Angle(float radians)
        {
            return new Angle(radians, false);
        }

        public static implicit operator float(Angle angle)
        {
            return angle.Radians;
        }
    }
}
