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
    public abstract class EterraApplicationBase
    {
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
        /// was successfully initialized and started.
        /// </exception>
        protected ResourceManager Resources
        {
            get
            {
                return resources ?? throw new InvalidOperationException(
                    "The resource unit was accessed before the application " +
                    "was successfully initialized.");
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
        /// was successfully initialized and started.
        /// </exception>
        protected ControlsManager Controls
        {
            get
            {
                return controls ?? throw new InvalidOperationException(
                    "The controls unit was accessed before the application " +
                    "was successfully initialized.");
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
        /// was successfully initialized and started.
        /// </exception>
        protected GraphicsManager Graphics
        {
            get
            {
                return graphics ?? throw new InvalidOperationException(
                    "The graphics unit was accessed before the application " +
                    "was successfully initialized.");
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
        /// was successfully initialized and started.
        /// </exception>
        protected SoundManager Sound
        {
            get
            {
                return sound ?? throw new InvalidOperationException(
                    "The sound unit was accessed before the application " +
                    "was successfully initialized.");
            }
        }
        private SoundManager sound;

        /// <summary>
        /// Gets a value indicating whether this 
        /// <see cref="EterraApplicationBase"/> instance is initialized and the 
        /// <see cref="Graphics"/>, <see cref="Sound"/>, <see cref="Controls"/>
        /// and <see cref="Resources"/> properties are accessible and non-null
        /// (<c>true</c>) or if the application hasn't been started yet
        /// or has been stopped already (<c>false</c>).
        /// </summary>
        public bool IsInitialized => graphics != null && sound != null &&
            controls != null && resources != null;

        /// <summary>
        /// Gets a value indicating whether this 
        /// <see cref="EterraApplicationBase"/> instance is initialized 
        /// (see <see cref="IsInitialized"/>) and currently running 
        /// and regularily updating and redrawing itself (<c>true</c>) or if 
        /// it wasn't started (yet) or has been stopped already (<c>false</c>).
        /// </summary>
        public bool IsRunning => IsInitialized &&
            graphics.Graphics.IsRunning;

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

                Graphics.Graphics.Initialized += OnInitialized;
                Graphics.Graphics.Closing += OnClosed;
                Graphics.Graphics.Redraw += OnRedraw;
                Graphics.Graphics.Update += OnUpdate;

                controls = new ControlsManager(platform.Controls ??
                    new ControlsDummy());
                sound = new SoundManager(platform.Sound ?? new SoundDummy());
                resources = new ResourceManager(fileSystem,
                    platform.ResourceFormatHandlers, Graphics.Graphics,
                    Sound.Sound);

                Graphics.Title = Assembly.GetCallingAssembly().GetName().Name
                    + " Application Launcher";

                Log.Information("Initialisation of application platform " +
                    "completed in "
                    + (DateTime.Now - start).TotalSeconds.ToString("F3") +
                    " seconds.");

                graphics.Graphics.Run();
            }
        }

        /// <summary>
        /// Stops the current platform and the 
        /// <see cref="Update"/>/<see cref="Redraw"/> cycle.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsRunning"/> is <c>false</c>.
        /// </exception>
        protected void Close()
        {
            if (!IsRunning) throw new InvalidOperationException("The " +
                "platform is not running.");
            else
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
            }
        }

        private void OnInitialized(object source, EventArgs args)
        {
            Log.Information("Loading main application...");

            DateTime start = DateTime.Now;
            try { Load(); }
            catch (Exception exc)
            {
                Log.Error(new ApplicationException("The main application " +
                    "couldn't be loaded.", exc));
                Close();
                return;
            }

            Log.Information("Loading of main application completed " +
                "in " + (DateTime.Now - start).TotalSeconds.ToString("F3") +
                " seconds.");
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
                Close();
                return;
            }
        }

        private void OnRedraw(IGraphics source, TimeSpan delta)
        {
            DateTime eventStart = DateTime.Now;

            try { Redraw(delta); }
            catch (Exception exc)
            {
                Log.Error(new ApplicationException("The main application " +
                    "failed unexpectedly while redrawing itself.", exc));
                Close();
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
    }
}
