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

//#define UNSAFE

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ShamanTK.Common
{
    /// <summary>
    /// Provides the base class from which those classes are derived which
    /// provide a typed, managed access to unmanaged data.
    /// </summary>
    public abstract class MemoryPointer : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of the value, which can be accessed
        /// through this <see cref="MemoryPointer"/>.
        /// </summary>
        public Type ElementType { get; }

        /// <summary>
        /// Gets the <see cref="IntPtr"/> to the start of the data in 
        /// unmanaged memory or <see cref="IntPtr.Zero"/>, 
        /// if <see cref="IsDisposed"/> is <c>true</c>.
        /// </summary>
        public abstract IntPtr StartAddress { get; }

        /// <summary>
        /// Gets the amount of elements of <see cref="DataT"/> in 
        /// unmanaged memory. More than 1 indicates arrays, 1 a single 
        /// element and 0, if <see cref="IsDisposed"/> is true.
        /// </summary>
        public int Count
        {
            get
            {
                if (IsDisposed) return 0;
                else return dataLength;
            }
        }
        private readonly int dataLength = 0;

        /// <summary>
        /// Gets the total size of all elements in the unmanaged memory 
        /// (in bytes).
        /// </summary>
        public int Size => Count * ElementSize;

        /// <summary>
        /// Gets the size of a single element (in bytes).
        /// </summary>
        public int ElementSize { get; }

        /// <summary>
        /// Gets a boolean indicating whether the data at the current pointer
        /// can be read (<c>true</c>) or not (<c>false</c>).
        /// If <see cref="IsDisposed"/> is <c>true</c>, the returned boolean 
        /// is always <c>false</c>.
        /// </summary>
        public bool CanRead => canRead && !IsDisposed;
        private readonly bool canRead;

        /// <summary>
        /// Gets a boolean indicating whether the data at the current pointer
        /// can be modified (<c>true</c>) or not (<c>false</c>).
        /// If <see cref="IsDisposed"/> is <c>true</c>, the returned boolean 
        /// is always <c>false</c>.
        /// </summary>
        public bool CanWrite => canWrite && !IsDisposed;
        private readonly bool canWrite;

        /// <summary>
        /// Gets a boolean which indicates whether the unmanaged data this 
        /// <see cref="Pointer"/> points to is available and can be accessed
        /// (<c>true</c>) or if the <see cref="StartAddress"/> is no longer 
        /// valid and the data at this position was released 
        /// properly (<c>false</c>).
        /// </summary>
        public bool IsDisposed { get; private set; } = false;

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="MemoryPointer"/> base class.
        /// </summary>
        /// <param name="elementCount">
        /// The amount of elements of the type accessible
        /// through the <see cref="StartAddress"/>.
        /// </param>
        /// <param name="canRead">
        /// <c>true</c> if the data at the <see cref="StartAddress"/> can be 
        /// read, <c>false</c> otherwise.
        /// </param>
        /// <param name="canWrite">
        /// <c>true</c> if the data at the <see cref="StartAddress"/> can be 
        /// modified, <c>false</c> otherwise.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="elementCount"/> is 
        /// less than/equal to 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the size of <see cref="DataT"/> couldn't 
        /// be determined.
        /// </exception>
        protected MemoryPointer(Type elementType, int elementCount, 
            bool canRead, bool canWrite)
        {
            if (!canRead && !canWrite)
                throw new ArgumentException("The pointer must be at least " +
                    "readable or writable!");
            if (elementCount < 1)
                throw new ArgumentOutOfRangeException(nameof(elementCount));
            ElementType = elementType ?? 
                throw new ArgumentNullException(nameof(elementType));

            dataLength = elementCount;
            this.canRead = canRead;
            this.canWrite = canWrite;

            try { ElementSize = Marshal.SizeOf(elementType); }
            catch (Exception exc)
            {
                throw new ArgumentException("The size of the specified" +
                    "element type couldn't be determined.", exc);
            }
        }

        ~MemoryPointer()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes the pointer and releases the unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                OnDispose();
                IsDisposed = true;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release the the unmanaged memory accessible by the current
        /// <see cref="Pointer"/> and performs all required tasks to 
        /// prevent memory leaks. This method is automatically called 
        /// when the current <see cref="Pointer"/> is disposed.
        /// </summary>
        protected abstract void OnDispose();

        /// <summary>
        /// Copies the unmanaged data to a managed byte array.
        /// </summary>
        /// <returns>A new <see cref="byte"/> array instance.</returns>
        public byte[] ToBuffer()
        {
            byte[] buffer = new byte[Size];
            Marshal.Copy(StartAddress, buffer, 0, buffer.Length);
            return buffer;
        }

        /// <summary>
        /// Gets the address of an element at a specific index.
        /// </summary>
        /// <param name="elementIndex">
        /// The element index.
        /// </param>
        /// <returns>A new <see cref="IntPtr"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="elementIndex"/> is less than 0
        /// or greater than/equal to <see cref="Count"/>.
        /// </exception>
        public IntPtr GetElementAddress(int elementIndex)
        {
            if (elementIndex < 0 || elementIndex >= Count)
                throw new ArgumentOutOfRangeException(nameof(elementIndex));

            if (Environment.Is64BitOperatingSystem)
                return new IntPtr(StartAddress.ToInt64() 
                    + elementIndex * ElementSize);
            else
                return new IntPtr(StartAddress.ToInt32() 
                    + elementIndex * ElementSize);
        }
    }

    /// <summary>
    /// Provides the base class from which those classes are derived which
    /// provide a typed, managed access to unmanaged data.
    /// </summary>
    /// <typeparam name="DataT">
    /// The managed type which represents the data element(s) in unmanaged
    /// memory.
    /// </typeparam>
    public abstract class MemoryPointer<DataT> : MemoryPointer
        where DataT : unmanaged
    {
        private class GenericMemoryPointer<T> : MemoryPointer<T>
            where T : unmanaged
        {
            public GenericMemoryPointer(IntPtr address, int dataLength,
                bool canRead, bool canWrite, bool freeHGlobal = true)
                : base(dataLength, canRead, canWrite)
            {
                dataAddress = address;
                this.freeHGlobal = freeHGlobal;
            }

            public override IntPtr StartAddress => dataAddress;
            private IntPtr dataAddress;

            private readonly bool freeHGlobal;

            protected override void OnDispose()
            {
                if (freeHGlobal)
                {
                    Marshal.FreeHGlobal(dataAddress);
                    dataAddress = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="MemoryPointer{DataT}"/> base class.
        /// </summary>
        /// <param name="dataLength">
        /// The amount of elements of the <see cref="DataT"/> type accessible
        /// through the <see cref="StartAddress"/>.
        /// </param>
        /// <param name="canRead">
        /// <c>true</c> if the data at the <see cref="StartAddress"/> can be 
        /// read, <c>false</c> otherwise.
        /// </param>
        /// <param name="canWrite">
        /// <c>true</c> if the data at the <see cref="StartAddress"/> can be 
        /// modified, <c>false</c> otherwise.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="dataLength"/> is less than/equal 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the size of <see cref="DataT"/> couldn't 
        /// be determined.
        /// </exception>
        /// <remarks>
        /// This method requires <typeparamref name="DataT"/> to have a 
        /// sequential or explicit layout.
        /// </remarks>
        public MemoryPointer(int dataLength, bool canRead, bool canWrite)
            : base(typeof(DataT), dataLength, canRead, canWrite)
        { }

        /// <summary>
        /// Reads in a data element from unmanaged memory.
        /// </summary>
        /// <param name="index">
        /// The index of the element which should be retrieved.
        /// </param>
        /// <returns>A new <see cref="DataT"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="index"/> is less than 0
        /// or greater than/equal to <see cref="Count"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <see cref="IsDisposed"/> is <c>true</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="CanRead"/> is <c>false</c>.
        /// </exception>
        public DataT Read(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
            if (!CanRead)
                throw new NotSupportedException("The current pointer did " +
                    "not support reading operations!");

#if UNSAFE
            unsafe
            {
                DataT* element = (DataT*)StartAddress.ToPointer();
                element += index;
                return *element;
            }
#else
            IntPtr elementPtr;
            if (Environment.Is64BitOperatingSystem)
                elementPtr = new IntPtr(StartAddress.ToInt64() 
                    + ElementSize * index);
            else
                elementPtr = new IntPtr(StartAddress.ToInt32() 
                    + ElementSize * index);

            return (DataT)Marshal.PtrToStructure(elementPtr, typeof(DataT));
#endif
        }

        /// <summary>
        /// Reads in the first data element from unmanaged memory.
        /// </summary>
        /// <returns>A new <see cref="DataT"/> instance.</returns>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <see cref="IsDisposed"/> is <c>true</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="CanRead"/> is <c>false</c>.
        /// </exception>
        public DataT Read()
        {
            return Read(0);
        }

        /// <summary>
        /// Reads in data elements from unmanaged memory.
        /// </summary>
        /// <param name="index">
        /// The index of the first element which should be retrieved.
        /// </param>
        /// <param name="length">
        /// The amount of elements to be read.
        /// </param>
        /// <returns>A new <see cref="DataT"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="index"/> is less than 0
        /// or greater than/equal to <see cref="Count"/>, when 
        /// <paramref name="length"/> is less than/equal to 0, or when
        /// the <paramref name="index"/> combined with the 
        /// <paramref name="length"/> would exceed the 
        /// <see cref="Count"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <see cref="IsDisposed"/> is <c>true</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="CanRead"/> is <c>false</c>.
        /// </exception>
        public DataT[] Read(int index, int length)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("offset");
            if (length <= 0 || (index + length) > Count)
                throw new ArgumentOutOfRangeException("length");
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
            if (!CanRead)
                throw new NotSupportedException("The current pointer did " +
                    "not support reading operations!");

            DataT[] dataArray = new DataT[length];

#if UNSAFE
            unsafe
            {
                DataT* element = (DataT*)StartAddress.ToPointer();
                element += index;

                for (int i = 0; i < length; i++) dataArray[i] = *(element + i);
            }
#else
            for (int i = 0; i < length; i++)
            {
                IntPtr ptr;
                if (Environment.Is64BitOperatingSystem)
                    ptr = new IntPtr(StartAddress.ToInt64() + (i + index)
                        * ElementSize);
                else
                    ptr = new IntPtr(StartAddress.ToInt32() + (i + index)
                        * ElementSize);

                dataArray[i] = (DataT)Marshal.PtrToStructure(ptr,
                    typeof(DataT));
            }
#endif

            return dataArray;
        }

        /// <summary>
        /// Writes a data element to unmanaged memory.
        /// </summary>
        /// <param name="data">
        /// The data element to be written into unmanaged memory.
        /// </param>
        /// <param name="index">
        /// The index in the unmanaged data "array" where the element should 
        /// be written to (not to be confused with a byte offset).
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="index"/> is less than 0
        /// or greater than/equal to <see cref="Count"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <see cref="IsDisposed"/> is <c>true</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="CanWrite"/> is <c>false</c>.
        /// </exception>
        public void Write(DataT data, int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("offset");
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
            if (!CanWrite)
                throw new NotSupportedException("The current pointer did " +
                    "not support writing operations!");

            int size = Marshal.SizeOf(typeof(DataT));

            IntPtr elementPtr;
            if (Environment.Is64BitOperatingSystem)
                elementPtr = new IntPtr(StartAddress.ToInt64() + size * index);
            else
                elementPtr = new IntPtr(StartAddress.ToInt32() + size * index);

            Marshal.StructureToPtr(data, elementPtr, false);
        }

        /// <summary>
        /// Writes a data element as first element to unmanaged memory.
        /// </summary>
        /// <param name="data">
        /// The data element to be written into unmanaged memory.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <see cref="IsDisposed"/> is <c>true</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="CanWrite"/> is <c>false</c>.
        /// </exception>
        public void Write(DataT data)
        {
            Write(data, 0);
        }

        /// <summary>
        /// Writes a collection of data elements to unmanaged memory.
        /// </summary>
        /// <param name="data">
        /// The data element to be written into unmanaged memory.
        /// </param>
        /// <param name="index">
        /// The index in the unmanaged data "array" where the element should 
        /// be written to (not to be confused with a byte offset).
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="index"/> is less than 0
        /// or greater than/equal to <see cref="Count"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <paramref name="data"/> was empty or contained 
        /// too many elements to be written into unmanaged memory at the 
        /// specified <paramref name="index"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <see cref="IsDisposed"/> is <c>true</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="CanWrite"/> is <c>false</c>.
        /// </exception>
        public void Write(IList<DataT> data, int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("offset");
            if (data.Count <= 0)
                throw new ArgumentException("The data collection must not " +
                    "be empty!");
            if ((index + data.Count) > Count)
                throw new ArgumentException("The data collection was too " +
                    "big to be written into memory at the specified index!");
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
            if (!CanWrite)
                throw new NotSupportedException("The current pointer did " +
                    "not support writing operations!");

            int size = Marshal.SizeOf(typeof(DataT));

            for (int i = 0; i < data.Count; i++)
            {
                IntPtr ptr;
                if (Environment.Is64BitOperatingSystem)
                    ptr = new IntPtr(StartAddress.ToInt64() + (i + index)
                        * size);
                else
                    ptr = new IntPtr(StartAddress.ToInt32() + (i + index)
                        * size);
                Marshal.StructureToPtr(data[i], ptr, false);
            }
        }

        /// <summary>
        /// Creates a new pointer to an existing unmanaged memory segment.
        /// </summary>
        /// <param name="startAddress">
        /// The adress of the start of the segment in unmanaged memory.
        /// </param>
        /// <param name="elementCount">
        /// The amount of elements of type <see cref="DataT"/> in the
        /// unmanaged memory segment.
        /// </param>
        /// <param name="canRead">
        /// <c>true</c> if the data at the <see cref="StartAddress"/> can be 
        /// read, <c>false</c> otherwise.
        /// </param>
        /// <param name="canWrite">
        /// <c>true</c> if the data at the <see cref="StartAddress"/> can be 
        /// modified, <c>false</c> otherwise.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MemoryPointer{DataT}"/> class.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="dataLength"/> is less than/equal 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the size of <see cref="DataT"/> couldn't 
        /// be determined.
        /// </exception>
        /// <remarks>
        /// This method requires <typeparamref name="DataT"/> to have a 
        /// sequential or explicit layout.
        /// </remarks>
        public static MemoryPointer<DataT> Create(IntPtr startAddress, 
            int elementCount, bool canRead, bool canWrite)
        {
            return new GenericMemoryPointer<DataT>(startAddress, elementCount,
                canRead, canWrite, false);
        }

        /// <summary>
        /// Initializes a new pointer with full read/write access and 
        /// allocates the required unmanaged memory.
        /// </summary>
        /// <param name="length">
        /// The amount of <see cref="DataT"/> elements for which the
        /// memory should be allocated.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="MemoryPointer{DataT}"/> class.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="length"/> is less than/equal to 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the size of <see cref="DataT"/> couldn't 
        /// be determined.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// Is thrown when there is insufficient memory to satisfy the
        /// request.
        /// </exception>
        /// <remarks>
        /// This method requires <typeparamref name="DataT"/> to have a 
        /// sequential or explicit layout.
        /// </remarks>
        public static MemoryPointer<DataT> Initialize(int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            int elementSize = Marshal.SizeOf(typeof(DataT));
            IntPtr ptr = Marshal.AllocHGlobal(elementSize * length);

            return new GenericMemoryPointer<DataT>(ptr, length, true, true);
        }

        /// <summary>
        /// Initializes a new pointer using data from an existing byte buffer.
        /// </summary>
        /// <param name="buffer">The byte buffer.</param>
        /// <returns>
        /// A new instance of the <see cref="MemoryPointer{DataT}"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the length of the <paramref name="buffer"/> doesn't
        /// match the size of one (or more) instances of the type
        /// <typeparamref name="DataT"/>.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// Is thrown when there is insufficient memory to satisfy the request.
        /// </exception>
        /// <remarks>
        /// The amount of elements is automatically determined using the
        /// length of <paramref name="buffer"/> and the size of a single
        /// <typeparamref name="DataT"/> instance.
        /// This method requires <typeparamref name="DataT"/> to have a 
        /// sequential or explicit layout.
        /// </remarks>
        public static MemoryPointer<DataT> Initialize(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            int instanceSize = GetDataSize(1);

            if ((buffer.Length % instanceSize) != 0)
                throw new ArgumentException("The length of the specified " +
                    "buffer doesn't match the size of one (or more) " +
                    "instances of the type '" + typeof(DataT).Name + "'.");

            IntPtr pointer = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, pointer, buffer.Length);

            return new GenericMemoryPointer<DataT>(pointer, buffer.Length, 
                true, true);
        }

        /// <summary>
        /// Converts one or more instances of <typeparamref name="DataT"/>
        /// to a managed byte buffer.
        /// </summary>
        /// <param name="instances">The instances to be converted.</param>
        /// <returns>A new <see cref="byte"/> array instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="instances"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="instances"/> contained no elements
        /// or when the size of <see cref="DataT"/> couldn't be determined.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// Is thrown when there is insufficient memory to satisfy the request.
        /// </exception>
        /// <remarks>
        /// This method requires <typeparamref name="DataT"/> to have a 
        /// sequential or explicit layout.
        /// </remarks>
        public static byte[] ConvertInstances(params DataT[] instances)
        {
            if (instances == null)
                throw new ArgumentNullException(nameof(instances));
            if (instances.Length == 0)
                throw new ArgumentException("No instances were specified.");

            using (MemoryPointer<DataT> pointer = Initialize(instances.Length))
            {
                for (int i = 0; i < instances.Length; i++)
                    pointer.Write(instances[i], i);
                return pointer.ToBuffer();
            }
        }

        /// <summary>
        /// Converts a byte buffer into one or more instances 
        /// of <typeparamref name="DataT"/>.
        /// </summary>
        /// <param name="buffer">A byte array.</param>
        /// <returns>
        /// A new array of <typeparamref name="DataT"/> instances.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the converted buffer contained no elements or
        /// when the size of <see cref="DataT"/> couldn't be determined.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// Is thrown when there is insufficient memory to satisfy the request.
        /// </exception>
        /// <remarks>
        /// This method requires <typeparamref name="DataT"/> to have a 
        /// sequential or explicit layout.
        /// </remarks>
        public static DataT[] ConvertBuffer(byte[] buffer)
        {
            using (MemoryPointer<DataT> pointer = Initialize(buffer))
            {
                DataT[] instances = new DataT[pointer.Count];
                for (int i = 0; i < instances.Length; i++)
                    instances[i] = pointer.Read(i);
                return instances;
            }
        }

        /// <summary>
        /// Calculates how much space (in bytes) a specified amount of elements
        /// of <see cref="DataT"/> would use in unmanaged memory.
        /// </summary>
        /// <param name="elementCount">
        /// The amount of elements.
        /// </param>
        /// <returns>An <see cref="int"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="elementCount"/> is less than 1.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the size of the <see cref="DataT"/> couldn't be
        /// determined. See the inner exception for more details.
        /// </exception>
        private static int GetDataSize(int elementCount)
        {
            if (elementCount < 1)
                throw new ArgumentOutOfRangeException("elementCount");

            int elementSize;
            try { elementSize = Marshal.SizeOf(typeof(DataT)); }
            catch (Exception exc)
            {
                throw new ArgumentException("The size of the specified" +
                    "data type couldn't be determined!", exc);
            }

            return elementSize * elementCount;
        }
    }
}
