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
using System.Runtime.InteropServices;

namespace ShamanTK.Common
{
    /// <summary>
    /// Represents a value on a <see cref="TimelineLayer"/>.
    /// </summary>
    public abstract class Keyframe
    {
        /// <summary>
        /// Gets the size of a base <see cref="Keyframe"/> instance without
        /// a value.
        /// </summary>
        protected static int SizeBase { get; } = sizeof(long);

        /// <summary>
        /// Gets the position of the current <see cref="Keyframe"/>.
        /// </summary>
        public TimeSpan Position { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Keyframe"/> class.
        /// </summary>
        /// <param name="position">
        /// The position on a <see cref="Timeline"/>.
        /// </param>
        protected Keyframe(TimeSpan position)
        {
            Position = position;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Keyframe"/> class.
        /// </summary>
        /// <param name="positionSeconds">
        /// The position on a <see cref="Timeline"/> in seconds.
        /// </param>
        protected Keyframe(double positionSeconds)
            : this(TimeSpan.FromSeconds(positionSeconds)) { }

        /// <summary>
        /// Calculates the ratio of a timeline position between two keyframes.
        /// </summary>
        /// <param name="other">
        /// The other <see cref="Keyframe"/> instance.
        /// </param>
        /// <param name="position">
        /// The timeline position to be set into relation to the two
        /// <see cref="Keyframe"/> instances.
        /// </param>
        /// <returns>
        /// A <see cref="float"/> value between 0.0 and 1.0, specifying the
        /// relative position of <paramref name="position"/> in relation to
        /// the current <see cref="Keyframe"/> and <paramref name="other"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="other"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="position"/> is not inside the range
        /// defined by the current <see cref="Keyframe"/> and 
        /// <paramref name="other"/>.
        /// </exception>
        public float CalculateRatioTo(Keyframe other, TimeSpan position)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            TimeSpan start, end;
            if (Position < other.Position)
            {
                start = Position;
                end = other.Position;
            }
            else
            {
                start = other.Position;
                end = Position;
            }

            if (position > end || position < start)
                throw new ArgumentOutOfRangeException(nameof(position));

            return (float)((position - Position).TotalMilliseconds /
                (other.Position - Position).TotalMilliseconds);
        }

        /// <summary>
        /// Gets the size of a single instance of an object of a certain type
        /// in unmanaged memory. Only works for unmanaged objects.
        /// </summary>
        /// <param name="type">The type of the instance.</param>
        /// <returns>The size of an object of that type in bytes.</returns>
        protected static int GetUnmanagedTypeSize(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return Marshal.SizeOf(type);
        }

        /// <summary>
        /// Initializes a new <see cref="Keyframe{T}"/> instance from a
        /// <see cref="byte"/> buffer created with <see cref="ToBytes"/>.
        /// </summary>
        /// <param name="buffer">The byte buffer.</param>
        /// <returns>
        /// A new instance of the <see cref="Keyframe{T}"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="Keyframe{T}"/> of type 
        /// <typeparamref name="T"/> could be loaded from the specified 
        /// <paramref name="buffer"/>.
        /// </exception>
        public static Keyframe<T> FromBytes<T>(byte[] buffer)
            where T : unmanaged
        {
            return FromBytes<T>(buffer, 0);
        }

        /// <summary>
        /// Initializes a new <see cref="Keyframe{T}"/> instance from a
        /// <see cref="byte"/> buffer created with <see cref="ToBytes"/>.
        /// </summary>
        /// <param name="buffer">The byte buffer.</param>
        /// <param name="startIndex">
        /// The index in the <paramref name="buffer"/>, from where the bytes 
        /// for the <see cref="Keyframe{T}"/> should be taken.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Keyframe{T}"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="startIndex"/> is less than 0.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when no <see cref="Keyframe{T}"/> of type 
        /// <typeparamref name="T"/> could be loaded from the specified 
        /// <paramref name="buffer"/> at the offset specified in 
        /// <paramref name="startIndex"/>.
        /// </exception>
        public static Keyframe<T> FromBytes<T>(byte[] buffer, int startIndex)
            where T : unmanaged
        {
            /*if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            if ((Keyframe<T>.Size + startIndex) > buffer.Length)
                throw new FormatException("The specified buffer is too " +
                    "small to contain a keyframe with a size of "
                    + Keyframe<T>.Size + " bytes" +
                    (startIndex > 0 ? (" at the byte array index "
                    + startIndex + ".") : "."));

            long positionTicks = BitConverter.ToInt64(buffer, startIndex);
            T value;
            int valueSize = GetUnmanagedTypeSize(typeof(T));

            IntPtr pointer = Marshal.AllocHGlobal(valueSize);
            try
            {
                //The position is stored at the buffer start in ticks (Int64).
                Marshal.Copy(buffer, startIndex + sizeof(long), pointer,
                    valueSize);
                value = (T)Marshal.PtrToStructure(pointer, typeof(T));
            }
            catch (Exception exc)
            {
                throw new FormatException("The specified buffer was " +
                    "invalid.", exc);
            }
            finally { Marshal.FreeHGlobal(pointer); }

            return new Keyframe<T>(new TimeSpan(positionTicks), value);*/

            return (Keyframe<T>)FromBytes(typeof(T), buffer, startIndex);
        }

        /// <summary>
        /// Initializes a new <see cref="Keyframe"/> instance from a
        /// <see cref="byte"/> buffer created with <see cref="ToBytes"/>.
        /// </summary>
        /// <param name="valueType">
        /// The type of the value stored by the loaded keyframe.
        /// Must match the <c>unmanaged</c> constraint.
        /// </param>
        /// <param name="buffer">The byte buffer.</param>
        /// <returns>
        /// A new instance of the <see cref="Keyframe"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="valueType"/> is no
        /// type matching the <c>unmanaged</c> constraint.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when no <see cref="Keyframe"/> of type 
        /// <typeparamref name="T"/> could be loaded from the specified 
        /// <paramref name="buffer"/>.
        /// </exception>
        public static Keyframe FromBytes(Type valueType, byte[] buffer)
        {
            return FromBytes(valueType, buffer, 0);
        }

        /// <summary>
        /// Initializes a new <see cref="Keyframe"/> instance from a
        /// <see cref="byte"/> buffer created with <see cref="ToBytes"/>.
        /// </summary>
        /// <param name="valueType">
        /// The type of the value stored by the loaded keyframe.
        /// Must match the <c>unmanaged</c> constraint.
        /// </param>
        /// <param name="buffer">The byte buffer.</param>
        /// <param name="startIndex">
        /// The index in the <paramref name="buffer"/>, from where the bytes 
        /// for the <see cref="Keyframe"/> should be taken.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Keyframe"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="startIndex"/> is less than 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="valueType"/> is no
        /// type matching the <c>unmanaged</c> constraint.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when no <see cref="Keyframe"/> of type 
        /// <typeparamref name="T"/> could be loaded from the specified 
        /// <paramref name="buffer"/> at the offset specified in 
        /// <paramref name="startIndex"/>.
        /// </exception>
        public static Keyframe FromBytes(Type valueType, byte[] buffer,
            int startIndex)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            Type keyframeType;
            int keyframeValueSize;

            try
            {
                keyframeType = typeof(Keyframe<>).MakeGenericType(valueType);
                keyframeValueSize = GetUnmanagedTypeSize(valueType);
            }
            catch (Exception exc)
            {
                throw new ArgumentException("The specified value type is " +
                    "invalid and can't be used as keyframe value type.", exc);
            }

            int keyframeInstanceSize = keyframeValueSize + SizeBase;

            if ((keyframeInstanceSize + startIndex) > buffer.Length)
                throw new FormatException("The specified buffer is too " +
                    "small to contain a keyframe with a size of "
                    + keyframeInstanceSize + " bytes" +
                    (startIndex > 0 ? (" at the byte array index "
                    + startIndex + ".") : "."));

            long positionTicks = BitConverter.ToInt64(buffer, startIndex);
            object value;

            IntPtr pointer = Marshal.AllocHGlobal(keyframeValueSize);
            try
            {
                //The position is stored at the buffer start in ticks (Int64).
                Marshal.Copy(buffer, startIndex + SizeBase, pointer,
                    keyframeValueSize);
                value = Marshal.PtrToStructure(pointer, valueType);
            }
            catch (Exception exc)
            {
                throw new FormatException("The specified buffer was " +
                    "invalid.", exc);
            }
            finally { Marshal.FreeHGlobal(pointer); }

            return (Keyframe)Activator.CreateInstance(keyframeType,
                new TimeSpan(positionTicks), value);
        }

