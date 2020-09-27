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
using System.Numerics;

namespace ShamanTK.Common
{
    /// <summary>
    /// Provides helper methods for various operations with vectors and
    /// matrices.
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// Defines the amount of seconds of a calculation step for the
        /// accerlation methods.
        /// </summary>
        internal const float StepSizeSeconds = 0.03f;

        /// <summary>
        /// Defines the default amount of friction for the
        /// accerlation methods.
        /// </summary>
        internal const float DefaultFriction = 2;

        /// <summary>
        /// Creates a transformation matrix out of a position and a scale.
        /// </summary>
        /// <param name="positionX">The X component of the position.</param>
        /// <param name="positionY">The Y component of the position.</param>
        /// <param name="positionZ">The Z component of the position.</param>
        /// <param name="scaleX">The X component of the scale.</param>
        /// <param name="scaleY">The Y component of the scale.</param>
        /// <param name="scaleZ">The Z component of the scale.</param>
        /// <returns>A new transformation matrix.</returns>
        public static Matrix4x4 CreateTransformation(float positionX, 
            float positionY, float positionZ, float scaleX, float scaleY, 
            float scaleZ)
        {
            return Matrix4x4.CreateScale(scaleX, scaleY, scaleZ) *
                Matrix4x4.CreateTranslation(positionX, positionY, positionZ);
        }

        /// <summary>
        /// Creates a transformation matrix out of a position and a scale.
        /// </summary>
        /// <param name="position">The position vector.</param>
        /// <param name="scale">The scale vector.</param>
        /// <returns>A new transformation matrix.</returns>
        public static Matrix4x4 CreateTransformation(in Vector3 position,
            in Vector3 scale)
        {
            return Matrix4x4.CreateScale(scale) 
                * Matrix4x4.CreateTranslation(position);
        }

        /// <summary>
        /// Creates a transformation matrix out of a position,
        /// scale and rotation.
        /// </summary>
        /// <param name="position">The position vector.</param>
        /// <param name="scale">The scale vector.</param>
        /// <param name="rotation">The rotation quaternion.</param>
        /// <returns>A new transformation matrix.</returns>
        public static Matrix4x4 CreateTransformation(in Vector3 position,
            in Vector3 scale, in Quaternion rotation)
        {
            return Matrix4x4.CreateScale(scale) 
                * Matrix4x4.CreateFromQuaternion(rotation) 
                * Matrix4x4.CreateTranslation(position);
        }

        /// <summary>
        /// Creates an absolute transformation matrix out of a relative
        /// transformation and its absolute "parent" transformation.
        /// </summary>
        /// <param name="parentTransformation">
        /// The parent root transformation matrix.
        /// </param>
        /// <param name="childTransformation">
        /// The relative child transformation matrix.
        /// </param>
        /// <returns>A new transformation matrix.</returns>
        public static Matrix4x4 CombineTransformations(
            in Matrix4x4 parentTransformation, 
            in Matrix4x4 childTransformation)
        {
            return parentTransformation * childTransformation;
        }

        /// <summary>
        /// Creates a absolute vector out of the specified by making every
        /// component absolute (unsigned).
        /// </summary>
        /// <returns>A new vector.</returns>
        public static Vector2 CreateAbsoluteVector(Vector2 vector)
        {
            return new Vector2(Math.Abs(vector.X), Math.Abs(vector.Y));
        }

        //TODO: Remove following two methods after Camera class replaced
        /// <summary>
        /// Creates a new <see cref="Quaternion"/> from a set of euler 
        /// rotation values.
        /// </summary>
        /// <param name="eulerRotation">
        /// The euler rotation components as <see cref="Vector3"/>.
        /// </param>
        /// <returns>A new <see cref="Quaternion"/> instance.</returns>
        public static Quaternion CreateRotation(Vector3 eulerRotation)
        {
            return CreateRotation(eulerRotation.X, eulerRotation.Y,
                eulerRotation.Z);
        }

