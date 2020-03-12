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
    /// Provides a base class which implements the <see cref="IDisposable"/>
    /// pattern for classes storing unmanaged data which needs to be disposed.
    /// </summary>
    public abstract class DisposableBase : IDisposable
    {
        /// <summary>
        /// Gets a <see cref="System.ObjectDisposedException"/> with a default
        /// exception message which indicates that the object with the method
        /// which threw this exception was disposed and can't be used anymore.
        /// See <see cref="ThrowIfDisposed"/> for a shortcut method.
        /// </summary>
        protected ObjectDisposedException ObjectDisposedException { get; }
            = new ObjectDisposedException("The current instance has been " +
                "disposed and can't be used anymore.");

        /// <summary>
        /// Contains the value of unmanaged bytes, which will be notified to 
        /// the garbage collector as memory pressure.
        /// </summary>
        private readonly long unmanagedSize = 0;

        /// <summary>
        /// Gets a value indicating whether the current instance
        /// has been disposed (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public virtual bool IsDisposed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableBase"/> 
        /// base class.
        /// </summary>
        protected DisposableBase() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableBase"/> 
        /// base class.
        /// </summary>
        /// <param name="bytesAllocated">
        /// The size of the data in unmanaged memory which is allocated
        /// during the lifetime of this instance and is released when
        /// <see cref="Dispose(bool)"/> is called.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="bytesAllocated"/> is less than
        /// or equal to 0, or - on a 32-bit processor - larger than
        /// <see cref="int.MaxValue"/>.
        /// </exception>
        protected DisposableBase(long bytesAllocated)
        {
            try
            {
                GC.AddMemoryPressure(bytesAllocated);
                unmanagedSize = bytesAllocated;
            }
            catch (ArgumentOutOfRangeException) { throw; }
        }

        /// <summary>
        /// Finalizes and disposes the current instance.
        /// </summary>
        ~DisposableBase()
        {
            Dispose(false);
            IsDisposed = true;
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/>, when
        /// <see cref="IsDisposed"/> is <c>true</c> or does nothing otherwise.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">
        /// Is thrown when <see cref="IsDisposed"/> is <c>true</c>.
        /// </exception>
        protected void ThrowIfDisposed()
        {
            if (IsDisposed) throw ObjectDisposedException;
        }

        /// <summary>
        /// Performs tasks associated with freeing, releasing, or resetting 
        /// unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>false</c> to only dispose unmanaged resources and reset large 
        /// fields to null, <c>true</c> to dispose managed objects (which 
        /// implement <see cref="IDisposable"/>) too.
        /// </param>
        /// <remarks>
        /// This method is only called once by either the <see cref="Dispose"/>
        /// method of this class or the finalizer.
        /// </remarks>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Performs tasks associated with freeing, releasing, or resetting 
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                Dispose(true);
                if (unmanagedSize > 0)
                    GC.RemoveMemoryPressure(unmanagedSize);
                IsDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Checks whether a object instance derived of the
        /// <see cref="DisposableBase"/> type is usable or not.
        /// </summary>
        /// <param name="disposableObject">
        /// The object instance to be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified instance is null or disposed,
        /// <c>false</c> if the object is neither null nor disposed.
        /// </returns>
        public static bool IsNullOrDisposed(DisposableBase disposableObject)
        {
            return !(disposableObject != null && !disposableObject.IsDisposed);
        }
    }
}