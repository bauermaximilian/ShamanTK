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
using Eterra.Controls;
using Eterra.Graphics;
using Eterra.IO;
using Eterra.Sound;
using System;
using System.Reflection;

namespace Eterra
{
    /// <summary>
    /// Provides the base class for multimedia applications with simple 
    /// access to various functions for rendering graphics, playing back sound, 
    /// handling user input and importing/exporting resources.
    /// </summary>
    public abstract class EterraApplicationBase : IDisposable
    {
        private static readonly InvalidOperationException 
            notInitializedException = new InvalidOperationException("The " +
                "unit can't be accessed before the application was " +
                "initialized - the first possibility to access the unit is " +
                "in the 'Load' method.");

        private static readonly InvalidOperationException 
            alreadyDisposedException = new InvalidOperationException("The " +
                "unit can't be accessed after the application was closed - " +
                "the last possibility to access the unit is in the " +
                "'Unload' method.");

        /// <summary>
        /// Defines the target frequency of updates per second.
        /// </summary>
        public const int TargetUpdatesPerSecond = 60;

        /// <summary>
        /// Defines the target frequency of redraws per second.
        /// </summary>
        public const int TargetRedrawsPerSecond = 30;

        /// <summary>
        /// Gets the <see cref="ResourceManager"/> of this 
        /// <see cref="EterraApplicationBase"/> instance, which provides 
        /// functionality to import and export application resources from/to a 
        /// file system and to load/buffer these resources.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when this property is accessed before the application
        /// was successfully initialized or when the application was already
        /// closed and disposed.
        /// </exception>
        protected ResourceManager Resources
        {
            get
            {
                if (!IsInitialized) throw notInitializedException;
                else if (IsDisposed) throw alreadyDisposedException;
                else return resources;
            }
        }
        private ResourceManager resources;

        /// <summary>
        /// Gets the <see cref="ControlsManager"/> of this 
        /// <see cref="EterraApplicationBase"/> instance, which provides 
        /// access to the various available human interface devices (keyboard, 
        /// mouse, etc.).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when this property is accessed before the application
        /// was successfully initialized or when the application was already
        /// closed and disposed.
        /// </exception>
        protected ControlsManager Controls
        {
            get
            {
                if (!IsInitialized) throw notInitializedException;
                else if (IsDisposed) throw alreadyDisposedException;
                else return controls;
            }
        }
        private ControlsManager controls;

        /// <summary>
        /// Gets the <see cref="GraphicsManager"/> of this 
        /// <see cref="EterraApplicationBase"/> instance, which provides access
        /// to the available drawing functions and properties of the graphics 
        /// window.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when this property is accessed before the application
        /// was successfully initialized or when the application was already
        /// closed and disposed.
        /// </exception>
        protected GraphicsManager Graphics
        {
            get
            {
                if (!IsInitialized) throw notInitializedException;
                else if (IsDisposed) throw alreadyDisposedException;
                else return graphics;
            }
        }
        private GraphicsManager graphics;

        /// <summary>
        /// Gets the <see cref="SoundManager"/> of this 
        /// <see cref="EterraApplicationBase"/> instance, which provides access
        /// to the available functions for playing sound.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when this property is accessed before the application
        /// was successfully initialized or when the application was already
        /// closed and disposed.
        /// </exception>
        protected SoundManager Sound
        {
            get
            {
                if (!IsInitialized) throw notInitializedException;
                else if (IsDisposed) throw alreadyDisposedException;
                else return sound;
            }
        }
        private SoundManager sound;

        /// <summary>
        /// Gets a value indicating whether this 
        /// <see cref="EterraApplicationBase"/> instance is initialized and the 
        /// <see cref="Graphics"/>, <see cref="Sound"/>, <see cref="Controls"/>
        /// and <see cref="Resources"/> properties are accessible and non-null
        /// (<c>true</c>) or if the application hasn't been started yet 
        /// (<c>false</c>). Also check the <see cref="IsDisposed"/> property
        /// to verify that the application hasn't been closed yet - or use the
        /// <see cref="IsRunning"/> property instead.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this 
        /// <see cref="EterraApplicationBase"/> instance was closed/disposed 
        /// and can no longer be used (<c>true</c>) or not (<c>false</c>).
        /// Also check the <see cref="IsInitialized"/> property - or use the
        /// <see cref="IsRunning"/> property instead.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this 
        /// <see cref="EterraApplicationBase"/> instance is initialized 
        /// (see <see cref="IsInitialized"/>) and currently running 
        /// and regularily updating and redrawing itself (<c>true</c>) or if 
        /// it wasn't started (yet) or has been stopped already (<c>false</c>).
        /// </summary>
        public bool IsRunning => IsInitialized && !IsDisposed &&
            graphics.Graphics.IsRunning;