        /// <summary>
        /// Creates a new <see cref="Quaternion"/> from a set of euler 
        /// rotation values (Z*X*Y).
        /// </summary>
        /// <param name="eulerX">The rotation around the X axis.</param>
        /// <param name="eulerY">The rotation around the Y axis.</param>
        /// <param name="eulerZ">The rotation around the Z axis.</param>
        /// <returns>A new <see cref="Quaternion"/> instance.</returns>
        public static Quaternion CreateRotation(Angle eulerX, Angle eulerY, 
            Angle eulerZ)
        {
            return Quaternion.CreateFromAxisAngle(Vector3.UnitZ, eulerZ) *
                Quaternion.CreateFromAxisAngle(Vector3.UnitX, eulerX) *
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, eulerY);
        }

        /// <summary>
        /// Combines two rotations to one.
        /// </summary>
        /// <param name="rotation">
        /// The base rotation as <see cref="Quaternion"/>.
        /// </param>
        /// <param name="deltaEuler">
        /// The euler rotation, which should be applied to the base
        /// <paramref name="rotation"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="Quaternion"/> instance.
        /// </returns>
        public static Quaternion CombineRotation(Quaternion rotation,
            Vector3 deltaEuler)
        {
            return 
                Quaternion.CreateFromAxisAngle(Vector3.UnitZ, deltaEuler.Z) *
                Quaternion.CreateFromAxisAngle(Vector3.UnitX, deltaEuler.X) *
                rotation *
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, deltaEuler.Y);
        }

        /// <summary>
        /// Rotates a directional vector using a quaternion.
        /// </summary>
        /// <param name="direction">
        /// The vector representing the direction to be rotated.
        /// </param>
        /// <param name="rotation">
        /// The quaternion representing the rotation to be applied to the
        /// provided direction.
        /// </param>
        /// <returns>A new <see cref="Vector3"/> instance.</returns>
        /// <remarks>
        /// Combination of https://gamedev.stackexchange.com/a/50545 and the
        /// comment of j00hi to the answer.
        /// </remarks>
        public static Vector3 RotateDirection(Vector3 direction, 
            Quaternion rotation)
        {
            Vector3 rotationXyz = new Vector3(rotation.X, rotation.Y, 
                rotation.Z);
            Vector3 rotationXyzCrossDirection = 
                Vector3.Cross(rotationXyz, direction);

            return direction + ((rotationXyzCrossDirection * rotation.W) + 
                Vector3.Cross(rotationXyz, rotationXyzCrossDirection)) * 2;
        }

        /// <summary>
        /// Calculates a cubic interpolation of two values.
        /// </summary>
        /// <param name="beforeX">The value before the first value.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="afterY">The value after the second value.</param>
        /// <param name="ratio">
        /// The mixing ratio of the two main values.
        /// </param>
        /// <returns>A new <see cref="Vector2"/> instance.</returns>
        /// <remarks>
        /// This algorithm is based on the 3D cubic interpolation algorithm 
        /// from http://paulbourke.net/miscellaneous/interpolation/.
        /// </remarks>
        public static Vector2 InterpolateCubic(in Vector2 beforeX,
                in Vector2 x, in Vector2 y, in Vector2 afterY,
                float ratio)
        {
            float ratioSquared = ratio * ratio;
            Vector2 interimVector = afterY - y - beforeX + x;

            return interimVector * ratio * ratioSquared
                + (beforeX - x - interimVector) * ratioSquared
                + (y - beforeX) * ratio
                + x;
        }

        /// <summary>
        /// Calculates a cubic interpolation of two values.
        /// </summary>
        /// <param name="beforeX">The value before the first value.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="afterY">The value after the second value.</param>
        /// <param name="ratio">
        /// The mixing ratio of the two main values.
        /// </param>
        /// <returns>A new <see cref="Vector3"/> instance.</returns>
        /// <remarks>
        /// This algorithm is based on the 3D cubic interpolation algorithm 
        /// from http://paulbourke.net/miscellaneous/interpolation/.
        /// </remarks>
        public static Vector3 InterpolateCubic(in Vector3 beforeX,
                in Vector3 x, in Vector3 y, in Vector3 afterY,
                float ratio)
        {
            float ratioSquared = ratio * ratio;
            Vector3 interimVector = afterY - y - beforeX + x;

            return interimVector * ratio * ratioSquared
                + (beforeX - x - interimVector) * ratioSquared
                + (y - beforeX) * ratio
                + x;
        }

        /// <summary>
        /// Calculates a cubic interpolation of two values.
        /// </summary>
        /// <param name="beforeX">The value before the first value.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="afterY">The value after the second value.</param>
        /// <param name="ratio">
        /// The mixing ratio of the two main values.
        /// </param>
        /// <returns>A new <see cref="Vector2D"/> instance.</returns>
        public static Matrix4x4 InterpolateCubic(in Matrix4x4 beforeX,
                in Matrix4x4 x, in Matrix4x4 y, in Matrix4x4 afterY,
                float ratio)
        {
            return ((beforeX + x) - (afterY + y))
                * (float)Math.Pow(ratio, 3)
                + ((beforeX + x * 2) - (afterY + y * 2))
                * (float)Math.Pow(ratio, 2) + x;
        }

