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
        /// occurs after the value of <see cref="CurrentState"/> was changed.
        /// </summary>
        public event EventHandler StateChanged;

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
    /// Represents a larger, time-consuming task with a result of the type 
    /// <typeparamref name="T"/>, that can be executed in small 
    /// steps without "blocking" the thread where it's created and executed 
    /// using a parent <see cref="SyncTaskScheduler"/>. 
    /// The <see cref="Finished"/> and <see cref="Failed"/> events and
    /// all property changes occur in the same thread where the 
    /// <see cref="SyncTaskScheduler.Continue(TimeSpan)"/> method is called.
    /// </summary>
    public abstract class SyncTask<T> : SyncTask
        where T : class
    {
        /// <summary>
        /// Gets the result of the object factory or null, if the task wasn't
        /// finished successfully (yet).
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Occurs after the <see cref="CurrentState"/> changed to
        /// <see cref="SyncTaskState.Finished"/> or
        /// <see cref="SyncTaskState.Failed"/>.
        /// </summary>
        public event EventHandler<SyncTaskCompletedEventArgs<T>> Completed;

        /// <summary>
        /// Gets a boolean which indicates whether the current 
        /// <see cref="SyncTask{T}"/> was completed successfully and
        /// <see cref="Value"/> contains the result of the operation
        /// (<c>true</c>) or not (<c>false</c>).
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
        /// Creates a new 
        /// <see cref="EventHandler{SyncTaskCompletedEventArgs{T}}"/> and
        /// adds it to <see cref="Completed"/>.
        /// Errors are written to the <see cref="Log"/> by default.
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
        /// A new instance of the 
        /// <see cref="EventHandler{SyncTaskCompletedEventArgs{T}}"/>, which
        /// was added to <see cref="Completed"/>.
        /// </returns>
        public EventHandler<SyncTaskCompletedEventArgs<T>> AddFinalizer(
            Action<T> onSuccess, bool logError = true)
        {
            void finalizer(object sender, SyncTaskCompletedEventArgs<T> args)
            {
                if (args.Success) onSuccess(args.Result);
                else if (logError) Log.Error(new Exception("The task " +
                    "execution failed.", args.Error), Name);
            }

            Completed += finalizer;
            return finalizer;
        }

        /// <summary>
        /// Creates a new 
        /// <see cref="EventHandler{SyncTaskCompletedEventArgs{T}}"/> and
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
        /// <param name="onError">
        /// The action which is executed if the <see cref="Completed"/> event
        /// occurs and 
        /// <see cref="SyncTaskCompletedEventArgs{ResultT}.Success"/> of its
        /// event arguments is <c>false</c>.
        /// The parameter of the <see cref="Action{Exception}"/> is the 
        /// operation error.
        /// </param>
        /// <returns>
        /// A new instance of the 
        /// <see cref="EventHandler{SyncTaskCompletedEventArgs{T}}"/>, which
        /// was added to <see cref="Completed"/>.
        /// </returns>
        public EventHandler<SyncTaskCompletedEventArgs<T>> AddFinalizer(
            Action<T> onSuccess, Action<Exception> onError)
        {
            void finalizer(object sender, SyncTaskCompletedEventArgs<T> args)
            {
                if (args.Success) onSuccess(args.Result);
                else onError(args.Error);
            }

            Completed += finalizer;
            return finalizer;
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
        /// Gets called during the operation when the value of
        /// <see cref="CurrentState"/> changed.
        /// </summary>
        protected override void OnStateChanged()
        {
            base.OnStateChanged();

            if (CurrentState == SyncTaskState.Finished)
            {
                Completed?.Invoke(this,
                    new SyncTaskCompletedEventArgs<T>(Value));
            }
            else if (CurrentState == SyncTaskState.Failed)
                Completed?.Invoke(this,
                    new SyncTaskCompletedEventArgs<T>(Error));
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
