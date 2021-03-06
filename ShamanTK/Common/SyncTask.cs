﻿/* 
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

namespace ShamanTK.Common
{
    /// <summary>
    /// Describes the different states of a <see cref="SyncTask"/>.
    /// </summary>
    public enum SyncTaskState
    {
        /// <summary>
        /// The task was initialized, but hasn't been started yet.
        /// </summary>
        Idle,
        /// <summary>
        /// The task is currently running and is being processed by a
        /// <see cref="SyncTaskScheduler"/>.
        /// </summary>
        Running,
        /// <summary>
        /// The task was finished successfully. If any object instances were
        /// generated, these can be retrieved now.
        /// </summary>
        Finished,
        /// <summary>
        /// The task failed. The error information can be retrieved from the
        /// <see cref="SyncTask.Error"/> property. No object instances were
        /// generated or published.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Represents a larger, time-consuming task that can be executed in small 
    /// steps without "blocking" the thread where it's created and executed 
    /// using a parent <see cref="SyncTaskScheduler"/>. 
    /// The <see cref="Finished"/> and <see cref="Failed"/> events and
    /// all property changes occur in the same thread where the 
    /// <see cref="SyncTaskScheduler.Continue(TimeSpan)"/> method is called.
    /// </summary>
    public abstract class SyncTask
    {
        /// <summary>
        /// Gets the state of the current <see cref="SyncTask"/>.
        /// </summary>
        public SyncTaskState CurrentState { get; private set; }
            = SyncTaskState.Idle;

        /// <summary>
        /// Occurs after the value of <see cref="CurrentState"/> was changed.
        /// </summary>
        public event EventHandler StateChanged;

        /// <summary>
        /// Occurs after the <see cref="CurrentState"/> changed to
        /// <see cref="SyncTaskState.Finished"/> or
        /// <see cref="SyncTaskState.Failed"/>.
        /// </summary>
        public event EventHandler Completed;

        /// <summary>
        /// Gets the error which occurred during the execution of the 
        /// current <see cref="SyncTask"/> if 
        /// <see cref="CurrentState"/> is <see cref="SyncTaskState.Failed"/>,
        /// or null otherwise.
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>
        /// Gets the name of the current <see cref="SyncTask"/> instance.
        /// Can be null.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncTask"/> class
        /// with a <see cref="CurrentState"/> of 
        /// <see cref="SyncTaskState.Idle"/>.
        /// The task will be started by an <see cref="SyncTaskScheduler"/>
        /// this instance is added to.
        /// </summary>
        /// <param name="name">
        /// The name of the new <see cref="SyncTask"/> instance.
        /// Can be null.
        /// </param>
        protected SyncTask(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Continues the task in the current thread for a specific amount 
        /// of time.
        /// </summary>
        /// <param name="timeout">
        /// The intended amount of time available to continue the task.
        /// </param>
        /// <returns>
        /// <c>true</c> if the task was started/continued without errors and
        /// should be invoked another time to continue the task,
        /// <c>false</c> if the task finished/failed in this step or in another
        /// step before and this method of this instance shouldn't be 
        /// invoked anymore.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="timeout"/> is zero or negative.
        /// </exception>
        internal bool Continue(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            if (CurrentState == SyncTaskState.Idle)
            {
                CurrentState = SyncTaskState.Running;
                OnStateChangedSafe();
            }

            if (CurrentState == SyncTaskState.Running)
            {
                DateTime start = DateTime.Now;

                try
                {
                    while ((DateTime.Now - start) < timeout &&
                        CurrentState == SyncTaskState.Running)
                    {
                        if (!Continue())
                        {
                            CurrentState = SyncTaskState.Finished;
                            OnStateChangedSafe();
                        }
                    }
                }
                catch (Exception exc)
                {
                    Error = exc;
                    CurrentState = SyncTaskState.Failed;
                    OnStateChangedSafe();
                }
            }

            return CurrentState == SyncTaskState.Running;
        }

        /// <summary>
        /// Gets called during the operation when the value of
        /// <see cref="CurrentState"/> changed, but catches any exception
        /// which might occur and stores it into the <see cref="Error"/>
        /// property after setting <see cref="CurrentState"/> to
        /// <see cref="SyncTaskState.Failed"/>.
        /// In that case, the <see cref="OnStateChanged"/> is called one
        /// more time - any failure in that last call is appended to the
        /// <see cref="Error"/>.
        /// </summary>
        protected void OnStateChangedSafe()
        {
            try { OnStateChanged(); }
            catch (Exception exc)
            {
                Error = new ApplicationException("One of the state change " +
                    "event handlers caused an exception. The operation " +
                    "state may be invalid.", exc);
                CurrentState = SyncTaskState.Failed;

                try { OnStateChanged(); }
                catch
                {
                    Error = new ApplicationException("One of the state " +
                        "change event handlers caused an exception. " +
                        "Additionally, the error handler failed. The " +
                        "operation state may be invalid.", exc);
                }
            }
        }
        
        /// <summary>
        /// Gets called during the operation when the value of
        /// <see cref="CurrentState"/> changed. This method shouldn't be
        /// called directly - the <see cref="OnStateChangedSafe"/> method
        /// should be used to handle any occurring exception in the
        /// state change event handlers and change the state of this
        /// <see cref="SyncTask"/> accordingly.
        /// </summary>
        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);

            if (CurrentState == SyncTaskState.Finished ||
                CurrentState == SyncTaskState.Failed)
                Completed?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// Performs another small step in the preparation of the object 
        /// instance. The execution shouldn't take more than few milliseconds.
        /// </summary>
        /// <returns>
        /// <c>false</c> if the preparation was finished and no more steps
        /// (invocations of this method) are needed, 
        /// <c>true</c> if the preparation step was executed successfully, but
        /// more steps (invocations of this method) are required.
        /// </returns>
        /// <exception cref="Exception">
        /// Is thrown when the object generation failed.
        /// </exception>
        protected abstract bool Continue();
    }

    /// <summary>
    /// Specifies a method that defines the reaction to the completion of a
    /// <see cref="SyncTask{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the instance generated by the <see cref="SyncTask{T}"/>.
    /// </typeparam>
    /// <param name="success">
    /// <c>true</c> if the operation was successful and the result of the 
    /// operation is provided in <paramref name="result"/>, <c>false</c>
    /// if the operation failed with the error provided with
    /// <paramref name="error"/>.
    /// </param>
    /// <param name="result">
    /// The result of the operation or null, if <paramref name="success"/>
    /// is <c>false</c>.
    /// </param>
    /// <param name="error">
    /// The error which caused the operation to fail or null, if
    /// <paramref name="success"/> is <c>true</c>.
    /// </param>
    public delegate void SyncTaskCompleted<T>(bool success, T result,
        Exception error);

    /// <summary>
    /// Represents a larger, time-consuming task with a result of the type 
    /// <typeparamref name="T"/>, that can be executed in small 
    /// steps without "blocking" the thread where it's created and executed 
    /// using a parent <see cref="SyncTaskScheduler"/>. 
    /// The <see cref="Finished"/> and <see cref="Failed"/> events and
    /// all property changes occur in the same thread where the 
    /// <see cref="SyncTaskScheduler.Continue(TimeSpan)"/> method is called.
    /// </summary>
    public abstract class SyncTask<T> : SyncTask
    {
        /// <summary>
        /// Gets the result of the operation or null, if the task wasn't
        /// finished successfully (yet).
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Gets a boolean which indicates whether the current 
        /// <see cref="SyncTask{T}"/> was completed successfully and
        /// <see cref="Value"/> contains the result of the operation
        /// (<c>true</c>) or whether the operation didn't finish 
        /// successfully (yet) (<c>false</c>).
        /// </summary>
        public bool HasValue => Value != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncTask{T}"/> class
        /// with a <see cref="CurrentState"/> of 
        /// <see cref="SyncTaskState.Idle"/>.
        /// The task will be started by an <see cref="SyncTaskScheduler"/>
        /// this instance is added to.
        /// </summary>
        /// <param name="name">
        /// The name of the new <see cref="SyncTask"/> instance.
        /// Can be null.
        /// </param>
        protected SyncTask(string name) : base(name) { }

        /// <summary>
        /// Creates a new <see cref="EventHandler{TEventArgs}"/> and
        /// adds it to <see cref="Completed"/>.
        /// </summary>
        /// <param name="onSuccess">
        /// The action which is executed if the <see cref="Completed"/> event
        /// occurs and 
        /// <see cref="SyncTaskCompletedEventArgs{ResultT}.Success"/> of its
        /// event arguments is <c>true</c>.
        /// The parameter of the <see cref="Action{T}"/> is the 
        /// operation result.
        /// </param>
        /// <param name="logError">
        /// <c>true</c> to log errors to the <see cref="Log"/>,
        /// <c>false</c> to not do anything when the task fails.
        /// </param>
        /// <returns>
        /// The current <see cref="SyncTask{T}"/> instance to allow for
        /// concatenation of multiple invocations of this method.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="onSuccess"/> is null.
        /// </exception>
        public SyncTask<T> Subscribe(Action<T> onSuccess, bool logError = true)
        {
            if (onSuccess == null)
                throw new ArgumentNullException(nameof(onSuccess));

            Completed += (s, e) =>
            {
                if (HasValue) onSuccess(Value);
                else if (logError) Log.Error(new Exception("The task " +
                    "execution failed.", Error), Name);
            };

            return this;
        }

        /// <summary>
        /// Creates a new <see cref="EventHandler{TEventArgs}"/> and
        /// adds it to <see cref="Completed"/>.
        /// </summary>
        /// <param name="onError">
        /// The action which is executed if the <see cref="Completed"/> event
        /// occurs and 
        /// <see cref="SyncTaskCompletedEventArgs{ResultT}.Success"/> of its
        /// event arguments is <c>false</c>.
        /// The parameter of the <see cref="Action{T}"/> is the 
        /// error that ocurred during the execution of the operation.
        /// </param>
        /// <returns>
        /// The current <see cref="SyncTask{T}"/> instance to allow for
        /// concatenation of multiple invocations of this method.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="onError"/> is null.
        /// </exception>
        public SyncTask<T> Subscribe(Action<Exception> onError)
        {
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));

            Completed += (s, e) =>
            {
                if (!HasValue) onError(Error);
            };

            return this;
        }

        /// <summary>
        /// Creates a new <see cref="EventHandler{TEventArgs}"/> and
        /// adds it to <see cref="Completed"/>.
        /// </summary>
        /// <param name="onCompleted">
        /// The action which is executed if the <see cref="Completed"/> event
        /// occurs.
        /// </param>
        /// <returns>
        /// The current <see cref="SyncTask{T}"/> instance to allow for
        /// concatenation of multiple invocations of this method.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="onCompleted"/> is null.
        /// </exception>
        public SyncTask<T> Subscribe(Action<SyncTask<T>> onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            Completed += (s, e) => onCompleted(this);

            return this;
        }

        /// <summary>
        /// Creates a new <see cref="EventHandler{TEventArgs}"/> and
        /// adds it to <see cref="Completed"/>.
        /// </summary>
        /// <param name="onCompleted">
        /// The action which is executed if the <see cref="Completed"/> event
        /// occurs.
        /// </param>
        /// <returns>
        /// The current <see cref="SyncTask{T}"/> instance to allow for
        /// concatenation of multiple invocations of this method.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="onCompleted"/> is null.
        /// </exception>
        public SyncTask<T> Subscribe(SyncTaskCompleted<T> onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            Completed += (s, e) => onCompleted(HasValue, Value, Error);

            return this;
        }

        /// <summary>
        /// Creates a new <see cref="EventHandler{TEventArgs}"/> and
        /// adds it to <see cref="Completed"/>.
        /// </summary>
        /// <param name="onCompleted">
        /// The action which is executed if the <see cref="Completed"/> event
        /// occurs.
        /// </param>
        /// <returns>
        /// The current <see cref="SyncTask{T}"/> instance to allow for
        /// concatenation of multiple invocations of this method.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="onCompleted"/> is null.
        /// </exception>
        public SyncTask<T> Subscribe(Action onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            Completed += (s, e) => onCompleted();

            return this;
        }

        /// <summary>
        /// Performs another small step in the preparation of the object 
        /// instance.
        /// </summary>
        /// <returns>
        /// <c>false</c> if the preparation was finished and no more steps
        /// (invocations of this method) are needed, 
        /// <c>true</c> if the preparation step was executed successfully, but
        /// more steps (invocations of this method) are required.
        /// </returns>
        /// <exception cref="Exception">
        /// Is thrown when the object generation failed.
        /// </exception>
        protected override bool Continue()
        {
            if (CurrentState == SyncTaskState.Running)
                Value = ContinueGenerator();
            return Value == null;
        }

        /// <summary>
        /// Performs another small step in the preparation of the object 
        /// instance.
        /// </summary>
        /// <returns>
        /// The finished object instance of type <typeparamref name="T"/>
        /// or null if more steps (invocations of this method) are required.
        /// </returns>
        /// <exception cref="Exception">
        /// Is thrown when the object generation failed.
        /// </exception>
        protected abstract T ContinueGenerator();
    }
}