        /// <summary>
        /// A flag that - when set to <c>true</c> - causes the application to
        /// <see cref="Dispose"/> itself before the next 
        /// <see cref="Update(TimeSpan)"/>.
        /// </summary>
        private bool applicationTerminationRequested = false;

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="EterraApplicationBase"/> class.
        /// </summary>
        /// <remarks>
        /// Please note that the units of the application
        /// (<see cref="Graphics"/>, <see cref="Controls"/>, 
        /// <see cref="Resources"/> and <see cref="Sound"/>) can only be
        /// accessed when the application was started with 
        /// <see cref="Run(IPlatformProvider)"/> or
        /// <see cref="Run(IPlatformProvider, IFileSystem)"/>.
        /// Any initialisation which involves these units can be done in the
        /// <see cref="Load"/> method.
        /// </remarks>
        protected EterraApplicationBase() { }

        /// <summary>
        /// Initializes the application after the platform components
        /// (e.g. <see cref="Graphics"/>, <see cref="Sound"/> etc.) have been
        /// successfully initialized (and <see cref="IsInitialized"/> is 
        /// <c>true</c>).
        /// This method is called after the 
        /// <see cref="Run(IPlatformProvider)"/> (or 
        /// <see cref="Run(IPlatformProvider, IFileSystem)"/>) method.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsInitialized"/> is <c>false</c> 
        /// or when <see cref="IsRunning"/> is <c>true</c>.
        /// </exception>
        protected abstract void Load();

        /// <summary>
        /// Redraws the application.
        /// </summary>
        /// <param name="delta">
        /// The time elapsed since the last <see cref="Redraw(TimeSpan)"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="delta"/> is less than
        /// <see cref="TimeSpan.Zero"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsInitialized"/> or
        /// <see cref="IsRunning"/> is <c>false</c>.
        /// </exception>
        protected abstract void Redraw(TimeSpan delta);

        /// <summary>
        /// Updates the application logic.
        /// </summary>
        /// <param name="delta">
        /// The time elapsed since the last <see cref="Update(TimeSpan)"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="delta"/> is less than
        /// <see cref="TimeSpan.Zero"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsInitialized"/> or
        /// <see cref="IsRunning"/> is <c>false</c>.
        /// </exception>
        protected abstract void Update(TimeSpan delta);

        /// <summary>
        /// Destroys the application and unloads/disposes all resources.
        /// This method is called during <see cref="Close"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsInitialized"/> or 
        /// <see cref="IsRunning"/> is <c>false</c>.
        /// </exception>
        protected abstract void Unload();

        /// <summary>
        /// Starts running current <see cref="EterraApplicationBase"/> instance
        /// in the current thread.
        /// The <see cref="FileSystem.ProgramData"/> file system will be used.
        /// </summary>
        /// <param name="platformProvider">
        /// The <see cref="IPlatformProvider"/> instance, which will be used
        /// to initialize the platform (and all components for graphics, 
        /// sounds etc.) for this application instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="platformProvider"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsRunning"/> is <c>true</c>.
        /// </exception>
        public void Run(IPlatformProvider platformProvider)
        {
            Run(platformProvider, FileSystem.ProgramData);
        }