        /// <summary>
        /// Converts the current <see cref="Keyframe"/> instance to a byte
        /// buffer, which can be used to create an instance of the 
        /// <see cref="Keyframe"/> class with the same value type and position
        /// later using the <see cref="FromBytes(byte[])"/> method.
        /// </summary>
        /// <returns>A new <see cref="byte"/> buffer.</returns>
        public abstract byte[] ToBytes();

        /// <summary>
        /// Initializes a new instance of the <see cref="Keyframe{flToat}"/>
        /// class with a <see cref="float"/> as value.
        /// </summary>
        /// <param name="positionSeconds">
        /// The position on a <see cref="Timeline"/> in seconds.
        /// </param>
        /// <param name="f">
        /// The value of the float.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Keyframe{T}"/> class.
        /// </returns>
        public static Keyframe<float> Create(double positionSeconds, float f)
            => new Keyframe<float>(positionSeconds, f);

        /// <summary>
        /// Initializes a new instance of the <see cref="Keyframe{T}"/>
        /// class with a <see cref="Vector2"/> as value.
        /// </summary>
        /// <param name="positionSeconds">
        /// The position on a <see cref="Timeline"/> in seconds.
        /// </param>
        /// <param name="x">
        /// The value for the <see cref="System.Numerics.Vector2.X"/> property.
        /// </param>
        /// <param name="y">
        /// The value for the <see cref="System.Numerics.Vector2.Y"/> property.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Keyframe{Vector2}"/> class.
        /// </returns>
        public static Keyframe<Vector2> Create(double positionSeconds,
            float x, float y)
            => new Keyframe<Vector2>(positionSeconds, new Vector2(x, y));