#if ENABLE_EXPERIMENTAL_API
        /// <summary>
        /// Creates a new <see cref="Quaternion"/> as a result of looking at
        /// a target.
        /// </summary>
        /// <param name="yaw">The yaw rotation component (X-axis).</param>
        /// <param name="pitch">The pitch rotation component (Y-axis).</param>
        /// <param name="roll">The roll rotation component (Z-axis).</param>
        /// <param name="limitValues">
        /// <c>true</c> to limit the <paramref name="yaw"/> between 
        /// <c>-90/90</c>, the <paramref name="pitch"/> and 
        /// <paramref name="roll"/> between <c>-180/180</c> and to flip
        /// the <paramref name="pitch"/> if the <paramref name="yaw"/> exceeds
        /// its limits and use these clamped values to create the new rotation
        /// <see cref="Quaternion"/>, <c>false</c> to just create the 
        /// rotation <see cref="Quaternion"/> out of the original values 
        /// without modifying them.
        /// </param>
        /// <returns>A new <see cref="Quaternion"/> instance.</returns>
        public static Quaternion CreateRotation(Vector3 lookAtPosition,
                Vector3 origin)
        {
            //This probably doesn't work, it has never been tested.
            lookAtPosition -= origin;
            float r = (float)Math.Sqrt(Math.Pow(lookAtPosition.X, 2)
                + Math.Pow(lookAtPosition.Z, 2));
            return Quaternion.CreateFromYawPitchRoll(
                (float)Math.Atan2(lookAtPosition.X, lookAtPosition.Z),
                (float)Math.Atan2(lookAtPosition.Y * -1.0, r), 0);

            /*
            //Alternative implementation to try out:
            Vector3 v = lookAtTarget - Position;
            float r = (float)Math.Sqrt(Math.Pow(v.X, 2) + Math.Pow(v.Z, 2));
            float yaw = (float)Math.Atan2(v.X, v.Z);
            float pitch = (float)Math.Atan2(v.Y * -1.0, r);
            RotateTo(yaw, pitch, 0);

            //https://answers.unity.com/questions/306184/lootat-euler-angles.html
            //https://www.gamedev.net/forums/topic/563474-lookat-functionality-for-fps-euler-angle-camera/
            */
        }

        /// <summary>
        /// Calculates the advancement of a value in a given time.
        /// </summary>
        /// <param name="x">The current value.</param>
        /// <param name="y">The target value.</param>
        /// <param name="currentAccerlation">
        /// The current accerlation, which will be modified by this method.
        /// </param>
        /// <param name="delta">
        /// The amount of time for which the accerlation should be calculated.
        /// </param>
        /// <param name="friction">
        /// The friction, which will affect how the accerlation builds up
        /// and decreases.
        /// </param>
        /// <returns>A new <see cref="Vector3D"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="friction"/> is less than 0 or when
        /// <paramref name="delta"/> is negative.
        /// </exception>
        public static Vector2 Accerlate(in Vector2 x, in Vector2 y,
            ref Vector2 currentAccerlation, TimeSpan delta,
            float friction = DefaultFriction)
        {
            if (friction < 0)
                throw new ArgumentOutOfRangeException(nameof(friction));

            float deltaSeconds = (float)delta.TotalSeconds;
            if (deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            Vector2 currentPosition = x;
            while (deltaSeconds > 0)
            {
                deltaSeconds -= StepSizeSeconds;
                float stepSeconds =
                    Math.Max(0, Math.Min(StepSizeSeconds,
                    deltaSeconds));

                Vector2 direction = y - x;
                if (direction.Length() > float.Epsilon)
                {
                    currentAccerlation = currentAccerlation * (friction + 1)
                        + direction * (friction + 2);
                    currentPosition += currentAccerlation * stepSeconds;
                }
            }
            return currentPosition;
        }

        /// <summary>
        /// Calculates the advancement of a value in a given time.
        /// </summary>
        /// <param name="x">The current value.</param>
        /// <param name="y">The target value.</param>
        /// <param name="currentAccerlation">
        /// The current accerlation, which will be modified by this method.
        /// </param>
        /// <param name="delta">
        /// The amount of time for which the accerlation should be calculated.
        /// </param>
        /// <param name="friction">
        /// The friction, which will affect how the accerlation builds up
        /// and decreases.
        /// </param>
        /// <returns>A new <see cref="Vector3D"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="friction"/> is less than 0 or when
        /// <paramref name="delta"/> is negative.
        /// </exception>
        public static Vector3 Accerlate(in Vector3 x, in Vector3 y,
            ref Vector3 currentAccerlation, TimeSpan delta,
            float friction = DefaultFriction)
        {
            if (friction < 0)
                throw new ArgumentOutOfRangeException(nameof(friction));

            float deltaSeconds = (float)delta.TotalSeconds;
            if (deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            Vector3 currentPosition = x;
            while (deltaSeconds > 0)
            {
                deltaSeconds -= StepSizeSeconds;
                float stepSeconds =
                    Math.Max(0, Math.Min(StepSizeSeconds,
                    deltaSeconds));

                Vector3 direction = y - x;
                if (direction.Length() > float.Epsilon)
                {
                    currentAccerlation = currentAccerlation * (friction + 1)
                        + direction * (friction + 2);
                    currentPosition += currentAccerlation * stepSeconds;
                }
            }
            return currentPosition;
        }
#endif
    }
}