        /// <summary>
        /// Starts running current <see cref="EterraApplicationBase"/> instance
        /// in the current thread.
        /// </summary>
        /// <param name="platformProvider">
        /// The <see cref="IPlatformProvider"/> instance, which will be used
        /// to initialize the platform (and all components for graphics, 
        /// sounds etc.) for this application instance.
        /// </param>
        /// <param name="fileSystem">
        /// The <see cref="IFileSystem"/> instance, which will provide 
        /// access to the (resource) files available to the application.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="platformProvider"/> or
        /// <paramref name="fileSystem"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsRunning"/> is <c>true</c>.
        /// </exception>
        /// <exception cref="ApplicationException">
        /// Is thrown when the initialisation of the specified
        /// <paramref name="platformProvider"/> failed.
        /// </exception>
        public void Run(IPlatformProvider platformProvider,
            IFileSystem fileSystem)
        {
            if (platformProvider == null)
                throw new ArgumentNullException(nameof(platformProvider));
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            if (IsRunning) throw new InvalidOperationException("The " +
                "application is already running.");
            else if (IsDisposed) throw new InvalidOperationException("The " +
                "application was closed/disposed before and can no longer " +
                "be used.");
            else
            {
                Log.Information("Initializing application platform...");

                DateTime start = DateTime.Now;

                PlatformComponents platform = null;
                try { platform = platformProvider.Initialize(); }
                catch (Exception exc)
                {
                    throw new ApplicationException("The initialisation of " +
                        "the application platform failed.", exc);
                }
                if (platform == null) throw new ApplicationException("The " +
                    "specified platform provider returned null.");

                graphics = new GraphicsManager(platform.Graphics);

                graphics.Graphics.Initialized += OnInitialized;
                graphics.Graphics.Closing += OnClosed;
                graphics.Graphics.Redraw += OnRedraw;
                graphics.Graphics.Update += OnUpdate;

                controls = new ControlsManager(platform.Controls ??
                    new ControlsDummy());
                sound = new SoundManager(platform.Sound ?? new SoundDummy());
                resources = new ResourceManager(fileSystem,
                    platform.ResourceFormatHandlers, graphics.Graphics,
                    sound.Sound);

                graphics.Title = Assembly.GetCallingAssembly().GetName().Name
                    + " Application Launcher";

                IsInitialized = true;

                Log.Information("Initialisation of application platform " +
                    "completed in "
                    + (DateTime.Now - start).TotalSeconds.ToString("F3") +
                    " seconds.");

                graphics.Graphics.Run();
            }
        }

        /// <summary>
        /// Terminates the application after the current
        /// <see cref="Update(TimeSpan)"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsRunning"/> is <c>false</c>.
        /// </exception>
        protected void Close()
        {
            if (!IsRunning) throw new InvalidOperationException("The " +
                "platform is not running and can't be closed.");
            else applicationTerminationRequested = true;
        }

        private void OnInitialized(object source, EventArgs args)
        {
            Log.Information("Loading main application...");

            DateTime start = DateTime.Now;
            try
            {
                Load();
                Log.Information("Loading of main application completed " +
                    "in " + (DateTime.Now - start).TotalSeconds.ToString("F3") 
                    + " seconds.");
            }
            catch (Exception exc)
            {
                Log.Error(new ApplicationException("The main application " +
                    "couldn't be loaded.", exc));
                Close();
            }
        }

        private void OnClosed(object source, EventArgs args)
        {
            Log.Information("Unloading and terminating application...");
            try
            {
                Unload();
                Log.Information("Application unloaded and terminated.");
            }
            catch (Exception exc)
            {
                Log.Error(new ApplicationException("The application was " +
                    "terminated, but couldn't be unloaded properly.", exc));
            }
        }

        private void OnUpdate(IGraphics source, TimeSpan delta)
        {
            if (IsDisposed) return;

            //Catch close requests from previous updates (or redraws) and a
            //potential close from a failed initialisation.
            if (applicationTerminationRequested)
            {
                Dispose();
                return;
            }

            try { Controls.Update(delta); }
            catch (Exception exc)
            {
                Log.Error(new ApplicationException("The controller manager " +
                    "failed unexpectedly while updating itself.", exc));
                Close();
                return;
            }

            try { Update(delta); }
            catch (Exception exc)
            {
                Log.Error(new ApplicationException("The main application " +
                    "failed unexpectedly while updating itself.", exc));
                if (IsRunning) Close();
                return;
            }
        }

        private void OnRedraw(IGraphics source, TimeSpan delta)
        {
            if (IsDisposed || applicationTerminationRequested) return;

            DateTime eventStart = DateTime.Now;

            try { Redraw(delta); }
            catch (Exception exc)
            {
                Log.Error(new ApplicationException("The main application " +
                    "failed unexpectedly while redrawing itself.", exc));
                Close();
                return;
            }

            //Continue the task scheduler with the time remaining after the
            //redraw event invocation - but at least with 1ms, to prevent
            //inefficient update event subscribers to "disable" the scheduler.
            TimeSpan elapsed = DateTime.Now - eventStart;
            TimeSpan remaining =
                TimeSpan.FromSeconds(1 / TargetRedrawsPerSecond) - elapsed;
            if (remaining <= TimeSpan.Zero) remaining =
                    TimeSpan.FromMilliseconds(1);

            Resources.ContinueTasks(remaining);
        }

        /// <summary>
        /// Closes the application and releases all associated resources.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                graphics.Graphics.Close();
                graphics.Graphics.Initialized -= OnInitialized;
                graphics.Graphics.Closing -= OnClosed;
                graphics.Graphics.Redraw -= OnRedraw;
                graphics.Graphics.Update -= OnUpdate;
                graphics = null;

                sound = null;

                controls = null;

                resources.Dispose();
                resources = null;

                IsDisposed = true;
            }
        }
    }
}
