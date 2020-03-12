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

using Eterra.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Eterra.IO
{
    /// <summary>
    /// Provides extension methods for every <see cref="Stream"/> instance
    /// to add support for reading and writing commonly used object instances.
    /// </summary>
    public static class StreamExtension
    {
        /// <summary>
        /// Defines the maximum allowed size (in bytes) a byte buffer or a 
        /// string may have.
        /// </summary>
        /// <remarks>
        /// This limit was added to prevent invalid buffer/string size values
        /// to allocate too much memory.
        /// </remarks>
        public const uint MaximumBufferSize = 1024 * 1024 * 25;

        /// <summary>
        /// Defines the maximum allowed amount of elements in a list loaded or
        /// saved with a method in this extension class.
        /// </summary>
        /// <remarks>
        /// This limit was added to prevent invalid buffer/string size values
        /// to allocate too much memory.
        /// </remarks>
        public const uint MaximumListElementCount = 1024 * 1024;

        /// <summary>
        /// Defines the maximum allowed amount of elements in a dictionary 
        /// loaded or saved with a method in this extension class.
        /// </summary>
        /// <remarks>
        /// This limit was added to prevent invalid buffer/string size values
        /// to allocate too much memory.
        /// </remarks>
        public const uint MaximumDictionaryElementCount = 
            MaximumListElementCount / 2;

        private static readonly EndOfStreamException endOfStreamException =
            new EndOfStreamException("The end of the stream was reached " +
            "before the value could be read completely.");

        /// <summary>
        /// Reads a <see cref="byte"/> array from the current stream and 
        /// advances the position within the stream by 
        /// <paramref name="length"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="length">
        /// The length of the byte array to be read.
        /// </param>
        /// <returns>A new <see cref="byte"/> array instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="length"/> is greater than
        /// <see cref="MaximumBufferSize"/>.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// block of bytes requested could be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static byte[] ReadBuffer(this Stream stream, uint length)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (length > MaximumBufferSize)
                throw new ArgumentOutOfRangeException(nameof(length));

            byte[] buffer = new byte[length];
            if (length == 0 ||
                stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                return buffer;
            else throw endOfStreamException;
        }

        /// <summary>
        /// Reads a <see cref="byte"/> array from the current stream and 
        /// advances the position within the stream by the length of the 
        /// buffer, which is specified by a single <see cref="uint"/> before
        /// the beginning of the <see cref="byte"/> buffer in the stream.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <returns>A new <see cref="byte"/> array instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the buffer size, as it was read from the stream,
        /// exceeds the <see cref="MaximumBufferSize"/>.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// block of bytes requested could be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static byte[] ReadBuffer(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            uint length = stream.ReadUnsignedInteger();

            if (length > MaximumBufferSize)
                throw new FormatException("The buffer size, as read from " +
                    "the file, exceeds the maximum length of " +
                    MaximumBufferSize + " bytes.");

            byte[] buffer = new byte[length];
            if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                return buffer;
            else throw endOfStreamException;
        }

        /// <summary>
        /// Reads a simple ASCII-encoded string of a fixed length from the 
        /// current stream and advances the position within the stream by 
        /// the amount of bytes defined by <paramref name="length"/>.
        /// This method can't read strings written by
        /// <see cref="WriteString(Stream, string, Encoding)"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="length">
        /// The length of the string.
        /// </param>
        /// <returns>
        /// A new <see cref="string"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="length"/> is less than 1 or
        /// greater than <see cref="MaximumBufferSize"/>.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// block of bytes required for initializing the object instance could
        /// be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static string ReadStringFixed(this Stream stream, uint length)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (length < 1 || length > MaximumBufferSize)
                throw new ArgumentOutOfRangeException(nameof(length));

            byte[] buffer = new byte[length];
            if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                return Encoding.ASCII.GetString(buffer);
            else throw endOfStreamException;
        }

        /// <summary>
        /// Calculates the amount of bytes required by a string written with 
        /// <see cref="WriteStringFixed(Stream, string)"/>.
        /// </summary>
        /// <param name="s">
        /// The string to be used for calculation.
        /// </param>
        /// <returns>
        /// A new <see cref="uint"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="s"/> is null.
        /// </exception>
        public static uint GetStringFixedSize(string s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));

            return (uint)Encoding.ASCII.GetByteCount(s);
        }

        /// <summary>
        /// Calculates the amount of bytes required by a string written with 
        /// <see cref="WriteStringFixed(Stream, string)"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="s">
        /// The string to be used for calculation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="s"/> is null.
        /// </exception>
        /// <returns>
        /// A new <see cref="uint"/>.
        /// </returns>
        public static uint GetStringFixedSize(this Stream stream,
            string s)
        {
            return GetStringFixedSize(s);
        }

        /// <summary>
        /// Reads a string with a dynamic length and advances the position 
        /// within the stream by the size of the string header (two integers) 
        /// and the amount of bytes the string has (as defined in the header).
        /// This method can't read strings written by
        /// <see cref="WriteStringFixed(Stream, string)"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <returns>A new <see cref="string"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the string header is invalid, the string had
        /// an invalid format or its size exceeded the 
        /// <see cref="MaximumBufferSize"/>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static string ReadString(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            int codepage = ReadSignedInteger(stream);

            Encoding encoding;
            try { encoding = Encoding.GetEncoding(codepage); }
            catch (Exception exc)
            {
                throw new FormatException("The codepage of the string is " +
                    "not supported or invalid.", exc);
            }

            byte[] buffer;
            try { buffer = stream.ReadBuffer(); }
            catch (Exception exc)
            {
                throw new FormatException("The size of the read string, " +
                    "as read from the stream, was invalid.", exc);
            }

            try { return encoding.GetString(buffer); }
            catch (Exception exc)
            {
                throw new FormatException("The byte representation of the " +
                    "string was invalid.", exc);
            }
        }

        /// <summary>
        /// Reads a <see cref="ResourcePath"/> and advances the position 
        /// within the stream by the size of a string header (two integers) 
        /// and the amount of bytes the resource path string has with a
        /// <see cref="UnicodeEncoding"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <returns>
        /// A new <see cref="ResourcePath"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the string violated the format specifications of a 
        /// <see cref="ResourcePath"/> or the format/length of the base string
        /// within in the stream was invalid.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static ResourcePath ReadResourcePath(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            string resourcePathString;
            try { resourcePathString = ReadString(stream); }
            catch (Exception exc)
            {
                throw new FormatException("The format or length of the base " +
                    "stream for the resource path was invalid.", exc);
            }

            try { return new ResourcePath(resourcePathString); }
            catch (ArgumentException exc)
            {
                throw new FormatException("The resource path string has " +
                    "an invalid format.", exc);
            }
        }

        /// <summary>
        /// Reads a single <see cref="bool"/> from the current stream and and 
        /// advances the position within the stream by the size of an 
        /// <see cref="bool"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <returns>A new <see cref="bool"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// block of bytes required for initializing the object instance could
        /// be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static bool ReadBool(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = new byte[sizeof(byte)];
            if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                return BitConverter.ToBoolean(buffer, 0);
            else throw endOfStreamException;
        }

        /// <summary>
        /// Reads a single <see cref="byte"/> from the current stream and 
        /// advances the position within the stream by one byte.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <returns>A new <see cref="byte"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// block of bytes required for initializing the object instance could
        /// be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static byte ReadSingleByte(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = new byte[1];
            if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                return buffer[0];
            else throw endOfStreamException;
        }

        /// <summary>
        /// Reads a single <see cref="char"/> from the current stream and 
        /// advances the position within the stream by the size of an 
        /// <see cref="char"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <returns>A new <see cref="Color"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// block of bytes required for initializing the object instance could
        /// be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static char ReadChar(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = new byte[sizeof(char)];
            if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                return BitConverter.ToChar(buffer, 0);
            else throw endOfStreamException;
        }

        /// <summary>
        /// Reads a single <see cref="uint"/> from the current stream and and 
        /// advances the position within the stream by the size of an 
        /// <see cref="uint"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <returns>A new <see cref="uint"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// block of bytes required for initializing the object instance could
        /// be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static uint ReadUnsignedInteger(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = new byte[sizeof(int)];
            if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                return BitConverter.ToUInt32(buffer, 0);
            else throw endOfStreamException;
        }

        /// <summary>
        /// Reads a single <see cref="int"/> from the current stream and and 
        /// advances the position within the stream by the size of an 
        /// <see cref="int"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <returns>A new <see cref="int"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// block of bytes required for initializing the object instance could
        /// be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static int ReadSignedInteger(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = new byte[sizeof(int)];
            if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                return BitConverter.ToInt32(buffer, 0);
            else throw endOfStreamException;
        }

        /// <summary>
        /// Reads a single <see cref="short"/> from the current stream and  
        /// advances the position within the stream by the size of an 
        /// <see cref="short"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <returns>A new <see cref="short"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// block of bytes required for initializing the object instance could
        /// be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static short ReadShort(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = new byte[sizeof(short)];
            if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                return BitConverter.ToInt16(buffer, 0);
            else throw endOfStreamException;
        }

        /// <summary>
        /// Reads a single <see cref="float"/> from the current stream and 
        /// advances the position within the stream by the size of an 
        /// <see cref="float"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <returns>A new <see cref="float"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// block of bytes required for initializing the object instance could
        /// be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static float ReadFloat(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = new byte[sizeof(float)];
            if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                return BitConverter.ToSingle(buffer, 0);
            else throw endOfStreamException;
        }

        /// <summary>
        /// Reads a single <see cref="TimeSpan"/> from the current stream and 
        /// advances the position within the stream by the size of an 
        /// <see cref="long"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <returns>A new <see cref="TimeSpan"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// block of bytes required for initializing the object instance could
        /// be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static TimeSpan ReadTimeSpan(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = new byte[sizeof(long)];
            if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                return TimeSpan.FromTicks(BitConverter.ToInt64(buffer, 0));
            else throw endOfStreamException;
        }

        /// <summary>
        /// Reads an instance of a type with a fixed size from the current 
        /// stream and advances the current position within this stream by the 
        /// number of bytes of a single <see cref="uint"/> and the byte size
        /// of an instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value to be read from the stream.
        /// Must have a fixed size (see the <c>unmanaged</c> keyword for more 
        /// information).
        /// </typeparam>
        /// <param name="stream">The stream to operate on.</param>
        /// <returns>A new instance of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the byte buffer definition in the stream is invalid,
        /// the read buffer length doesn't match the byte size of an instance
        /// of type <typeparamref name="T"/>, or the buffer couldn't be
        /// converted to an instance of the type <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static T Read<T>(this Stream stream)
            where T : unmanaged
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            int expectedBufferSize = Marshal.SizeOf(typeof(T));

            byte[] buffer;
            try { buffer = stream.ReadBuffer(); }
            catch (FormatException exc)
            {
                throw new FormatException("The byte representation of the " +
                    "requested object couldn't be read from the stream.", exc);
            }

            if (expectedBufferSize != buffer.Length)
                throw new FormatException("The size of the read buffer " +
                    "doesn't match the size of an object instance of the " +
                    "requested type '" + typeof(T).GetType().Name + "'.");

            T value;
            IntPtr pointer = Marshal.AllocHGlobal(buffer.Length);
            try
            {
                Marshal.Copy(buffer, 0, pointer, buffer.Length);
                value = (T)Marshal.PtrToStructure(pointer, typeof(T));
            }
            catch (FormatException exc)
            {
                throw new FormatException("The read byte buffer couldn't be " +
                    "converted to an instance of the requested type '" + 
                    typeof(T).GetType().Name + "'.", exc);
            }
            finally { Marshal.FreeHGlobal(pointer); }

            return value;
        }

        /// <summary>
        /// Reads a <see cref="Dictionary{TKey, TValue}"/> instance from the 
        /// current stream and advances the position within the stream by the 
        /// amount of read bytes.
        /// </summary>
        /// <typeparam name="TKey">
        /// The type of the dictionary key.
        /// </typeparam>
        /// <typeparam name="TValue">
        /// The type of the dictionary value.
        /// </typeparam>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="keyDeserializer">
        /// A delegate that deserializes a byte buffer into an instance of
        /// type <typeparamref name="TKey"/>.
        /// </param>
        /// <param name="valueDeserializer">
        /// A delegate that deserializes a byte buffer into an instance of
        /// type <typeparamref name="TValue"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="Dictionary{TKey, TValue}"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/>,
        /// <paramref name="keyDeserializer"/> or
        /// <paramref name="valueDeserializer"/> are null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the amount of elements, as defined in the 
        /// <paramref name="stream"/>, exceeds the limit defined by
        /// <see cref="MaximumDictionaryElementCount"/> or when one of the
        /// byte buffers in the stream, which make up the keys and values,
        /// have a length which exceeds the limit defined by
        /// <see cref="MaximumBufferSize"/>.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// block of bytes required for initializing the object instance could
        /// be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanRead"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            this Stream stream, Func<byte[], TKey> keyDeserializer,
            Func<byte[], TValue> valueDeserializer)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (keyDeserializer == null)
                throw new ArgumentNullException(nameof(keyDeserializer));
            if (valueDeserializer == null)
                throw new ArgumentNullException(nameof(valueDeserializer));

            uint elementCount = stream.ReadUnsignedInteger();
            if (elementCount > MaximumDictionaryElementCount)
                throw new FormatException("The amount of dictionary " +
                    "elements, as specified in the file, is too large.");

            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>(
                (int)elementCount);

            for (int i = 0; i < elementCount; i++)
            {
                byte[] keyBuffer = ReadBuffer(stream);
                byte[] valueBuffer = ReadBuffer(stream);

                TKey key = keyDeserializer(keyBuffer);
                TValue value = valueDeserializer(valueBuffer);

                dictionary.Add(key, value);
            }

            return dictionary;
        }

        /// <summary>
        /// Writes a <see cref="byte"/> array to the current stream and
        /// and advances the current position within this stream by the 
        /// length of <paramref name="buffer"/> (and, depending on 
        /// <paramref name="prependBufferSize"/>, the size of one 
        /// <see cref="uint"/>).
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="buffer">
        /// The byte array to be written.
        /// </param>
        /// <param name="prependBufferSize">
        /// <c>true</c> to prepend a single <see cref="uint"/> before the
        /// buffer data (which increases the amount of written bytes by the
        /// size of a <see cref="uint"/> and allows the use of 
        /// <see cref="ReadBuffer(Stream)"/> to read the buffer back in later),
        /// <c>false</c> to just write the buffer data (and read it back in 
        /// with <see cref="ReadBuffer(Stream, uint)"/> later).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> or 
        /// <paramref name="buffer"/> are null.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteBuffer(this Stream stream, byte[] buffer,
            bool prependBufferSize)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (prependBufferSize)
                stream.WriteUnsignedInteger((uint)buffer.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes an ASCII-encoded <see cref="string"/> to the current 
        /// stream and and advances the current position within this stream 
        /// by the number of bytes written, which equals to the amount of 
        /// characters in <paramref name="value"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="value">
        /// The instance to be written to the current stream.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteStringFixed(this Stream stream, string value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = Encoding.ASCII.GetBytes(value);
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes a string with a dynamic length and advances the position 
        /// within the stream by the size of the string header (two integers) 
        /// and the amount of bytes the string has when encoded with
        /// <see cref="Encoding.Unicode"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="s">The string to be written.</param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> or <paramref name="s"/> 
        /// are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the byte representation of <paramref name="s"/>
        /// in <see cref="Encoding.Unicode"/> exceeds the maximum
        /// size defined in <see cref="MaximumStringSize"/>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteString(this Stream stream, string s)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            WriteString(stream, s, Encoding.Unicode);
        }

        /// <summary>
        /// Writes a string with a dynamic length and advances the position 
        /// within the stream by the size of the string header (two integers) 
        /// and the amount of bytes the string has with the specified
        /// <paramref name="encoding"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="s">The string to be written.</param>
        /// <param name="encoding">
        /// The encoding, which should be used to convert the specified
        /// string to bytes.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/>,
        /// <paramref name="s"/> or <paramref name="encoding"/>
        /// are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the byte representation of <paramref name="s"/>
        /// in the specified <paramref name="encoding"/> exceeds the maximum
        /// buffer size defined in <see cref="MaximumBufferSize"/>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteString(this Stream stream, string s,
            Encoding encoding)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (s == null) throw new ArgumentNullException(nameof(s));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            if (encoding.GetByteCount(s) > MaximumBufferSize)
                throw new ArgumentException("The size in bytes of the " +
                    "specified string in the specified encoding exceeds " +
                    "the maximum allowed size of " + MaximumBufferSize + ".");

            byte[] sBytes = encoding.GetBytes(s);

            WriteSignedInteger(stream, encoding.CodePage);
            stream.WriteBuffer(sBytes, true);
        }

        /// <summary>
        /// Writes a <see cref="ResourcePath"/> and advances the position 
        /// within the stream by the size of a string header (two integers) 
        /// and the amount of bytes the resource path string has with a
        /// <see cref="UnicodeEncoding"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="resourcePath">The resource path to be written.</param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the byte representation of 
        /// <see cref="ResourcePath.ToString"/> in 
        /// <see cref="UnicodeEncoding"/> exceeds the maximum
        /// size defined in <see cref="MaximumBufferSize"/>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteResourcePath(this Stream stream,
            ResourcePath resourcePath)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                WriteString(stream, resourcePath.ToString(), Encoding.Unicode);
            }
            catch (Exception exc)
            {
                throw new ArgumentException("The specified resource path " +
                    "couldn't be written to the stream.", exc);
            }
        }

        /// <summary>
        /// Writes a single <see cref="byte"/> to the current stream and
        /// and advances the current position within this stream by one byte.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="value">
        /// The instance to be written to the current stream.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteByte(this Stream stream, byte value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            stream.Write(new byte[] { value }, 0, 1);
        }

        /// <summary>
        /// Writes a single <see cref="char"/> to the current stream and
        /// and advances the current position within this stream by the 
        /// number of bytes of a <see cref="char"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="value">
        /// The instance to be written to the current stream.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteChar(this Stream stream, char value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = BitConverter.GetBytes(value);
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes a single <see cref="bool"/> to the current stream and
        /// and advances the current position within this stream by the 
        /// number of bytes of a <see cref="bool"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="value">
        /// The instance to be written to the current stream.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteBool(this Stream stream, bool value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = BitConverter.GetBytes(value);
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes a single <see cref="int"/> to the current stream and
        /// and advances the current position within this stream by the 
        /// number of bytes of a <see cref="int"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="value">
        /// The instance to be written to the current stream.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteSignedInteger(this Stream stream, int value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = BitConverter.GetBytes(value);
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes a single <see cref="uint"/> to the current stream and
        /// and advances the current position within this stream by the 
        /// number of bytes of a <see cref="uint"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="value">
        /// The instance to be written to the current stream.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteUnsignedInteger(this Stream stream, uint value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = BitConverter.GetBytes(value);
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes a single <see cref="short"/> to the current stream and
        /// and advances the current position within this stream by the 
        /// number of bytes of a <see cref="short"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="value">
        /// The instance to be written to the current stream.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteShort(this Stream stream, short value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = BitConverter.GetBytes(value);
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes a single <see cref="float"/> to the current stream and
        /// and advances the current position within this stream by the 
        /// number of bytes of a <see cref="float"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="value">
        /// The instance to be written to the current stream.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteFloat(this Stream stream, float value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = BitConverter.GetBytes(value);
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes a single <see cref="TimeSpan"/> to the current stream and
        /// and advances the current position within this stream by the 
        /// number of bytes of a <see cref="long"/>.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="value">
        /// The instance to be written to the current stream.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteTimeSpan(this Stream stream, TimeSpan value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = BitConverter.GetBytes(value.Ticks);
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes an instance of a type with a fixed size to the current 
        /// stream and advances the current position within this stream by the 
        /// number of bytes of a single <see cref="uint"/> and the converted 
        /// <paramref name="instance"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value which should be written to the stream.
        /// Must have a fixed size (see the <c>unmanaged</c> keyword for more 
        /// information).
        /// </typeparam>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="value">
        /// The instance to be written to the current stream.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void Write<T>(this Stream stream, T instance)
            where T : unmanaged
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];

            IntPtr pointer = Marshal.AllocHGlobal(buffer.Length);
            Marshal.StructureToPtr(instance, pointer, true);
            Marshal.Copy(pointer, buffer, 0, buffer.Length);
            Marshal.FreeHGlobal(pointer);

            stream.WriteBuffer(buffer, true);
        }

        /// <summary>
        /// Writes a <see cref="IDictionary{TKey, TValue}"/> instance to the 
        /// current stream and and advances the current position within this 
        /// stream by the amount of bytes written.
        /// </summary>
        /// <param name="stream">The stream to operate on.</param>
        /// <param name="value">
        /// The instance to be written to the current stream.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/>, 
        /// <paramref name="dictionary"/>,
        /// <paramref name="keySerializer"/> or
        /// <paramref name="valueSerializer"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the amount of elements in
        /// <paramref name="dictionary"/> exceeds the limit defined by
        /// <see cref="MaximumDictionaryElementCount"/> or when the
        /// byte buffer generated by <paramref name="keySerializer"/> or 
        /// <paramref name="valueSerializer"/> exceeds the limit defined by
        /// <see cref="MaximumBufferSize"/>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="Stream.CanWrite"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        public static void WriteDictionary<TKey, TValue>(this Stream stream, 
            IDictionary<TKey, TValue> dictionary, 
            Func<TKey, byte[]> keySerializer, 
            Func<TValue, byte[]> valueSerializer)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));
            if (keySerializer == null)
                throw new ArgumentNullException(nameof(keySerializer));
            if (valueSerializer == null)
                throw new ArgumentNullException(nameof(valueSerializer));

            if (dictionary.Count > MaximumDictionaryElementCount)
                throw new ArgumentException("The specified dictionary " +
                    "contains too much elements.");

            stream.WriteUnsignedInteger((uint)dictionary.Count);

            foreach (KeyValuePair<TKey, TValue> element in dictionary)
            {
                byte[] keyBuffer = keySerializer(element.Key);
                byte[] valueBuffer = valueSerializer(element.Value);

                WriteBuffer(stream, keyBuffer, true);
                WriteBuffer(stream, valueBuffer, true);
            }
        }
    }
}
