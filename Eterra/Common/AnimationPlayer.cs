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
    /// Provides the base class from which classes that can produce a fluent,
    /// controllable animation using the data from a 
    /// <see cref="Timeline"/> are derived.
    /// </summary>
    public class AnimationPlayer : IAnimation
    {
        /// <summary>
        /// Gets or sets a value indicating whether the animation is currently 
        /// being played back and the <see cref="Position"/> is updated when 
        /// the <see cref="Update(TimeSpan)"/> method is called regularly
        /// (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsPlaying { get; set; }

        /// <summary>
        /// Gets or sets the current playback position.
        /// </summary>
        public TimeSpan Position
        {
            get => position;
            set
            {
                position = value;
                resetPositionToStartOnPlaybackStart = false;
            }
        }
        private TimeSpan position;

        /// <summary>
        /// Gets the timeline which is used by this 
        /// <see cref="AnimationPlayer"/>.
        /// </summary>
        protected internal Timeline Timeline { get; }

        /// <summary>
        /// Gets a sorted list of all markers.
        /// </summary>
        public IList<Marker> Markers => Timeline.Markers;

        /// <summary>
        /// Gets a collection of the identifiers of all markers.
        /// </summary>
        public ICollection<string> MarkerIdentifiers => 
            Timeline.MarkerIdentifiers;

        /// <summary>
        /// Gets a collection of all animation layers.
        /// </summary>
        public ICollection<IAnimationLayer> Layers => layers.Values;

        /// <summary>
        /// Gets a collection of the identifiers of all animation layers.
        /// </summary>
        public ICollection<string> LayerIdentifiers => layers.Keys;

        /// <summary>
        /// Gets or sets the start of the playback.
        /// </summary>
        public TimeSpan PlaybackStart { get; set; }

        /// <summary>
        /// Gets or sets the marker, which defines the start of the playback,
        /// or null if the start of the playback isn't defined by any marker.
        /// Setting this property with an existing, non-null marker identifier
        /// will update <see cref="PlaybackStart"/> with the position of the
        /// specified marker. Setting this value to null or a non-existing
        /// marker identifier will have no effect.
        /// </summary>
        public string PlaybackStartMarker
        {
            get => playbackStartMarker;
            set
            {
                if (value != null && MarkerIdentifiers.Contains(value))
                    PlaybackStart = Timeline.GetMarker(value).Position;
                playbackStartMarker = value;
            }
        }
        private string playbackStartMarker = null;

        /// <summary>
        /// Gets or sets the end of the playback.
        /// </summary>
        public TimeSpan PlaybackEnd { get; set; }

        /// <summary>
        /// Gets or sets the marker, which defines the end of the playback,
        /// or null if the end of the playback isn't defined by any marker.
        /// Setting this property with an existing, non-null marker identifier
        /// will update <see cref="PlaybackEnd"/> with the position of the
        /// specified marker. Setting this value to null or a non-existing
        /// marker identifier will have no effect.
        /// </summary>
        public string PlaybackEndMarker
        {
            get => playbackEndMarker;
            set
            {
                if (value != null && MarkerIdentifiers.Contains(value))
                    PlaybackEnd = Timeline.GetMarker(value).Position;
                playbackEndMarker = value;
            }
        }
        private string playbackEndMarker = null;

        /// <summary>
        /// Gets or sets a value indicating whether the playback should be
        /// restarted at <see cref="PlaybackStart"/> after the 
        /// <see cref="Position"/> has reached <see cref="PlaybackEnd"/>
        /// (<c>true</c>) or if the playback should just be stopped at
        /// <see cref="PlaybackEnd"/> (<c>false</c>).
        /// </summary>
        public bool PlaybackLoop { get; set; }

        private readonly Dictionary<string, IAnimationLayer> layers = 
            new Dictionary<string, IAnimationLayer>();

        private bool resetPositionToStartOnPlaybackStart = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimationPlayer"/> 
        /// base class.
        /// </summary>
        /// <param name="timeline">
        /// The timeline, which is used by this <see cref="AnimationPlayer"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thown when <paramref name="timeline"/> is null.
        /// </exception>
        public AnimationPlayer(Timeline timeline)
        {
            Timeline = timeline ?? 
                throw new ArgumentNullException(nameof(timeline));
            position = PlaybackStart = Timeline.Start;
            PlaybackEnd = Timeline.End;

            foreach (TimelineLayer timelineLayer in timeline.Layers)
            {
                //It is assumed that the timeline.Layers collection has no
                //duplicates (due to the constructor of the TimelineLayer
                //class, which would throw an exception in case layers with 
                //duplicate identifiers were found).

                Type animationPlayerLayerType = 
                    typeof(AnimationPlayerLayer<>).MakeGenericType(
                        timelineLayer.ValueType);
                IAnimationLayer animationPlayerLayer = 
                    (IAnimationLayer)Activator.CreateInstance(
                        animationPlayerLayerType, this, 
                        timelineLayer.Identifier);

                layers[timelineLayer.Identifier] = animationPlayerLayer;
            }
        }

        /// <summary>
        /// Starts the animation playback at the current 
        /// <see cref="Position"/>.
        /// </summary>
        public void Play()
        {
            if (resetPositionToStartOnPlaybackStart) Position = PlaybackStart;
            IsPlaying = true;
        }

        /// <summary>
        /// Starts the animation playback.
        /// </summary>
        /// <param name="rewind">
        /// <c>true</c> to rewind to <see cref="PlaybackStart"/> and start
        /// the playback from there, <c>false</c> to just continue the
        /// playback at the current <see cref="Position"/> (or do nothing
        /// if the animation is playing already).
        /// </param>
        public void Play(bool rewind)
        {
            resetPositionToStartOnPlaybackStart = rewind;
            Play();
        }

        /// <summary>
        /// Pauses the animation playback at the current position.
        /// </summary>
        public void Pause()
        {
            IsPlaying = false;
        }

        /// <summary>
        /// Stops the animation playback and resets the current 
        /// <see cref="Position"/> to <see cref="PlaybackStart"/>.
        /// </summary>
        public void Stop()
        {
            Position = Timeline.Start;
            IsPlaying = false;
        }

        /// <summary>
        /// Updates the <see cref="Position"/> of the current animation player.
        /// </summary>
        /// <param name="delta">
        /// The amount of time which elapsed since the last invocation of this
        /// method, which will be added to <see cref="Position"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="delta"/> is negative.
        /// </exception>
        public virtual void Update(TimeSpan delta)
        {
            if (delta < TimeSpan.Zero)
                throw new ArgumentException("A negative value for time " +
                    "delta is not supported.");

            if (PlaybackEnd < PlaybackStart) IsPlaying = false;

            if (IsPlaying)
            {                
                TimeSpan newPosition = Position + delta;
                if (newPosition > PlaybackEnd)
                {
                    if (PlaybackLoop)
                        Position = PlaybackStart + (newPosition - PlaybackEnd);
                    else
                    {
                        Position = PlaybackEnd;
                        IsPlaying = false;
                    }
                }
                else Position = newPosition;
            }
        }

        /// <summary>
        /// Gets a <see cref="AnimationPlayerLayer{T}"/> from the current
        /// <see cref="AnimationPlayer"/>, which provides access to the 
        /// animated data of the <see cref="TimelineLayer{T}"/> with the
        /// same identifier in the associated <see cref="Timeline"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The value type of the keyframes on the associated 
        /// <see cref="TimelineLayer{T}"/>.
        /// </typeparam>
        /// <param name="identifier">
        /// The identifier of the requested <see cref="TimelineLayer{T}"/>.
        /// </param>
        /// <returns>
        /// The requested <see cref="IAnimationLayer{T}"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no player layer with the specified
        /// <paramref name="identifier"/> was found.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// Is thrown when the specified <typeparamref name="T"/> doesn't
        /// match the type of the requested 
        /// <see cref="IAnimationLayer{T}"/> (and the associated 
        /// <see cref="TimelineLayer{T}"/>).
        /// </exception>
        public IAnimationLayer<T> GetLayer<T>(string identifier)
            where T : unmanaged
        {
            IAnimationLayer layer = GetLayer(identifier);
            try { return (IAnimationLayer<T>)layer; }
            catch (InvalidCastException)
            {
                throw new InvalidCastException("Couldn't convert the " +
                    "player layer with the value type \"" 
                    + layer.ValueType.Name + "\" to a player layer with the " +
                    "(requested) value type \"" + typeof(T).Name + "\".");
            }
        }

        /// <summary>
        /// Gets a <see cref="AnimationPlayerLayer{T}"/> from the current
        /// <see cref="AnimationPlayer"/>, which provides access to the 
        /// animated data of the <see cref="TimelineLayer{T}"/> with the
        /// same identifier in the associated <see cref="Timeline"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="identifier">
        /// The identifier of the requested <see cref="TimelineLayer{T}"/>.
        /// </param>
        /// <param name="layer">
        /// The requested <see cref="IAnimationLayer{T}"/> instance or null if
        /// no layer with the specified <paramref name="identifier"/> and
        /// type <typeparamref name="T"/> was found.
        /// </param>
        /// <returns>
        /// <c>true</c> if a <see cref="IAnimationLayer{T}"/> with the specifed
        /// <paramref name="identifier"/> and type <typeparamref name="T"/> 
        /// was found and stored into the <paramref name="layer"/> parameter,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        public bool TryGetLayer<T>(string identifier,
            out IAnimationLayer<T> layer)
            where T : unmanaged
        {
            try { layer = GetLayer<T>(identifier); return true; }
            catch (ArgumentNullException) { throw; }
            catch
            {
                layer = null;
                return false;
            }
        }

        /// <summary>
        /// Gets an <see cref="IAnimationLayer"/> instance from the current
        /// <see cref="AnimationPlayer"/>, which provides access to the 
        /// animated data of the <see cref="TimelineLayer"/> with the
        /// same identifier in the associated <see cref="Timeline"/>
        /// (when casted to the <see cref="AnimationPlayerLayer{T}"/> with the
        /// correct type).
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the requested <see cref="TimelineLayer"/>.
        /// </param>
        /// <returns>
        /// The requested <see cref="IAnimationLayer"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no player layer with the specified
        /// <paramref name="identifier"/> was found.
        /// </exception>
        public IAnimationLayer GetLayer(string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (layers.TryGetValue(identifier, out IAnimationLayer layer))
                return layer;
            else throw new ArgumentException("The current animation player " +
                "doesn't contain a layer with the specified identifier.");
        }
    }
}
