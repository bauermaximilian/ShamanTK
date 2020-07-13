using Eterra.Common;
using System;

namespace Eterra.Graphics
{
    /// <summary>
    /// Provides parameters for configuring a fog effect.
    /// </summary>
    public struct Fog
    {
        /// <summary>
        /// Gets a <see cref="Fog"/> instance that defines that no fog should
        /// be used.
        /// </summary>
        public static Fog Disabled { get; } = new Fog();

        /// <summary>
        /// Gets a value indicating whether the current <see cref="Fog"/>
        /// instance defines an active fog effect (<c>false</c>) or specifies
        /// that no fog effect should be used (<c>true</c>).
        /// </summary>
        public bool IsDisabled => OnsetDistance == 0 && FalloffLength == 0;

        /// <summary>
        /// Gets the distance from the <see cref="Camera"/>, from which the
        /// fog starts to influence the drawn objects.
        /// </summary>
        public float OnsetDistance { get; }

        /// <summary>
        /// Gets the length of the falloff, which - in combination with the 
        /// <see cref="OnsetDistance"/> - defines the intensity of the fog.
        /// </summary>
        /// <remarks>
        /// At the beginning of the falloff length (the 
        /// <see cref="OnsetDistance"/>), the fog effect starts and increases
        /// linear until the end of the falloff (<see cref="OnsetDistance"/>
        /// plus the <see cref="FalloffLength"/>) where the fog effect is the
        /// strongest (and the fragment colors are completely "replaced" by
        /// the specified <see cref="Color"/>).
        /// </remarks>
        public float FalloffLength { get; }

        /// <summary>
        /// Gets the color of the <see cref="Fog"/>, with which the fragment 
        /// color is gradually replaced when it's inside the 
        /// "<see cref="Fog"/> zone".
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// Gets a value indicating whether the distance from the 
        /// <see cref="Camera"/> should be calculated only using the X and Z
        /// axis (<c>true</c>) or whether the Y axis should be included in the
        /// calculation (<c>false</c>).
        /// </summary>
        public bool IgnoreYAxis { get; }

        /// <summary>
        /// Initializes a new <see cref="Fog"/> instance.
        /// </summary>
        /// <param name="onsetDistance">
        /// The distance from the <see cref="Camera"/> from which the fog will
        /// come into effect.
        /// </param>
        /// <param name="falloffLength">
        /// The length from the <paramref name="onsetDistance"/> that defines
        /// how strong the fog influences the drawn fragments.
        /// </param>
        /// <param name="color">
        /// The color, to which the fragments fade to when inside the 
        /// "fog zone".
        /// </param>
        /// <param name="ignoreYAxis">
        /// <c>true</c> to only use the X and Z axis for calculation the 
        /// distance from the <see cref="Camera"/>, <c>false</c> to use all
        /// axis.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="onsetDistance"/> or
        /// <paramref name="falloffLength"/> are less than 0.
        /// </exception>
        /// <remarks>
        /// An <paramref name="onsetDistance"/> and 
        /// <paramref name="falloffLength"/> of 0 will create a 
        /// <see cref="Fog"/> instance with <see cref="IsDisabled"/> 
        /// evaluating to <c>true</c>. To specify that no fog effect
        /// should be used, the value provided by <see cref="Disabled"/> 
        /// can be used.
        /// </remarks>
        public Fog(float onsetDistance, float falloffLength, Color color,
            bool ignoreYAxis)
        {
            if (onsetDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(onsetDistance));
            if (falloffLength < 0)
                throw new ArgumentOutOfRangeException(nameof(falloffLength));

            OnsetDistance = onsetDistance;
            FalloffLength = falloffLength;
            Color = color;
            IgnoreYAxis = ignoreYAxis;
        }
    }
}
