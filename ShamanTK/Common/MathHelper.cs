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
using System.Diagnostics;
using System.Numerics;

namespace ShamanTK.Common
{
    /// <summary>
    /// Provides helper methods for various mathematical operations with 
    /// vectors, quaternions and matrices.
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// Defines the "start of time" for time-based operations, which is 
        /// usually the time of the first usage of the <see cref="MathHelper"/>
        /// class.
        /// </summary>
        /// <remarks>
        /// As the <see cref="Stopwatch"/> class uses a <see cref="long"/> 
        /// for storing its time, operations using this value could yield 
        /// incorrect results after an application runtime of ~10675199 days.
        /// </remarks>
        private static readonly Stopwatch timeStart = Stopwatch.StartNew();

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

        /// <summary>
        /// Gets a sine of the current time.
        /// </summary>
        /// <param name="oscillationsPerSecond">
        /// The amount of oscillations/cycles per second.
        /// </param>
        /// <param name="amplitude">
        /// The peak deviation of the function from zero.
        /// </param>
        /// <returns>A <see cref="float"/>.</returns>
        public static float CalculateTimeSine(double oscillationsPerSecond, 
            double amplitude = 1)
        {
            return (float)(Math.Sin(2 * Math.PI * oscillationsPerSecond *
                timeStart.Elapsed.TotalSeconds) * amplitude);
        }

        /// <summary>
        /// Gets a cyclic value using the current time.
        /// </summary>
        /// <param name="progressPerSecond">
        /// The amount the cyclic value advances per second.
        /// </param>
        /// <param name="restartAt">
        /// The maximum value of the cyclic value, at which the cycle starts
        /// at zero again.
        /// </param>
        /// <returns>A <see cref="float"/>.</returns>
        public static float CalculateTimeCycle(double progressPerSecond,
            double restartAt)
        {
            return (float)((timeStart.Elapsed.TotalSeconds * 
                progressPerSecond) % restartAt);
        }

        /// <summary>
        /// Aligns a <see cref="Vector3"/> to a specific euler orientation.
        /// </summary>
        /// <param name="vector">
        /// The vector to align to the orientation.
        /// </param>
        /// <param name="orientationEulerRad">
        /// The euler orientation with each component in radians.
        /// </param>
        /// <param name="ignoreOrientationX">
        /// <c>true</c> to ignore the <see cref="Vector3.X"/> component of
        /// the current <see cref="Orientation"/> to prevent the 
        /// <see cref="Vector3.Y"/> component of 
        /// the <paramref name="vector"/> to be altered, 
        /// <c>false</c> otherwise (default).
        /// </param>
        /// <param name="ignoreOrientationY">
        /// <c>true</c> to ignore the <see cref="Vector3.Y"/> component of
        /// the current <see cref="Orientation"/> to prevent the 
        /// <see cref="Vector3.X"/> and <see cref="Vector3.Z"/> component of 
        /// the <paramref name="vector"/> to be altered, 
        /// <c>false</c> otherwise (default).
        /// </param>
        /// <returns>
        /// A new <see cref="Vector3"/>.
        /// </returns>
        public static Vector3 AlignVector(in Vector3 vector,
            in Vector3 orientationEulerRad,
            bool ignoreRotationX,
            bool ignoreRotationY)
        {
            float orientationX = ignoreRotationX ? 0 : orientationEulerRad.X;
            float orientationY = ignoreRotationY ? 0 : orientationEulerRad.Y;

            Vector2 orientationSin = new Vector2((float)Math.Sin(orientationX),
                (float)Math.Sin(orientationY));
            Vector2 orientationCos = new Vector2((float)Math.Cos(orientationX),
                (float)Math.Cos(orientationY));

            return new Vector3(vector.X * orientationCos.Y +
                vector.Z * orientationSin.Y * orientationCos.X,
                vector.Y * (orientationCos.X >= 0 ? 1 : -1) -
                vector.Z * orientationSin.X,
                vector.Z * orientationCos.Y * orientationCos.X -
                vector.X * orientationSin.Y);
        }
    }
}
