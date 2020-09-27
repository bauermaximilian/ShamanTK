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

using ShamanTK.IO;
using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace ShamanTK.Common
{
    /// <summary>
    /// Describes the available primitive types for a 
    /// <see cref="ColliderPrimitive"/>.
    /// </summary>
    public enum ColliderPrimitiveType
    {
        /// <summary>
        /// The default value for an empty <see cref="ColliderPrimitive"/>.
        /// </summary>
        None,
        /// <summary>
        /// A cuboid box.
        /// </summary>
        Box,
        /// <summary>
        /// A sphere.
        /// </summary>
        Sphere,
        /// <summary>
        /// A cylinder with a flat surface both on the base and the top.
        /// </summary>
        Cylinder,
        /// <summary>
        /// A pointed cone with a flat surface at the base.
        /// </summary>
        Cone,
        /// <summary>
        /// A capsule, which looks like a cone but with a hemisphere instead of
        /// a flat surface on the base and the top.
        /// </summary>
        Capsule
    }

    /// <summary>
    /// Represents a collider, which consists of a single primitive shape.
    /// For describing the physical properties in terms of motion, see
    /// <see cref="Dynamics"/>.
    /// </summary>
    public readonly struct ColliderPrimitive : IEquatable<ColliderPrimitive>
    {
        /// <summary>
        /// Gets an empty <see cref="ColliderPrimitive"/> instance with a
        /// <see cref="Type"/> of <see cref="ColliderPrimitiveType.None"/>.
        /// </summary>
        public static ColliderPrimitive Empty { get; } =
            new ColliderPrimitive(0, 0, 0, ColliderPrimitiveType.None);

        /// <summary>
        /// Gets the width of the current <see cref="ColliderPrimitive"/>
        /// instance (the dimension on the X-axis).
        /// </summary>
        public float Width => Dimensions.X;

        /// <summary>
        /// Gets the height of the current <see cref="ColliderPrimitive"/>
        /// instance (the dimension on the Y-axis).
        /// </summary>
        public float Height => Dimensions.Y;

        /// <summary>
        /// Gets the depth of the current <see cref="ColliderPrimitive"/>
        /// instance (the dimension on the Z-axis).
        /// </summary>
        public float Depth => Dimensions.Z;

        /// <summary>
        /// Gets the dimensions of the current <see cref="ColliderPrimitive"/>
        /// instance.
        /// </summary>
        public Vector3 Dimensions { get; }

        /// <summary>
        /// Gets the radius (equal to the <see cref="Width"/>) of the current
        /// <see cref="ColliderPrimitive"/> instance.
        /// </summary>
        public float Diameter => Width;

        /// <summary>
        /// Gets the <see cref="ColliderPrimitiveType"/> of the current
        /// <see cref="ColliderPrimitive"/> instance.
        /// </summary>
        public ColliderPrimitiveType Type { get; }

        /// <summary>
        /// Gets a value indicating whether the current 
        /// <see cref="ColliderPrimitive"/> is empty
        /// (with a <see cref="Type"/> of 
        /// <see cref="ColliderPrimitiveType.None"/> and all dimensions set 
        /// to 0) (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsEmpty => this == Empty;

        private ColliderPrimitive(float width, float height, float depth,
            ColliderPrimitiveType type)
        {
            Dimensions = new Vector3(width, height, depth);
            Type = type;
        }

        /// <summary>
        /// Creates a new <see cref="ColliderPrimitive"/> instance with the
        /// <see cref="Type"/> <see cref="ColliderPrimitiveType.Box"/>.
        /// </summary>
        /// <param name="width">The width of the shape (X-axis).</param>
        /// <param name="height">The height of the shape (Y-axis).</param>
        /// <param name="depth">The depth of the shape (Z-axis).</param>
        /// <returns>A new <see cref="ColliderPrimitive"/> instance.</returns>
        public static ColliderPrimitive CreateBox(float width, float height,
            float depth)
        {
            return new ColliderPrimitive(width, height, depth,
                ColliderPrimitiveType.Box);
        }

        /// <summary>
        /// Creates a new <see cref="ColliderPrimitive"/> instance with the
        /// <see cref="Type"/> <see cref="ColliderPrimitiveType.Sphere"/>.
        /// </summary>
        /// <param name="radius">The diameter of the shape (XZ-axis).</param>
        /// <returns>A new <see cref="ColliderPrimitive"/> instance.</returns>
        public static ColliderPrimitive CreateSphere(float diameter)
        {
            return new ColliderPrimitive(diameter, diameter, diameter,
                ColliderPrimitiveType.Sphere);
        }

        /// <summary>
        /// Creates a new <see cref="ColliderPrimitive"/> instance with the
        /// <see cref="Type"/> <see cref="ColliderPrimitiveType.Capsule"/>.
        /// </summary>
        /// <param name="diameter">The diameter of the shape (XZ-axis).</param>
        /// <param name="height">The height of the shape (Y-axis).</param>
        /// <returns>A new <see cref="ColliderPrimitive"/> instance.</returns>
        public static ColliderPrimitive CreateCapsule(float diameter, 
            float height)
        {
            return new ColliderPrimitive(diameter, height, diameter,
                ColliderPrimitiveType.Capsule);
        }

        /// <summary>
        /// Creates a new <see cref="ColliderPrimitive"/> instance with the
        /// <see cref="Type"/> <see cref="ColliderPrimitiveType.Cone"/>.
        /// </summary>
        /// <param name="diameter">The diameter of the shape (XZ-axis).</param>
        /// <param name="height">The height of the shape (Y-axis).</param>
        /// <returns>A new <see cref="ColliderPrimitive"/> instance.</returns>
        public static ColliderPrimitive CreateCone(float diameter, 
            float height)
        {
            return new ColliderPrimitive(diameter, height, diameter,
                ColliderPrimitiveType.Cone);
        }

        /// <summary>
        /// Creates a new <see cref="ColliderPrimitive"/> instance with the
        /// <see cref="Type"/> <see cref="ColliderPrimitiveType.Cylinder"/>.
        /// </summary>
        /// <param name="diameter">The diameter of the shape (XZ-axis).</param>
        /// <param name="height">The height of the shape (Y-axis).</param>
        /// <returns>A new <see cref="ColliderPrimitive"/> instance.</returns>
        public static ColliderPrimitive CreateCylinder(float diameter, 
            float height)
        {
            return new ColliderPrimitive(diameter, height, diameter,
                ColliderPrimitiveType.Cylinder);
        }

        /// <summary>
        /// Checks whether the current <see cref="ColliderPrimitive"/> 
        /// instance contains or collides with a vertex.
        /// </summary>
        /// <param name="offset">
        /// The offset position, which is applied to the collider before 
        /// checking whether the specified <paramref name="point"/> is inside
        /// or not.
        /// </param>
        /// <param name="point">
        /// The point which should be checked if it collides with the
        /// current <see cref="ColliderPrimitive"/> instance.
        /// </param>
        /// <param name="checkBoundingBoxOnly">
        /// <c>true</c> to ignore the current <see cref="Type"/> and only 
        /// check whether the point is within the bounding box defined by the
        /// current <see cref="Dimensions"/> and the specified 
        /// <paramref name="offset"/>, <c>false</c> to perform a full
        /// collision check.
        /// Note that <c>false</c> as parameter value is not supported yet.
        /// </param>
        /// <returns>
        /// <c>true</c> if the point is contained within or collides with
        /// the current <see cref="ColliderPrimitive"/> instance moved to the
        /// specified <paramref name="offset"/>, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <paramref name="checkBoundingBoxOnly"/> is 
        /// <c>false</c>. This exception will be removed in future versions.
        /// </exception>
        public bool Intersects(Vector3 offset, Vector3 point, 
            bool checkBoundingBoxOnly = true)
        {
            if (IsEmpty) return false;

            if (!checkBoundingBoxOnly)
                throw new NotSupportedException("Shape-aware collision " +
                    "checking is not supported yet.");
            //TODO: Implement shape-aware collision checking.

            Vector3 minimumOffsetted = offset - new Vector3(Width, Height, 
                Depth) / 2;
            Vector3 maximumOffsetted = offset + new Vector3(Width, Height,
                Depth) / 2;

            return (point.X <= maximumOffsetted.X &&
                point.X >= minimumOffsetted.X) &&
                (point.Y <= maximumOffsetted.Y &&
                point.Y >= minimumOffsetted.Y) &&
                (point.Z <= maximumOffsetted.Z &&
                point.Z >= minimumOffsetted.Z);
        }

        /// <summary>
        /// Checks whether the current <see cref="ColliderPrimitive"/> 
        /// instance contains or collides with another 
        /// <see cref="ColliderPrimitive"/> instance.
        /// Note that this current implementation ignores the 
        /// <see cref="Type"/> and only checks if the bounding box defined 
        /// by the <see cref="Dimensions"/> of the two 
        /// <see cref="ColliderPrimitive"/> instances intersect.
        /// </summary>
        /// <param name="offset">
        /// The offset position, which is applied to the current collider 
        /// before performing the collision check.
        /// </param>
        /// <param name="other">
        /// The other <see cref="ColliderPrimitive"/> instance which should
        /// be checked whether it collides with the current instance.
        /// </param>
        /// <param name="otherOffset">
        /// The offset position, which is applied to the 
        /// <paramref name="other"/> <see cref="ColliderPrimitive"/> instance
        /// before performing the collision check.
        /// </param>
        /// <param name="checkBoundingBoxOnly">
        /// <c>true</c> to ignore the current <see cref="Type"/> and only 
        /// check whether the point is within the bounding box defined by the
        /// current <see cref="Dimensions"/> and the specified 
        /// <paramref name="offset"/>, <c>false</c> to perform a full
        /// collision check.
        /// Note that <c>false</c> as parameter value is not supported yet.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="other"/>
        /// <see cref="ColliderPrimitive"/> instance (at the specified
        /// <paramref name="otherOffset"/> position) is contained within or 
        /// collides with the current <see cref="ColliderPrimitive"/> instance 
        /// (moved to the specified <paramref name="offset"/>), 
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <paramref name="checkBoundingBoxOnly"/> is 
        /// <c>false</c>. This exception will be removed in future versions.
        /// </exception>
        public bool Intersects(Vector3 offset, ColliderPrimitive other,
            Vector3 otherOffset, bool checkBoundingBoxOnly = true)
        {
            if (IsEmpty) return false;

            if (!checkBoundingBoxOnly)
                throw new NotSupportedException("Shape-aware collision " +
                    "checking is not supported yet.");

            Vector3 minimumOffsetted = offset - Dimensions / 2;
            Vector3 maximumOffsetted = offset + Dimensions / 2;
            Vector3 otherMinimumOffsetted = otherOffset - other.Dimensions / 2;
            Vector3 otherMaximumOffsetted = otherOffset + other.Dimensions / 2;

            return (minimumOffsetted.X <= otherMaximumOffsetted.X &&
                maximumOffsetted.X >= otherMinimumOffsetted.X) &&
                (minimumOffsetted.Y <= otherMaximumOffsetted.Y &&
                maximumOffsetted.Y >= otherMinimumOffsetted.Y) &&
                (minimumOffsetted.Z <= otherMaximumOffsetted.Z &&
                maximumOffsetted.Z >= otherMinimumOffsetted.Z);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// This string can be parsed into a new 
        /// <see cref="ColliderPrimitive"/> instance with the same parameters
        /// with <see cref="TryParse(string, out ColliderPrimitive)"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current object.
        /// </returns>
        /// <remarks>
        /// The <see cref="CultureInfo.InvariantCulture"/> is used for the
        /// dimensions of the <see cref="ColliderPrimitive"/>.
        /// </remarks>
        public override string ToString()
        {
            CultureInfo c = CultureInfo.InvariantCulture;

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Type.ToString());

            if (Type != ColliderPrimitiveType.None)
            {
                stringBuilder.Append(" (");

                if (Type == ColliderPrimitiveType.Box)
                {
                    stringBuilder.Append(Width.ToString(c));
                    stringBuilder.Append(',');
                    stringBuilder.Append(Height.ToString(c));
                    stringBuilder.Append(',');
                    stringBuilder.Append(Depth.ToString(c));
                }
                else if (Type == ColliderPrimitiveType.Sphere)
                {
                    stringBuilder.Append(Diameter.ToString(c));
                }
                else
                {
                    stringBuilder.Append(Diameter.ToString(c));
                    stringBuilder.Append(',');
                    stringBuilder.Append(Height.ToString(c));
                    stringBuilder.Append(',');
                }

                stringBuilder.Append(')');
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the string representation of a 
        /// <see cref="ColliderPrimitive"/> into an instance of that type
        /// with the values from the string.
        /// </summary>
        /// <param name="s">
        /// The input string which should be parsed.
        /// </param>
        /// <returns>
        /// The new parsed <see cref="ColliderPrimitive"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="s"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when <paramref name="s"/> is no valid string 
        /// representation of a <see cref="ColliderPrimitive"/> instance.
        /// </exception>
        public static ColliderPrimitive Parse(string s)
        {
            if (TryParse(s, out ColliderPrimitive result)) return result;
            else throw new FormatException("The specified string was no " +
                "valid " + nameof(ColliderPrimitive) 
                + " string representation.");
        }

        /// <summary>
        /// Converts the string representation of a 
        /// <see cref="ColliderPrimitive"/> into an instance of that type
        /// with the values from the string.
        /// </summary>
        /// <param name="s">
        /// The input string which should be parsed.
        /// </param>
        /// <param name="result">
        /// The new parsed <see cref="ColliderPrimitive"/> instance or
        /// <see cref="Empty"/>, if the process failed.
        /// </param>
        /// <returns>
        /// <c>true</c> if the conversion was successful and 
        /// <paramref name="result"/> contains a valid 
        /// <see cref="ColliderPrimitive"/> instance, <c>false</c> when the
        /// conversion failed and <paramref name="result"/> is 
        /// <see cref="Empty"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="s"/> is null.
        /// </exception>
        /// <remarks>
        /// The numeric dimensions in the string are assumed to be in the 
        /// <see cref="CultureInfo.InvariantCulture"/>.
        /// </remarks>
        public static bool TryParse(string s, out ColliderPrimitive result)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            CultureInfo c = CultureInfo.InvariantCulture;
            NumberStyles ns = NumberStyles.Float;

            result = Empty;

            string[] parts = s.Replace(" ", "").Trim(')').Split('(', ',');

            if (!Enum.TryParse(parts[0], true, out ColliderPrimitiveType type))
                return false;

            //The width is the same like the radius,
            //the height is the same like the length.
            float width = 0, height = 0, depth = 0;

            if (type != ColliderPrimitiveType.None)
            {
                if (!(parts.Length > 1 && float.TryParse(parts[1], ns, c,
                    out width))) return false;

                if (type != ColliderPrimitiveType.Sphere)
                {
                    if (!(parts.Length > 2 && float.TryParse(parts[2], ns, c,
                        out height))) return false;

                    if (type == ColliderPrimitiveType.Box)
                    {
                        if (!(parts.Length > 3 && float.TryParse(parts[3], ns,
                            c, out depth))) return false;
                    }
                    else depth = width;
                }
                else height = depth = width;

                //While these assignments above might not be required,
                //a sphere with a height, depth and width of the same value
                //(double of the radius) would probably make more sense to 
                //programmers than if these values were 0. The same with 
                //the other tank-like shapes.
            }

            result = new ColliderPrimitive(width, height, depth, type);
            return true;
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
            return obj is ColliderPrimitive && Equals((ColliderPrimitive)obj);
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
        public bool Equals(ColliderPrimitive other)
        {
            return Width == other.Width &&
                   Height == other.Height &&
                   Depth == other.Depth &&
                   Type == other.Type;
        }

        /// <summary>
        /// Calculates the hash of the current object instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            var hashCode = -2128931952;
            hashCode = hashCode * -1521134295 + Width.GetHashCode();
            hashCode = hashCode * -1521134295 + Height.GetHashCode();
            hashCode = hashCode * -1521134295 + Depth.GetHashCode();
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(ColliderPrimitive primitive1, 
            ColliderPrimitive primitive2)
        {
            return primitive1.Equals(primitive2);
        }

        public static bool operator !=(ColliderPrimitive primitive1, 
            ColliderPrimitive primitive2)
        {
            return !(primitive1 == primitive2);
        }
    }
}