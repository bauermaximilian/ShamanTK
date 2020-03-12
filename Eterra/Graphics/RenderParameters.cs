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
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Eterra.Graphics
{
    /// <summary>
    /// Provides parameters to configure the behaviour of a 
    /// <see cref="IRenderContext"/>.
    /// </summary>
    public class RenderParameters
    {
        /// <summary>
        /// Provides the parameters to configure stereoscopic rendering.
        /// </summary>
        public class StereoscopyParameters
        {
            /// <summary>
            /// Gets or sets a value indicating whether stereoscopic rendering 
            /// is enabled (<c>true</c>) or not (<c>false</c>).
            /// The default value is <c>false</c>.
            /// </summary>
            public bool Enabled { get; set; } = false;

            /// <summary>
            /// Gets or sets a value greater than/equal to 0, that defines the 
            /// interpupilliary distance (the distance between the two "eyes").
            /// The default value is 0.065.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException">
            /// Is thrown when the assigned value is less than 0.
            /// </exception>
            public float InterpupilliaryDistance
            {
                get => interpupilliaryDistance;
                set
                {
                    if (value < 0)
                        throw new ArgumentOutOfRangeException();
                    else interpupilliaryDistance = value;
                }
            }
            private float interpupilliaryDistance = 0.065f;

            /// <summary>
            /// Gets or sets the transformation matrix for the left eye 
            /// viewport. The coordinate system provided by
            /// <see cref="ProjectionMode.OrthographicAbsolute"/> is 
            /// applicable. The default value scales the viewport to a half of
            /// the screen width and moves it to the left side of the screen.
            /// </summary>
            public Matrix4x4 ViewportTransformationLeft { get; set; }
                = MathHelper.CreateTransformation(0, 0, 0, 0.5f, 1, 1);

            /// <summary>
            /// Gets or sets the transformation matrix for the right eye 
            /// viewport. The coordinate system provided by
            /// <see cref="ProjectionMode.OrthographicAbsolute"/> is 
            /// applicable. The default value scales the viewport to a half of
            /// the screen width and moves it to the right side of the screen.
            /// </summary>
            public Matrix4x4 ViewportTransformationRight { get; set; }
                = MathHelper.CreateTransformation(0.5f, 0, 0, 0.5f, 1, 1);

            /// <summary>
            /// Gets or sets a value between -1.0 and 1.0 that specifies the
            /// warp of the viewport to the outside (positive values) or the
            /// inside (negative values).
            /// The default value is 0 (= "no warp").
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException">
            /// Is thrown when the assigned value is less than -1.0 or greater
            /// than 1.0.
            /// </exception>
            public float ViewportWarp
            {
                get => viewportWarp;
                set
                {
                    if (value < -1.0f || value > 1.0f)
                        throw new ArgumentOutOfRangeException();
                    else viewportWarp = value;
                }
            }
            private float viewportWarp = 0;
        }

        /// <summary>
        /// Provides the parameters to configure a fog effect.
        /// </summary>
        public class FogParameters
        {
            /// <summary>
            /// Gets or sets a value indicating whether the fog effect is
            /// enabled (<c>true</c>) or not (<c>false</c>).
            /// The default value is <c>false</c>.
            /// </summary>
            public bool Enabled { get; set; } = false;

            /// <summary>
            /// Gets or sets a value greater than/equal to 0, which defines the
            /// distance from the camera where the fog will start.
            /// The default value is 50.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException">
            /// Is thrown when the assigned value is less than 0.
            /// </exception>
            public float Start
            {
                get => start;
                set
                {
                    if (value < 0)
                        throw new ArgumentOutOfRangeException();
                    else start = value;
                }
            }
            private float start = 50;

            /// <summary>
            /// Gets or sets a value greater than/equal to 0, which defines the
            /// length between the start of the fog effect and the point from 
            /// which a rendered object will no longer be visible because it's 
            /// completely disappeared in the fog.
            /// The default value is 10.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException">
            /// Is thrown when the assigned value is less than 0.
            /// </exception>
            public float OnsetLength
            {
                get => onsetLength;
                set
                {
                    if (value < 0)
                        throw new ArgumentOutOfRangeException();
                    else onsetLength = value;
                }
            }
            private float onsetLength = 10;

            /// <summary>
            /// Gets or sets the color of the fog.
            /// </summary>
            public Color Color { get; set; } = Color.Black;
        }

        /// <summary>
        /// Provides the parameters to configure graphic filters.
        /// </summary>
        public class GraphicsFilterParameters
        {
            /// <summary>
            /// Gets or sets a value indicating whether graphic filters are
            /// enabled (<c>true</c>) or not (<c>false</c>).
            /// The default value is <c>false</c>.
            /// </summary>
            public bool Enabled { get; set; } = false;

            /// <summary>
            /// Gets the default value of the <see cref="ColorShades"/> 
            /// property, defining the full color spectrum.
            /// </summary>
            public static Vector3 ColorShadesFull { get; } =
                new Vector3(byte.MaxValue, byte.MaxValue, byte.MaxValue);

            /// <summary>
            /// Defines the minimum value for 
            /// <see cref="ResolutionScaleFactorMinimum"/>.
            /// </summary>
            public const float ResolutionScaleFactorMinimum = 1 / 64f;

            /// <summary>
            /// Gets or sets a value between 
            /// <see cref="ResolutionScaleFactorMinimum"/> and 1.0, which is
            /// multiplied by the size of the screen or window to get the
            /// resolution of the main frame buffer (render target). A lower 
            /// value means faster render times, but a more blurred image. 
            /// The default value is 1.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException">
            /// Is thrown when the assigned value is greater than 1.0 or 
            /// less than <see cref="ResolutionScaleFactorMinimum"/>.
            /// </exception>
            public float ResolutionScaleFactor
            {
                get => resolutionScaleFactor;
                set
                {
                    if (value < ResolutionScaleFactorMinimum ||
                        value > 1.0f)
                        throw new ArgumentOutOfRangeException();
                    else resolutionScaleFactor = value;
                }
            }
            private float resolutionScaleFactor = 1;

            /// <summary>
            /// Gets or sets the <see cref="TextureFilter"/> which is used to
            /// scale the main frame buffer (render target) from the scaled 
            /// size (using <see cref="ResolutionScaleFactor"/>) to the size
            /// of the screen or window. The default value is
            /// <see cref="TextureFilter.Linear"/>.
            /// </summary>
            /// <exception cref="ArgumentException">
            /// Is thrown when the assigned value is no valid element of
            /// <see cref="TextureFilter"/>.
            /// </exception>
            public TextureFilter ResolutionScaleFilter
            {
                get => resolutionScaleFilter;
                set
                {
                    if (!Enum.IsDefined(typeof(TextureFilter), value))
                        throw new ArgumentException("The specified texture " +
                            "filter is invalid.");
                    else resolutionScaleFilter = value;
                }
            }
            private TextureFilter resolutionScaleFilter = TextureFilter.Linear;

            /// <summary>
            /// Gets or sets a value which specifies how many shades each 
            /// color component of each pixel will be reduced to. This can be
            /// used to create a vintage effect (e.g. a value of <c>(8|8|4)</c>
            /// will reduce the image to 256 colors). The default value will
            /// not reduce the colors in any way. Each component must be
            /// greater than 0 and less than/equal to 
            /// <see cref="byte.MaxValue"/>.
            /// </summary>
            /// <exception cref="ArgumentException">
            /// Is thrown when one of the components of the assigned value
            /// contains a value which is less than 1 or greater than 
            /// <see cref="byte.MaxValue"/>.
            /// </exception>
            public Vector3 ColorShades
            {
                get => colorShades;
                set
                {
                    if (value.X < 1 || value.X > byte.MaxValue ||
                        value.Y < 1 || value.Y > byte.MaxValue ||
                        value.Z < 1 || value.Z > byte.MaxValue)
                        throw new ArgumentException("The specified vector " +
                            "was no valid color shade definition.");

                    colorShades = value;
                }
            }
            private Vector3 colorShades = ColorShadesFull;
        }

        /// <summary>
        /// Provides the parameters to configure lighting.
        /// </summary>
        public class LightingParameters : HashSet<Light>
        {
            /// <summary>
            /// Gets or sets a value indicating whether illumination is
            /// enabled (<c>true</c>) or not (<c>false</c>).
            /// The default value is <c>false</c>.
            /// </summary>
            public bool Enabled { get; set; } = false;
        }

        /// <summary>
        /// Gets a collection of parameters and <see cref="Common.Light"/> 
        /// instances, which specify if and how objects drawn to a 
        /// <see cref="IRenderContext"/> will be illuminated (if supported by the 
        /// <see cref="IRenderContext"/> implementation created with these 
        /// parameters).
        /// </summary>
        public LightingParameters Lighting { get; }
            = new LightingParameters();

        /// <summary>
        /// Gets a collection of parameters, which are used to configure 
        /// stereoscopic rendering (if supported by the <see cref="IRenderContext"/> 
        /// implementation created with these parameters).
        /// </summary>
        public StereoscopyParameters Stereoscopy { get; }
            = new StereoscopyParameters();

        /// <summary>
        /// Gets a collection of parameters, which are used to configure the
        /// fog effect (if supported by the <see cref="IRenderContext"/> 
        /// implementation created with these parameters).
        /// </summary>
        public FogParameters Fog { get; } = new FogParameters();

        /// <summary>
        /// Gets a collection of parameters, which configure which graphic
        /// filters are applied for post-processing to the final image 
        /// (if supported by the <see cref="IRenderContext"/> implementation created
        /// with these parameters).
        /// </summary>
        public GraphicsFilterParameters Filters { get; } 
            = new GraphicsFilterParameters();

        /// <summary>
        /// Gets or sets the camera, which defines the projection mode and 
        /// viewer position, from which everything is rendered with the
        /// <see cref="IRenderContext"/> created using these parameters.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when the assigned value is null.
        /// </exception>
        public Camera Camera
        {
            get => camera;
            set
            {
                if (value == null) throw new ArgumentNullException();
                else camera = value;
            }
        }
        private Camera camera = new Camera();

        /// <summary>
        /// Gets or sets a value indicating whether backface culling, which 
        /// disables the drawing of the back side of faces and saves 
        /// rendering time, is enabled (<c>true</c>) or not (<c>false</c>).
        /// The default value is <c>false</c>.
        /// </summary>
        public bool BackfaceCullingEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether all rendering is done
        /// in wireframe mode, where the color of the wireframe is defined
        /// by the current value of <see cref="IRenderContext.Color"/> 
        /// (<c>true</c>) or not (<c>false</c>).
        /// The default value is <c>false</c>.
        /// </summary>
        public bool WireframeRenderingEnabled { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderParameters"/>
        /// class.
        /// </summary>
        public RenderParameters() { }
    }
}
