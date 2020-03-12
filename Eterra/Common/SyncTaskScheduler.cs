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
using System.Collections.Generic;

namespace Eterra.Common
{
    /// <summary>
    /// Represents a queue for <see cref="SyncTask"/> instances.
    /// </summary>
    public class SyncTaskScheduler
    {
        /// <summary>
        /// Gets the amount of pending tasks in the current
        /// <see cref="SyncTaskScheduler"/> instance.
        /// </summary>
        public int PendingTasks => tasks.Count;

        /// <summary>
        /// Occurs after a single loading task was completed (either finished
        /// successfully or failed) and the 
        /// <see cref="PendingTasks"/> property changed.
        /// </summary>
        public event EventHandler LoadingTaskCompleted;

        /// <summary>
        /// Occurs after all loading tasks were completed (either finished
        /// successfully or failed) and the 
        /// <see cref="PendingTasks"/> property changed to 0.
        /// </summary>
        public event EventHandler LoadingTasksCompleted;

        /// <summary>
        /// Occurs after a loading task was initiated and the 
        /// <see cref="PendingTasks"/> property changed.
        /// </summary>
        public event EventHandler LoadingTaskAdded;

        private readonly Queue<SyncTask> tasks = new Queue<SyncTask>();

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="SyncTaskScheduler"/> class.
        /// </summary>
        public SyncTaskScheduler() { }

        /// <summary>
        /// Enqueues a new, idle task to the <see cref="SyncTaskScheduler"/>.
        /// <see cref="Continue(TimeSpan)"/> needs to be called regularily
        /// to process this and the other tasks in the current queue.
        /// </summary>
        /// <param name="task">
        /// The task to be enqueued and processed by the current
        /// <see cref="SyncTaskScheduler"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="task"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="SyncTask.CurrentState"/> of 
        /// <paramref name="task"/> is not <see cref="SyncTaskState.Idle"/>.
        /// </exception>
        public void Enqueue(SyncTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            if (task.CurrentState != SyncTaskState.Idle)
                throw new ArgumentException("The state of the provided task " +
                    "is invalid.");

            tasks.Enqueue(task);

            try { LoadingTaskAdded?.Invoke(this, EventArgs.Empty); }
            catch (Exception exc)
            {
                Log.Error("A subscriber of the loading task added " +
                    "event in the resource manager failed.", exc);
            }
        }

        /// <summary>
        /// Continues the processing of the tasks in the current queue for a 
        /// specific amount of time.
        /// </summary>
        /// <param name="timeout">
        /// The intended amount of time available to continue a single task.
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
        public bool Continue(TimeSpan timeout)
        {
            if (tasks.Count > 0)
            {
                SyncTask currentTask = tasks.Peek();
                if (!currentTask.Continue(timeout))
                {
                    tasks.Dequeue();
                    try
                    {
                        EventArgs e = EventArgs.Empty;
                        if (PendingTasks > 0)
                            LoadingTaskCompleted?.Invoke(this, e);
                        else LoadingTasksCompleted?.Invoke(this, e);
                    }
                    catch (Exception exc)
                    {
                        Log.Error("A subscriber of the loading task(s) " +
                            "completed event in the resource manager " +
                            "failed.", exc);
                    }

                }
                return PendingTasks > 0;
            }
            else return false;
        }
    }
}
