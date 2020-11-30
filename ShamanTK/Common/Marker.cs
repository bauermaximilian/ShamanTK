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
using System.Text;

namespace ShamanTK.Common
{
    /// <summary>
    /// Represents a marker, which can be used to specify the beginning/end of 
    /// an animation clip on a <see cref="Timeline"/>.
    /// </summary>
    public class Marker
    {
        /// <summary>
        /// Defines the maximum size of the <see cref="Identifier"/> in bytes.
        /// </summary>
        public const int IdentifierSizeMax = 512;

        /// <summary>
        /// Defines the minimum amount of bytes a byte representation of
        /// a <see cref="Marker"/> instance has.
        /// </summary>
        /// <remarks>
        /// Because of the <see cref="Identifier"/> property, the byte size
        /// of a marker varies - but it always has at least the size specified
        /// in this constant, as the "header" (which consists of the 
        /// <see cref="Position"/> as <see cref="Int64"/> and the byte size
        /// of the <see cref="Identifier"/> as <see cref="UInt32"/>) is
        /// mandatory.
        /// </remarks>
        public const int SizeMinimum = sizeof(long) + sizeof(uint);

        /// <summary>
        /// Gets the <see cref="Encoding"/> which is used to encode the
        /// <see cref="Identifier"/> in 
        /// <see cref="ToBytes"/>/<see cref="FromBytes(byte[])"/>.
        /// </summary>
        protected static Encoding IdentifierEncoding { get; } = Encoding.UTF8;

        /// <summary>
        /// Gets the position of the current <see cref="Marker"/> instance.
        /// </summary>
        public TimeSpan Position { get; }

        /// <summary>
        /// Gets the identifier of the current <see cref="Marker"/> instance.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Marker"/> class.
        /// </summary>
        /// <param name="position">
        /// The position on a <see cref="Timeline"/>.
        /// </param>
        /// <param name="identifier">
        /// The identifier of the <see cref="Marker"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the byte size of <paramref name="identifier"/> is
        /// greater than <see cref="IdentifierSizeMax"/>.
        /// </exception>
        public Marker(TimeSpan position, string identifier)
        {
            Position = position;
            Identifier = identifier ??
                throw new ArgumentNullException(nameof(identifier));
            if (IdentifierEncoding.GetByteCount(identifier) 
                > IdentifierSizeMax)
                throw new ArgumentException("The specified identifier " +
                    "exceeds the limit of " + IdentifierSizeMax
                    + " bytes.");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Marker"/> class.
        /// </summary>
        /// <param name="positionSeconds">
        /// The position on a <see cref="Timeline"/> in seconds.
        /// </param>
        /// <param name="identifier">
        /// The identifier of the <see cref="Marker"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the byte size of <paramref name="identifier"/> is
        /// greater than <see cref="IdentifierSizeMax"/>.
        /// </exception>
        public Marker(double positionSeconds, string identifier)
            : this(TimeSpan.FromSeconds(positionSeconds), identifier) { }

        /// <summary>
        /// Initializes a new <see cref="Marker"/> instance from a
        /// <see cref="byte"/> buffer created with <see cref="ToBytes"/>.
        /// </summary>
        /// <param name="buffer">The byte buffer.</param>
        /// <returns>
        /// A new instance of the <see cref="Marker"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="Marker"/> could be loaded from the 
        /// specified <paramref name="buffer"/>, or when the marker identifier
        /// specified in the <paramref name="buffer"/> is too large or invalid.
        /// </exception>
        public static Marker FromBytes(byte[] buffer)
        {
            return FromBytes(buffer, 0);
        }

        /// <summary>
        /// Initializes a new <see cref="Marker"/> instance from a
        /// <see cref="byte"/> buffer created with <see cref="ToBytes"/>.
        /// </summary>
        /// <param name="buffer">The byte buffer.</param>
        /// <param name="startIndex">
        /// The index in the <paramref name="buffer"/>, from where the bytes 
        /// for the <see cref="Marker"/> should be taken.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Marker"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when no <see cref="Marker"/> could be loaded from the 
        /// specified <paramref name="buffer"/> at the offset specified in 
        /// <paramref name="startIndex"/>, or when the marker identifier
        /// specified in the <paramref name="buffer"/> is too large or invalid.
        /// </exception>
        public static Marker FromBytes(byte[] buffer, int startIndex)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            if ((SizeMinimum + startIndex) > buffer.Length)
                throw new FormatException("The specified buffer is too " +
                    "small to contain a marker with a size of at least "
                    + SizeMinimum + " bytes" +
                    (startIndex > 0 ? (" at the byte array index "
                    + startIndex + ".") : "."));

            long positionTicks = BitConverter.ToInt64(buffer, startIndex);
            int identifierSize = 0;

            try
            {
                identifierSize = (int)BitConverter.ToUInt32(buffer,
                    startIndex + sizeof(long));
                if (identifierSize > IdentifierSizeMax)
                    throw new FormatException("The size of the identifier " +
                        "as specified in the buffer exceeds the limit of " +
                        IdentifierSizeMax + " bytes for an identifier.");
            }
            catch (Exception exc)
            {
                throw new FormatException("The size of the identifier, " +
                    "as specified in the buffer, is invalid.", exc);
            }

            string identifier = "";

            if (identifierSize > 0)
            {
                try
                {
                    identifier = IdentifierEncoding.GetString(buffer,
                        startIndex + SizeMinimum, identifierSize);
                }
                catch (Exception exc)
                {
                    throw new FormatException("The marker identifier " +
                        "specified in the buffer is invalid.", exc);
                }
            }

            return new Marker(new TimeSpan(positionTicks), identifier);
        }

        /// <summary>
        /// Converts the current <see cref="Marker"/> instance to a byte
        /// buffer, which can be used to create an instance of the 
        /// <see cref="Marker"/> class with the same identifier and position
        /// later using the <see cref="FromBytes(byte[])"/> method.
        /// </summary>
        /// <returns>A new <see cref="byte"/> buffer.</returns>
        public byte[] ToBytes()
        {
            byte[] positionBuffer = BitConverter.GetBytes(Position.Ticks);
            byte[] identifierBuffer = IdentifierEncoding.GetBytes(Identifier);
            byte[] buffer = new byte[positionBuffer.Length + 
                identifierBuffer.Length];
            positionBuffer.CopyTo(buffer, 0);
            identifierBuffer.CopyTo(buffer, positionBuffer.Length);
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
            return $"\"{Identifier}\" at {Position}";
        }
    }
}
