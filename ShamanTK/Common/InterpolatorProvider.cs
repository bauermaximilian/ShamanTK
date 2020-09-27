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
using System.Reflection;

namespace ShamanTK.Common
{
    /// <summary>
    /// Provides an attribute for classes or structs which can be interpolated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, 
        Inherited = false, AllowMultiple = false)]
    public sealed class InterpolatorProviderAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the public, static property within the class or
        /// struct which returns an instance of an object which implements the
        /// <see cref="IInterpolator{T}"/> interface, where <c>T</c> specifies
        /// the type of the object or struct this attribute is assigned to.
        /// </summary>
        public string ProviderPropertyName { get; }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="InterpolatorProviderAttribute"/> attribute.
        /// </summary>
        /// <param name="providerPropertyName">
        /// The name of the public, static <see cref="IInterpolator{T}"/> 
        /// property.
        /// </param>
        public InterpolatorProviderAttribute(
            string providerPropertyName)
        {
            ProviderPropertyName = providerPropertyName ??
                throw new ArgumentNullException(nameof(providerPropertyName));
        }
    }

    /// <summary>
    /// Provides <see cref="IInterpolator{T}"/> instances for various 
    /// value types.
    /// </summary>
    public static class InterpolatorProvider
    {
        #region Animator for numeric primitive datatypes
        private class AnimatorPrimitive : IInterpolator<bool>, 
            IInterpolator<byte>,IInterpolator<sbyte>, IInterpolator<short>, 
            IInterpolator<ushort>, IInterpolator<int>, IInterpolator<uint>,
            IInterpolator<long>, IInterpolator<ulong>, IInterpolator<float>, 
            IInterpolator<double>, IInterpolator<decimal>,
            IInterpolator<TimeSpan>, IInterpolator<Vector2>,
            IInterpolator<Vector3>, IInterpolator<Quaternion>,
            IInterpolator<Matrix4x4>
        {
            ref readonly bool IInterpolator<bool>.Default => ref defBool;
            private readonly bool defBool = false;

            ref readonly byte IInterpolator<byte>.Default => ref defByte;
            private readonly byte defByte = 0;

            ref readonly sbyte IInterpolator<sbyte>.Default => ref defSbyte;
            private readonly sbyte defSbyte = 0;

            ref readonly short IInterpolator<short>.Default => ref defShort;
            private readonly short defShort = 0;

            ref readonly ushort IInterpolator<ushort>.Default => ref defUshort;
            private readonly ushort defUshort = 0;

            ref readonly int IInterpolator<int>.Default => ref defInt;
            private readonly int defInt = 0;

            ref readonly uint IInterpolator<uint>.Default => ref defUint;
            private readonly uint defUint = 0;

            ref readonly long IInterpolator<long>.Default => ref defLong;
            private readonly long defLong = 0;

            ref readonly ulong IInterpolator<ulong>.Default => ref defUlong;
            private readonly ulong defUlong = 0;

            ref readonly float IInterpolator<float>.Default => ref defFloat;
            private readonly float defFloat = 0;

            ref readonly double IInterpolator<double>.Default => ref defDouble;
            private readonly double defDouble = 0;

            ref readonly decimal IInterpolator<decimal>.Default => ref defDec;
            private readonly decimal defDec = 0;

            ref readonly Vector2 IInterpolator<Vector2>.Default => ref defVec2;
            private readonly Vector2 defVec2 = Vector2.Zero;

            ref readonly Vector3 IInterpolator<Vector3>.Default => ref defVec3;
            private readonly Vector3 defVec3 = Vector3.Zero;

            ref readonly Quaternion IInterpolator<Quaternion>.Default 
                => ref defQuat;
            private readonly Quaternion defQuat = Quaternion.Identity;

            ref readonly Matrix4x4 IInterpolator<Matrix4x4>.Default
                => ref defMat4x4;
            private readonly Matrix4x4 defMat4x4 = Matrix4x4.Identity;

            ref readonly TimeSpan IInterpolator<TimeSpan>.Default => ref defTi;
            private readonly TimeSpan defTi = TimeSpan.Zero;
            
            public bool InterpolateLinear(in bool x, in bool y, float ratio)
            {
                return InterpolateLinear(x ? 1f : 0f, y ? 1f : 0f, ratio) == 1;
            }

            public byte InterpolateLinear(in byte x, in byte y, float ratio)
            {
                return (byte)InterpolateLinear((float)x, y, ratio);
            }

            public sbyte InterpolateLinear(in sbyte x, in sbyte y, float ratio)
            {
                return (sbyte)InterpolateLinear((float)x, y, ratio);
            }

            public short InterpolateLinear(in short x, in short y, float ratio)
            {
                return (short)InterpolateLinear((float)x, y, ratio);
            }

            public ushort InterpolateLinear(in ushort x, in ushort y,
                float ratio)
            {
                return (ushort)InterpolateLinear((float)x, y, ratio);
            }

            public int InterpolateLinear(in int x, in int y, float ratio)
            {
                return (int)InterpolateLinear((double)x, y, ratio);
            }

            public uint InterpolateLinear(in uint x, in uint y, float ratio)
            {
                return (uint)InterpolateLinear((double)x, y, ratio);
            }

            public long InterpolateLinear(in long x, in long y, float ratio)
            {
                return (long)InterpolateLinear((decimal)x, y, ratio);
            }

            public ulong InterpolateLinear(in ulong x, in ulong y, float ratio)
            {
                return (ulong)InterpolateLinear((decimal)x, y, ratio);
            }

            public float InterpolateLinear(in float x, in float y, float ratio)
            {
                return (x + ratio * (y - x));
            }

            public double InterpolateLinear(in double x, in double y,
                float ratio)
            {
                return (x + ratio * (y - x));
            }

            public decimal InterpolateLinear(in decimal x, in decimal y,
                float ratio)
            {
                return (x + (decimal)ratio * (y - x));
            }

            public Vector2 InterpolateLinear(in Vector2 x, in Vector2 y,
                float ratioXY)
            {
                return Vector2.Lerp(x, y, ratioXY);
            }

            public Vector3 InterpolateLinear(in Vector3 x, in Vector3 y,
                float ratioXY)
            {
                return Vector3.Lerp(x, y, ratioXY);
            }

            public Quaternion InterpolateLinear(in Quaternion x,
                in Quaternion y, float ratioXY)
            {
                return Quaternion.Lerp(x, y, ratioXY);
            }

            public Matrix4x4 InterpolateLinear(in Matrix4x4 x, in Matrix4x4 y,
                float ratioXY)
            {
                return Matrix4x4.Lerp(x, y, ratioXY);
            }

            public TimeSpan InterpolateLinear(in TimeSpan x, in TimeSpan y,
                float ratio)
            {
                return new TimeSpan(InterpolateLinear(x.Ticks,
                    y.Ticks, ratio));
            }

            public bool InterpolateCubic(in bool beforeX, in bool x,
                in bool y, in bool afterY, float ratio)
            {
                return InterpolateCubic(beforeX ? 1f : 0f, x ? 1f : 0f,
                    y ? 1f : 0f, afterY ? 1f : 0f, ratio) == 1;
            }

            public byte InterpolateCubic(in byte beforeX, in byte x,
                in byte y, in byte afterY, float ratio)
            {
                return (byte)InterpolateCubic((float)beforeX, x, y, afterY, 
                    ratio);
            }

            public sbyte InterpolateCubic(in sbyte beforeX, in sbyte x,
                in sbyte y, in sbyte afterY, float ratio)
            {
                return (sbyte)InterpolateCubic((float)beforeX, x, y, afterY,
                    ratio);
            }

            public short InterpolateCubic(in short beforeX, in short x,
                in short y, in short afterY, float ratio)
            {
                return (short)InterpolateCubic((float)beforeX, x, y, afterY,
                    ratio);
            }

            public ushort InterpolateCubic(in ushort beforeX, in ushort x,
                in ushort y, in ushort afterY, float ratio)
            {
                return (ushort)InterpolateCubic((float)beforeX, x, y, afterY,
                    ratio);
            }

            public int InterpolateCubic(in int beforeX, in int x, in int y,
                in int afterY, float ratio)
            {
                return (int)InterpolateCubic((double)beforeX, x, y, afterY,
                    ratio);
            }

            public uint InterpolateCubic(in uint beforeX, in uint x,
                in uint y, in uint afterY, float ratio)
            {
                return (uint)InterpolateCubic((double)beforeX, x, y, afterY,
                    ratio);
            }

            public long InterpolateCubic(in long beforeX, in long x,
                in long y, in long afterY, float ratio)
            {
                return (long)InterpolateCubic((decimal)beforeX, x, y, afterY,
                    ratio);
            }

            public ulong InterpolateCubic(in ulong beforeX, in ulong x,
                in ulong y, in ulong afterY, float ratio)
            {
                return (ulong)InterpolateCubic((decimal)beforeX, x, y, afterY,
                    ratio);
            }

            public float InterpolateCubic(in float beforeX, in float x,
                in float y, in float afterY, float ratio)
            {
                float ratioSquared = ratio * ratio;
                float interimValue = afterY - y - beforeX + x;

                return interimValue * ratio * ratioSquared
                    + (beforeX - x - interimValue) * ratioSquared
                    + (y - beforeX) * ratio
                    + x;
            }

            public double InterpolateCubic(in double beforeX, in double x,
                in double y, in double afterY, float ratio)
            {
                double ratioPrec = ratio;
                double ratioPrecSquared = ratioPrec * ratioPrec;
                double interimValue = afterY - y - beforeX + x;

                return interimValue * ratioPrec * ratioPrecSquared
                    + (beforeX - x - interimValue) * ratioPrecSquared
                    + (y - beforeX) * ratioPrec
                    + x;
            }

            public decimal InterpolateCubic(in decimal beforeX, in decimal x,
                in decimal y, in decimal afterY, float ratio)
            {
                decimal ratioPrec = (decimal)ratio;
                decimal ratioPrecSquared = ratioPrec * ratioPrec;
                decimal interimValue = afterY - y - beforeX + x;

                return interimValue * ratioPrec * ratioPrecSquared
                    + (beforeX - x - interimValue) * ratioPrecSquared
                    + (y - beforeX) * ratioPrec
                    + x;
            }

            public Vector2 InterpolateCubic(in Vector2 beforeX, in Vector2 x,
                in Vector2 y, in Vector2 afterY, float ratioXY)
            {
                return MathHelper.InterpolateCubic(in beforeX, in x, in y,
                    in afterY, ratioXY);
            }

            public Vector3 InterpolateCubic(in Vector3 beforeX, in Vector3 x,
                in Vector3 y, in Vector3 afterY, float ratioXY)
            {
                return MathHelper.InterpolateCubic(in beforeX, in x, in y,
                    in afterY, ratioXY);
            }

            public Quaternion InterpolateCubic(in Quaternion beforeX,
                in Quaternion x, in Quaternion y, in Quaternion afterY,
                float ratioXY)
            {
                return Quaternion.Slerp(x, y, ratioXY);
            }

            public Matrix4x4 InterpolateCubic(in Matrix4x4 beforeX,
                in Matrix4x4 x, in Matrix4x4 y, in Matrix4x4 afterY,
                float ratioXY)
            {
                return MathHelper.InterpolateCubic(in beforeX, in x, in y,
                    in afterY, ratioXY);
            }

            public TimeSpan InterpolateCubic(in TimeSpan beforeX,
                in TimeSpan x, in TimeSpan y, in TimeSpan afterY,
                float ratio)
            {
                return new TimeSpan(InterpolateCubic(beforeX.Ticks, x.Ticks,
                    y.Ticks, afterY.Ticks, ratio));
            }
        }
        #endregion

        private static readonly AnimatorPrimitive animatorPrimitive
            = new AnimatorPrimitive();

        /// <summary>
        /// Checks if a specific type is supported by the current
        /// <see cref="InterpolatorProvider"/>. By default, all numeric 
        /// primitive datatypes and all value types with a valid
        /// <see cref="InterpolatorProviderAttribute"/> attribute are supported.
        /// </summary>
        /// <param name="type">
        /// The type to be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if the type is supported and can be used with
        /// <see cref="GetInterpolator{T}"/> to retrieve an animator for that
        /// type, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="type"/> is null.
        /// </exception>
        public static bool Supports(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (IsNumericPrimitiveType(type)) return true;
            else
            {
                try { GetInterpolator(type); return true; }
                catch { return false; }
            }
        }

        /// <summary>
        /// Checks whether a specific type is one of the following types:
        /// byte, sbyte, short, ushort, int, uint, long, ulong, float, double,
        /// decimal, bool, TimeSpan.
        /// </summary>
        /// <param name="type">The type to be checked.</param>
        /// <returns>
        /// <c>true</c> if the type is one of the previously mentioned types,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="type"/> is null.
        /// </exception>
        private static bool IsNumericPrimitiveType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type == typeof(byte) || type == typeof(sbyte)
                || type == typeof(short) || type == typeof(ushort)
                || type == typeof(int) || type == typeof(uint)
                || type == typeof(long) || type == typeof(ulong)
                || type == typeof(float) || type == typeof(double)
                || type == typeof(decimal) || type == typeof(bool)
                || type == typeof(TimeSpan) || type == typeof(Vector2)
                || type == typeof(Vector3) || type == typeof(Quaternion)
                || type == typeof(Matrix4x4);
        }

        /// <summary>
        /// Gets the <see cref="IInterpolator{T}"/> from a value type which is
        /// either a primitive type or a custom type with the
        /// <see cref="InterpolatorProviderAttribute"/>.
        /// </summary>
        /// <param name="type">
        /// The type, from which the <see cref="IInterpolator{T}"/> should be
        /// retrieved. Must be a value type and requires a valid
        /// <see cref="InterpolatorProviderAttribute"/> assigned to it.
        /// </param>
        /// <returns>
        /// The <see cref="IInterpolator{T}"/> as <see cref="object"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="type"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <see cref="IInterpolator{T}"/> couldn't be
        /// retrieved.
        /// </exception>
        private static object GetInterpolator(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (IsNumericPrimitiveType(type)) return animatorPrimitive;

            InterpolatorProviderAttribute attr;
            try
            {
                attr = (InterpolatorProviderAttribute)type.GetCustomAttributes(
                        typeof(InterpolatorProviderAttribute), false)[0];
            }
            catch (Exception exc)
            {
                throw new ArgumentException("The type doesn't have a valid " +
                    "AnimatorProviderAttribute attribute.", exc);
            }

            Type animatorType;
            try { animatorType = typeof(IInterpolator<>).MakeGenericType(type); }
            catch (Exception exc)
            {
                throw new ArgumentException("The type can't be used to " +
                    "create an animator.", exc);
            }

            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.GetProperty | BindingFlags.Public |
                BindingFlags.Static))
            {
                if (property.Name == attr.ProviderPropertyName &&
                    animatorType.IsAssignableFrom(property.PropertyType))
                    return property.GetValue(null);
            }

            throw new ArgumentException("The provider property name in the " +
                "attribute doesn't reference an existing public, static " +
                "property which implements the IAnimator<T> interface with" +
                "the specified type as T.");
        }

        /// <summary>
        /// Gets an <see cref="IInterpolator{T}"/> for a specific value type.
        /// </summary>
        /// <typeparam name="T">
        /// The value type which should be animated.
        /// </typeparam>
        /// <returns>
        /// An instance of an object implementing the 
        /// <see cref="IInterpolator{T}"/> interface.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <typeparamref name="T"/> is not
        /// supported.
        /// </exception>
        public static IInterpolator<T> GetInterpolator<T>()
            where T : unmanaged
        {
            try { return (IInterpolator<T>)GetInterpolator(typeof(T)); }
            catch (ArgumentException) { throw; }
        }
    }
}