        /// <summary>
        /// Initializes a new instance of the <see cref="Keyframe{T}"/>
        /// class with a <see cref="Vector3"/> as value.
        /// </summary>
        /// <param name="positionSeconds">
        /// The position on a <see cref="Timeline"/> in seconds.
        /// </param>
        /// <param name="x">
        /// The value for the <see cref="System.Numerics.Vector3.X"/> property.
        /// </param>
        /// <param name="y">
        /// The value for the <see cref="System.Numerics.Vector3.Y"/> property.
        /// </param>
        /// <param name="z">
        /// The value for the <see cref="System.Numerics.Vector3.Z"/> property.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Keyframe{T}"/> class.
        /// </returns>
        public static Keyframe<Vector3> Create(double positionSeconds,
            float x, float y, float z)
            => new Keyframe<Vector3>(positionSeconds, new Vector3(x, y, z));
    }

    /// <summary>
    /// Represents a value on a <see cref="TimelineLayer{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <remarks>
    /// It is highly recommended to only use value types or immutable structs 
    /// without any contained reference types as <typeparamref name="T"/>.
    /// Modifications to a <see cref="Keyframe{T}"/> while an animation is
    /// playing could have unexpected consequences.
    /// </remarks>
    public class Keyframe<T> : Keyframe where T : unmanaged
    {
        /// <summary>
        /// Gets an empty keyframe instance with a zero position for a keyframe 
        /// of the current type <typeparamref name="T"/>.
        /// </summary>
        public static Keyframe<T> Empty { get; } 
            = new Keyframe<T>(0, default);

