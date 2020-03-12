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
    /// Provides the data for a completed <see cref="SyncTask{T}"/>
    /// event.
    /// </summary>
    /// <typeparam name="ResultT">
    /// The type of the result.
    /// </typeparam>
    public class SyncTaskCompletedEventArgs<ResultT> : EventArgs
        where ResultT : class
    {
        /// <summary>
        /// Gets the result of the operation or null, if
        /// <see cref="Success"/> is <c>false</c>.
        /// </summary>
        public ResultT Result { get; }

        /// <summary>
        /// Gets the exception which caused the operation to fail
        /// or null, if <see cref="Success"/> is <c>true</c>.
        /// </summary>
        public Exception Error { get; }

        /// <summary>
        /// Gets a value which indicates whether the <see cref="SyncTask{T}"/>,
        /// from which this <see cref="SyncTaskCompletedEventArgs{ResultT}"/>
        /// originated from, was completed successfully with a result 
        /// (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool Success => Result != null;

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="SyncTaskCompletedEventArgs{ResultT}"/> class for a
        /// successfully completed <see cref="SyncTask{T}"/>.
        /// </summary>
        /// <param name="result">
        /// The result of the <see cref="SyncTask{T}"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="result"/> is null.
        /// </exception>
        public SyncTaskCompletedEventArgs(ResultT result)
        {
            Result = result ??
                throw new ArgumentNullException(nameof(result));
        }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="SyncTaskCompletedEventArgs{ResultT}"/> class for a
        /// failed <see cref="SyncTask{T}"/>.
        /// </summary>
        /// <param name="error">
        /// The error which caused the <see cref="SyncTask{T}"/> to fail.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="error"/> is null.
        /// </exception>
        public SyncTaskCompletedEventArgs(Exception error)
        {
            Error = error ??
                throw new ArgumentNullException(nameof(error));
        }

        /// <summary>
        /// Gets the value of <see cref="Result"/> or throws an exception
        /// with the <see cref="Error"/> if <see cref="Success"/> is
        /// <c>false</c>.
        /// </summary>
        /// <returns>
        /// The <see cref="Result"/> of this instance.
        /// </returns>
        /// <exception cref="Exception">
        /// Is thrown when <see cref="Success"/> is <c>false</c>.
        /// The thrown exception is <see cref="Error"/>.
        /// </exception>
        public ResultT GetResult()
        {
            if (Success) return Result;
            else throw Error;
        }
    }
}
