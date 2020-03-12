using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eterra.Engine.Common
{
    public static class CoordinateSystem
    {
        /// <summary>
        /// Gets a normalized vector which points to the left.
        /// </summary>
        public static ref readonly Vector3D Left => ref left;
        private static readonly Vector3D left = new Vector3D(-1, 0, 0);

        /// <summary>
        /// Gets a normalized vector which points to the right.
        /// </summary>
        public static ref readonly Vector3D Right => ref right;
        private static readonly Vector3D right = new Vector3D(1, 0, 0);

        /// <summary>
        /// Gets a normalized vector which points up.
        /// </summary>
        public static ref readonly Vector3D Up => ref up;
        private static readonly Vector3D up = new Vector3D(0, 1, 0);

        /// <summary>
        /// Gets a normalized vector which points down.
        /// </summary>
        public static ref readonly Vector3D Down => ref down;
        private static readonly Vector3D down = new Vector3D(0, -1, 0);

        /// <summary>
        /// Gets a normalized vector which points forward.
        /// </summary>
        public static ref readonly Vector3D Forward => ref forward;
        private static readonly Vector3D forward = new Vector3D(0, 0, 1);

        /// <summary>
        /// Gets a normalized vector which points backward.
        /// </summary>
        public static ref readonly Vector3D Backward => ref backward;
        private static readonly Vector3D backward = new Vector3D(0, 0, -1);

        /// <summary>
        /// Converts a <see cref="Vector3D"/> instance from either right-handed
        /// to left-handed or from left-handed to right-handed by inverting the
        /// <see cref="Vector3D.Z"/> component.
        /// </summary>
        /// <param name="vector">The input vector.</param>
        /// <returns>
        /// A new <see cref="Vector3D"/> instance with an inverted Z component.
        /// </returns>
        public static Vector3D ConvertLeftRightHanded(Vector3D vector)
        {
            return vector * Backward;
        }

        public static Vector3D Rotate(Vector3D point, Vector4D rotation)
        {
            return rotation * point;
        }

        public static Vector3D Rotate(Vector3D point, Vector3D pivot,
            Vector4D rotation)
        {
            return Rotate(point - pivot, rotation) + pivot;
        }

        /// <summary>
        /// Rotates the current rotation using another rotation
        /// and returns the rotated vector.
        /// </summary>
        /// <param name="rotation">
        /// The rotation to be combined with the current quaternion.
        /// </param>
        /// <returns>
        /// A new rotation quaternion.
        /// </returns>
        public static Vector4D Rotate(Vector4D baseRotation, 
            Vector4D rotation)
        {
            return rotation * baseRotation;
        }

        /// <summary>
        /// Creates a new four-dimensional vector/quaternion
        /// from three-dimensional euler rotation values.
        /// </summary>
        /// <param name="eulerX">
        /// The euler rotation X component.
        /// </param>
        /// <param name="eulerY">
        /// The euler rotation Y component.
        /// </param>
        /// <param name="eulerZ">
        /// The euler rotation Z component.
        /// </param>
        public static Vector4D CreateRotation(Angle eulerX, Angle eulerY, 
            Angle eulerZ)
        {
            float cx = (float)Math.Cos((eulerX.Radians) / 2);
            float sx = (float)Math.Sin((eulerX.Radians) / 2);
            float cy = (float)Math.Cos((eulerY.Radians) / 2);
            float sy = (float)Math.Sin((eulerY.Radians) / 2);
            float cz = (float)Math.Cos((eulerZ.Radians) / 2);
            float sz = (float)Math.Sin((eulerZ.Radians) / 2);

            float x = cy * cz * sx - sy * sz * cx;
            float y = sy * cz * cx - cy * sz * sx;
            float z = cy * sz * cx + sy * cz * sx;
            float w = cy * cz * cx + sy * sz * sx;

            return new Vector4D(x, y, z, w);
        }

        public static Vector4D CreateRotationFromRadians(Vector3D eulerRadians)
        {
            return CreateRotation(Angle.Rad(eulerRadians.X),
                Angle.Rad(eulerRadians.Y),
                Angle.Rad(eulerRadians.Z));
        }

        public static Vector4D CreateRotationFromDegrees(Vector3D eulerDegrees)
        {
            return CreateRotation(Angle.Deg(eulerDegrees.X),
                Angle.Deg(eulerDegrees.Y),
                Angle.Deg(eulerDegrees.Z));
        }

        /// <summary>
        /// Creates a transformation matrix.
        /// </summary>
        /// <param name="position">The position vector.</param>
        /// <returns>A new transformation matrix.</returns>
        public static Matrix4D CreateTransformationTranslation(
            in Vector3D position)
        {
            return new Matrix4D(1, 0, 0, position.X,
                0, 1, 0, position.Y,
                0, 0, 1, position.Z,
                0, 0, 0, 1);
        }

        /// <summary>
        /// Creates a transformation matrix.
        /// </summary>
        /// <param name="scale">The scale vector.</param>
        /// <returns>A new transformation matrix.</returns>
        public static Matrix4D CreateTransformationScale(in Vector3D scale)
        {
            return new Matrix4D(scale.X, 0, 0, 0,
                0, scale.Y, 0, 0,
                0, 0, scale.Z, 0,
                0, 0, 0, 1);
        }

        /// <summary>
        /// Creates a transformation matrix.
        /// </summary>
        /// <param name="rotation">The rotation quaternion.</param>
        /// <returns>A new transformation matrix.</returns>
        public static Matrix4D CreateTransformationRotation(
            in Vector4D rotation)
        {
            Vector4D r = rotation.ToNormalized();

            float x = r.X, y = r.Y,
                z = r.Z, w = r.W;

            float tWW = w * w;
            float tXX = x * x;
            float tYY = y * y;
            float tZZ = z * z;
            float tXY = x * y;
            float tZW = z * w;
            float tXZ = x * z;
            float tYW = y * w;
            float tYZ = y * z;
            float tXW = x * w;

            float a11 = (tXX - tYY - tZZ + tWW);
            float a22 = (-tXX + tYY - tZZ + tWW);
            float a33 = (-tXX - tYY + tZZ + tWW);
            float a21 = 2.0f * (tXY + tZW);
            float a12 = 2.0f * (tXY - tZW);
            float a31 = 2.0f * (tXZ - tYW);
            float a13 = 2.0f * (tXZ + tYW);
            float a32 = 2.0f * (tYZ + tXW);
            float a23 = 2.0f * (tYZ - tXW);

            return new Matrix4D(a11, a12, a13, 0,
                a21, a22, a23, 0,
                a31, a32, a33, 0,
                0, 0, 0, 1);
        }

        /// <summary>
        /// Creates a transformation matrix out of a position.
        /// </summary>
        /// <param name="position">The position vector.</param>
        /// <returns>A new transformation matrix.</returns>
        public static Matrix4D CreateTransformation(in Vector3D position)
        {
            return CreateTransformationTranslation(position);
        }

        /// <summary>
        /// Creates a transformation matrix out of a position and rotation.
        /// </summary>
        /// <param name="position">The position vector.</param>
        /// <param name="rotation">The rotation quaternion.</param>
        /// <returns>A new transformation matrix.</returns>
        public static Matrix4D CreateTransformation(in Vector3D position,
           in Vector4D rotation)
        {
            return CreateTransformationTranslation(position) 
                * CreateTransformationRotation(rotation);
        }

        /// <summary>
        /// Creates a transformation matrix out of a position and a scale.
        /// </summary>
        /// <param name="position">The position vector.</param>
        /// <param name="scale">The scale vector.</param>
        /// <returns>A new transformation matrix.</returns>
        public static Matrix4D CreateTransformation(in Vector3D position,
            in Vector3D scale)
        {
            return CreateTransformationTranslation(position) 
                * CreateTransformationScale(scale);
        }

        /// <summary>
        /// Creates a transformation matrix out of a position,
        /// scale and rotation.
        /// </summary>
        /// <param name="position">The position vector.</param>
        /// <param name="scale">The scale vector.</param>
        /// <param name="rotation">The rotation quaternion.</param>
        /// <returns>A new transformation matrix.</returns>
        public static Matrix4D CreateTransformation(in Vector3D position,
            in Vector3D scale, in Vector4D rotation)
        {
            return CreateTransformationTranslation(position) 
                * CreateTransformationRotation(rotation)
                * CreateTransformationScale(scale);
        }

        /// <summary>
        /// Creates an absolute transformation matrix out of a relative
        /// transformation and its absolute "parent" transformation.
        /// </summary>
        /// <param name="rootTransformation">
        /// The absolute root transformation matrix.
        /// </param>
        /// <param name="relativeTransformation">
        /// The relative transformation matrix.
        /// </param>
        /// <returns>A new transformation matrix.</returns>
        public static Matrix4D CreateTransformationCombined(
            in Matrix4D rootTransformation, 
            in Matrix4D relativeTransformation)
        {
            return rootTransformation * relativeTransformation;
        }

        /// <summary>
        /// Extracts the translation <see cref="Vector3D"/> from a 
        /// transformation <see cref="Matrix4D"/>.
        /// </summary>
        /// <returns>A new <see cref="Vector3D"/> instance.</returns>
        public static Vector3D ExtractTransformationTranslation(
            in Matrix4D transformation)
        {
            return new Vector3D(transformation.A14, transformation.A24,
                transformation.A34);
        }

        /// <summary>
        /// Extracts the scale <see cref="Vector3D"/> from the current 
        /// transformation <see cref="Matrix4D"/>.
        /// </summary>
        /// <returns>A new <see cref="Vector3D"/> instance.</returns>
        public static Vector3D ExtractTransformationScale(
            in Matrix4D transformation)
        {
            Vector3D column01 = new Vector3D(transformation.A11,
                transformation.A21, transformation.A31);
            Vector3D column02 = new Vector3D(transformation.A12,
                transformation.A22, transformation.A32);
            Vector3D column03 = new Vector3D(transformation.A13,
                transformation.A23, transformation.A33);

            return new Vector3D(column01.GetLength(), column02.GetLength(),
                column03.GetLength());
        }

        /// <summary>
        /// Extracts the rotation <see cref="Vector4D"/> from the current 
        /// transformation <see cref="Matrix4D"/>.
        /// </summary>
        /// <returns>A new <see cref="Vector4D"/> instance.</returns>
        public static Vector4D ExtractTransformationRotation(
            in Matrix4D transformation)
        {
            Vector3D scaleVector = ExtractTransformationScale(transformation);
            float sx = scaleVector.X, sy = scaleVector.Y, sz = scaleVector.Z;

            float a11 = transformation.A11 / sx, 
                a12 = transformation.A12 / sy, 
                a13 = transformation.A13 / sz,
                a21 = transformation.A21 / sx, 
                a22 = transformation.A22 / sy, 
                a23 = transformation.A23 / sz,
                a31 = transformation.A31 / sx, 
                a32 = transformation.A32 / sy, 
                a33 = transformation.A33 / sz;

            //https://d3cw3dd2w32x2b.cloudfront.net/wp-content/uploads/2015/01/matrix-to-quat.pdf
            float t;
            Vector4D r;
            if (a33 < 0)
            {
                if (a11 > a22)
                {
                    t = 1 + a11 - a22 - a33;
                    r = new Vector4D(t, a12 + a21, a31 + a13, a23 - a32);
                }
                else
                {
                    t = 1 - a11 + a22 - a33;
                    r = new Vector4D(a12 + a21, t, a23 + a32, a31 - a13);
                }
            }
            else
            {
                if (a11 < -a22)
                {
                    t = 1 - a11 - a22 + a33;
                    r = new Vector4D(a31 + a13, a23 + a32, t, a12 - a21);
                }
                else
                {
                    t = 1 + a11 + a22 + a33;
                    r = new Vector4D(a23 - a32, a31 - a13, a12 - a21, t);
                }
            }
            return r * (float)(0.5 / Math.Sqrt(t));
        }

        /// <summary>
        /// Creates a rotation which points towards a specific location.
        /// </summary>
        /// <param name="basePosition">
        /// The position, from which the target should be pointed towards.
        /// </param>
        /// <param name="lookAtTarget">
        /// The target towards the rotation will point.
        /// </param>
        public static Vector4D CreateRotation(in Vector3D basePosition,
            in Vector3D lookAtTarget)
        {
            //Source: http://answers.unity3d.com/questions/819699/calculate-quaternionlookrotation-manually.html
            Vector3D rootedTarget = (lookAtTarget - basePosition);
            if (rootedTarget != Vector3D.Zero)
            {
                if (rootedTarget != Up)
                {
                    Vector3D vector = rootedTarget + Up
                        * (Up.Dot(rootedTarget) * -1);
                    Vector4D rotation = FromToRotation(Forward,
                        vector);
                    return FromToRotation(vector, lookAtTarget) * rotation;
                }
                else return FromToRotation(Forward, rootedTarget);
            }
            else return Vector4D.Identity;
        }

        private static Vector4D FromToRotation(in Vector3D a, in Vector3D b)
        {
            //Source: http://lolengine.net/blog/2014/02/24/quaternion-from-two-vectors-final
            float normUV = (float)Math.Sqrt(a.Dot(a) * b.Dot(b));
            float w = normUV + a.Dot(b);

            if (w < 0.000001 * normUV)
            {
                if (Math.Abs(a.X) > Math.Abs(a.Z))
                    return new Vector4D(-a.Y, a.X, 0, 0f).ToNormalized();
                else
                    return new Vector4D(0, -a.Z, a.Y, 0f).ToNormalized();
            }
            else
            {
                Vector3D aXb = a.Cross(b);
                return new Vector4D(aXb.X, aXb.Y, aXb.Z, w);
            }
        }

        /*
        /// <summary>
        /// Creates a new four-dimensional quaternion from a 
        /// three-dimensional rotation matrix.
        /// </summary>
        /// <param name="a11">
        /// The value of the first cell in the first row.
        /// </param>
        /// <param name="a12">
        /// The value of the second cell in the first row.
        /// </param>
        /// <param name="a13">
        /// The value of the third cell in the first row.
        /// </param>
        /// <param name="a21">
        /// The value of the first cell in the second row.
        /// </param>
        /// <param name="a22">
        /// The value of the second cell in the second row.
        /// </param>
        /// <param name="a23">
        /// The value of the third cell in the second row.
        /// </param>
        /// <param name="a31">
        /// The value of the first cell in the third row.
        /// </param>
        /// <param name="a32">
        /// The value of the second cell in the third row.
        /// </param>
        /// <param name="a33">
        /// The value of the third cell in the third row.
        /// </param>
        public Vector4D(float a11, float a12, float a13,
            float a21, float a22, float a23,
            float a31, float a32, float a33)
        {
            W = (float)(Math.Sqrt(1.0 + a11 + a22 + a33) / 2.0);
            float w4 = W * 4.0f;
            X = ((a32 - a23) / w4);
            Y = ((a13 - a31) / w4);
            Z = ((a21 - a12) / w4);
        }*/

        /// <summary>
        /// Rotates the current rotation using another euler rotation and
        /// returns the rotated vector.
        /// </summary>
        /// <param name="eulerX">
        /// The euler rotation X component in degrees.
        /// </param>
        /// <param name="eulerY">
        /// The euler rotation Y component in degrees.
        /// </param>
        /// <param name="eulerZ">
        /// The euler rotation Z component in degrees.
        /// </param>
        /// <param name="angleType">
        /// The value type of the euler rotation components.
        /// </param>
        /// <returns>
        /// A new rotation quaternion.
        /// </returns>
        public Vector4D Rotated(Angle eulerX, Angle eulerY, Angle eulerZ)
        {
            return new Vector4D(eulerX, eulerY, eulerZ) * this;
        }

        /// <summary>
        /// Rotates the current rotation using an two-dimensional
        /// euler rotation along the X/Y-plane (e.g. for mouse-controlled 
        /// camera rotation) and returns the result as a new rotation.
        /// </summary>
        /// <param name="eulerX">The euler X rotation.</param>
        /// <param name="eulerY">The euler Y rotation.</param>
        /// <returns>A new rotation quaternion.</returns>
        public Vector4D Rotated(Angle eulerX, Angle eulerY)
        {
            return new Vector4D(eulerY, Angle.Zero, Angle.Zero) * this
                * new Vector4D(Angle.Zero, eulerX, Angle.Zero);
        }

        /// <summary>
        /// Converts the current rotation quaternion to 
        /// three-dimensional euler rotation values.
        /// </summary>
        /// <param name="eulerX">The euler X rotation angle.</param>
        /// <param name="eulerY">The euler Y rotation angle.</param>
        /// <param name="eulerZ">The euler Z rotation angle.</param>
        public void ToEuler(out Angle eulerX, out Angle eulerY,
            out Angle eulerZ)
        {
            float sqw = W * W;
            float sqx = X * X;
            float sqy = Y * Y;
            float sqz = Z * Z;

            eulerX = Angle.Rad((float)Math.Atan2(2.0 * (Y * Z + X * W),
                -sqx - sqy + sqz + sqw), false);
            eulerY = Angle.Rad((float)Math.Asin(-2.0 * (X * Z - Y * W)
                / (sqx + sqy + sqz + sqw)), false);
            eulerZ = Angle.Rad((float)Math.Atan2(2.0 * (X * Y + Z * W),
                    sqx - sqy - sqz + sqw), false);
        }

        /// <summary>
        /// Converts the current rotation quaternion to 
        /// three-dimensional euler rotation values in degrees.
        /// </summary>
        /// <returns>
        /// A new <see cref="Vector3D"/>.
        /// </returns>
        public Vector3D ToEulerDeg()
        {
            ToEuler(out Angle x, out Angle y, out Angle z);
            return new Vector3D(x.Degrees, y.Degrees, z.Degrees);
        }

        /// <summary>
        /// Converts the current rotation quaternion to 
        /// three-dimensional euler rotation values in radians.
        /// </summary>
        /// <returns>
        /// A new <see cref="Vector3D"/>.
        /// </returns>
        public Vector3D ToEulerRad()
        {
            ToEuler(out Angle x, out Angle y, out Angle z);
            return new Vector3D(x.Radians, y.Radians, z.Radians);
        }

        /// <summary>
        /// Converts the current rotation quaternion to 
        /// three-dimensional euler rotation values in Pi radians.
        /// </summary>
        /// <returns>
        /// A new <see cref="Vector3D"/>.
        /// </returns>
        public Vector3D ToEulerPiRad()
        {
            ToEuler(out Angle x, out Angle y, out Angle z);
            return new Vector3D(x.PiRadians, y.PiRadians, z.PiRadians);
        }
    }
}
