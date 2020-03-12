/*
 * Eterra Framework Platforms
 * Eterra platform providers for various operating systems and devices.
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Eterra.Common;
using Eterra.Controls;
using Eterra.Graphics;
using Eterra.IO;
using Eterra.Sound;
using System;
using System.Collections.Generic;

namespace Eterra.Platforms.Windows
{
    /// <summary>
    /// Provides functionality to initialize a <see cref="EterraApp"/>.
    /// </summary>
    public class PlatformProvider : IPlatformProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformProvider"/> 
        /// class.
        /// </summary>
        public PlatformProvider() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformComponents"/>
        /// class. This method matches the signature of the
        /// <see cref="PlatformProvider"/> delegate and can be used whereever
        /// a <see cref="PlatformProvider"/> delegate is required as parameter.
        /// </summary>
        /// <param name="configuration">
        /// The configuration for the individual components in 
        /// the new <see cref="PlatformComponents"/> interface.
        /// Invalid or unsupported parameters are ignored and replaced with
        /// their platform-specific defaults.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="PlatformComponents"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="configuration"/> is null.
        /// </exception>
        /// <exception cref="SystemException">
        /// Is thrown when one of the platform components couldn't be 
        /// initialized. This is often caused by missing OpenAL libraries, 
        /// which (at the time of writing this comment [2019-07-25]), 
        /// can be found under https://www.openal.org/downloads/oalinst.zip.
        /// For Unix-based systems, these libraries are usually available by
        /// default.
        /// </exception>
        /// <remarks>
        /// This method provides provides a complete 
        /// <see cref="PlatformComponents"/> instance. The included
        /// <see cref="ResourceManager"/> only supports native file formats.
        /// </remarks>
        public PlatformComponents Initialize()
        {
            IGraphics graphics; ISound sound; IControls controls;
            List<IResourceFormatHandler> resourceFormatHandlers = 
                new List<IResourceFormatHandler>();

            try
            {
                graphics = new Graphics.Graphics();
                sound = new Sound.Sound();
                controls = new Controls.Controls((Graphics.Graphics)graphics);

                resourceFormatHandlers.Add(new IO.FontFormatHandler());
                resourceFormatHandlers.Add(new IO.ImageFormatHandler());
                resourceFormatHandlers.Add(new IO.SoundFormatHandler());
                resourceFormatHandlers.Add(new IO.AssimpFormatHandler());
            }
            catch (Exception exc)
            {
                throw new SystemException("The OpenTK context couldn't be " +
                    "initialized.", exc);
            }

            return new PlatformComponents(graphics, sound, controls, 
                resourceFormatHandlers);
        }
    }
}
