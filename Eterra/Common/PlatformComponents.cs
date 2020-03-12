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

using Eterra.Controls;
using Eterra.Graphics;
using Eterra.IO;
using Eterra.Sound;
using System;
using System.Collections.Generic;

namespace Eterra.Common
{
    /// <summary>
    /// Provides initialized and ready unit instances for usage in 
    /// an <see cref="EterraApp"/> instance.
    /// </summary>
    public class PlatformComponents
    {
        /// <summary>
        /// Gets the graphics unit.
        /// </summary>
        public IGraphics Graphics { get; }

        /// <summary>
        /// Gets the sound unit. Can be null.
        /// </summary>
        public ISound Sound { get; }

        /// <summary>
        /// Gets the controls unit. Can be null.
        /// </summary>
        public IControls Controls { get; }

        /// <summary>
        /// Gets the handlers for additional (non-native) resource file 
        /// formats. Can be null.
        /// </summary>
        public IEnumerable<IResourceFormatHandler> ResourceFormatHandlers
            { get; }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="PlatformComponents"/> class with a graphics unit only.
        /// </summary>
        /// <param name="graphics"></param>
        public PlatformComponents(IGraphics graphics)
            : this(graphics, null, null, null) { }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="PlatformComponents"/> class.
        /// </summary>
        /// <param name="graphics">The graphics unit.</param>
        /// <param name="sound">The sound unit. Can be null.</param>
        /// <param name="controls">The controls unit. Can be null.</param>
        /// <param name="resourceFormatHandlers">
        /// The handlers for additional (non-native) resource file formats. 
        /// Can be null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="graphics"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="graphics"/> is already running.
        /// </exception>
        public PlatformComponents(IGraphics graphics, ISound sound,
            IControls controls, 
            IEnumerable<IResourceFormatHandler> resourceFormatHandlers)
        {
            Graphics = graphics ??
                throw new ArgumentNullException(nameof(graphics));
            if (graphics.IsRunning)
                throw new ArgumentException("The specified graphics unit " +
                    "is already running and can't be used.");

            Sound = sound;
            Controls = controls;
            ResourceFormatHandlers = resourceFormatHandlers;
        }
    }
}