        /// <summary>
        /// Gets the size of a <typeparamref name="T"/> instance in bytes.
        /// </summary>
        protected static int SizeT { get; } = GetUnmanagedTypeSize(typeof(T));

        /// <summary>
        /// Gets the size of a <see cref="Keyframe{T}"/> instance in bytes.
        /// </summary>
        public static int Size { get; } = SizeT + SizeBase;

        /// <summary>
        /// Gets a readonly reference to the value of the 
        /// <see cref="Keyframe{T}"/>.
        /// </summary>
        public ref readonly T Value => ref value;
        private readonly T value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Keyframe{T}"/> class.
        /// </summary>
        /// <param name="position">
        /// The position on a <see cref="Timeline"/>.
        /// </param>
        /// <param name="value">The value.</param>
        public Keyframe(TimeSpan position, T value) : base(position)
        {
            this.value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Keyframe{T}"/> class.
        /// </summary>
        /// <param name="positionSeconds">
        /// The position on a <see cref="Timeline"/> in seconds.
        /// </param>
        /// <param name="value">The value.</param>
        public Keyframe(double positionSeconds, T value)
            : this(TimeSpan.FromSeconds(positionSeconds), value) { }

        /// <summary>
        /// Calculates the ratio of a timeline position between two keyframes.
        /// </summary>
        /// <param name="other">
        /// The other <see cref="Keyframe{T}"/> instance.
        /// </param>
        /// <param name="position">
        /// The timeline position to be set into relation to the two
        /// <see cref="Keyframe{T}"/> instances.
        /// </param>
        /// <returns>
        /// A <see cref="float"/> value between 0.0 and 1.0, specifying the
        /// relative position of <paramref name="position"/> in relation to
        /// the current <see cref="Keyframe{T}"/> and <paramref name="other"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="other"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="position"/> is not inside the range
        /// defined by the current <see cref="Keyframe{T}"/> and 
        /// <paramref name="other"/>.
        /// </exception>
        public float CalculateRatioTo(Keyframe<T> other, TimeSpan position)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            TimeSpan start, end;
            if (Position < other.Position)
            {
                start = Position;
                end = other.Position;
            }
            else
            {
                start = other.Position;
                end = Position;
            }

            if (position > end || position < start)
                throw new ArgumentOutOfRangeException(nameof(position));

            return (float)((position - Position).TotalMilliseconds /
                (other.Position - Position).TotalMilliseconds);
        }

        /// <summary>
        /// Converts the current <see cref="Keyframe{T}"/> instance to a byte
        /// buffer, which can be used to create an instance of the 
        /// <see cref="Keyframe{T}"/> class with the same 
        /// <see cref="Position"/> and <see cref="Value"/> later using the
        /// <see cref="FromBytes(byte[])"/> method.
        /// </summary>
        /// <returns>A new <see cref="byte"/> buffer.</returns>
        public override byte[] ToBytes()
        {
            //The final buffer, which will contain the position at the 
            //beginning before the value byte representation is added
            byte[] buffer = new byte[Size];
            BitConverter.GetBytes(Position.Ticks).CopyTo(buffer, 0);

            IntPtr pointer = Marshal.AllocHGlobal(SizeT);
            try
            {
                Marshal.StructureToPtr(value, pointer, true);
                //The position is stored at the buffer start in ticks (Int64).
                Marshal.Copy(pointer, buffer, sizeof(long), SizeT);
            }
            catch (Exception exc)
            {
                throw new ApplicationException("The current keyframe value " +
                    "couldn't be converted to a buffer.", exc);
            }
            finally { Marshal.FreeHGlobal(pointer); }

            return buffer;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return Value.ToString() + " (" + Position.ToString() + ")";
        }
    }
}
