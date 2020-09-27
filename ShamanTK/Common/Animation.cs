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
using System.Collections;
using System.Collections.Generic;

namespace ShamanTK.Common
{
    /// <summary>
    /// Provides the base class from which classes that can produce a fluent,
    /// controllable animation using the data from a 
    /// <see cref="sourceTimeline"/> are derived.
    /// </summary>
    public class Animation : IEnumerable<AnimationLayer>
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
        /// Gets a sorted list of all markers.
        /// </summary>
        public ICollection<Marker> Markers => sourceTimeline.Markers;

        /// <summary>
        /// Gets or sets the start of the playback.
        /// </summary>
        public TimeSpan PlaybackStart { get; set; }

        /// <summary>
        /// Gets or sets the marker identifier, which defines the end of the 
        /// playback, or null.
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
                if (value != null && sourceTimeline.TryGetMarker(value, 
                    out Marker marker)) PlaybackStart = marker.Position;

                playbackStartMarker = value;
            }
        }
        private string playbackStartMarker = null;

        /// <summary>
        /// Gets or sets the end of the playback.
        /// </summary>
        public TimeSpan PlaybackEnd { get; set; }

        /// <summary>
        /// Gets or sets the marker identifier, which defines the end of the 
        /// playback, or null.
        /// Setting this property with an existing, non-null marker identifier
        /// will update <see cref="PlaybackEnd"/> with the position of the
        /// specified marker minus the time specified by
        /// <see cref="Timeline.MarkerBreak"/>.
        /// Setting this value to null or a non-existing marker identifier 
        /// will have no effect.
        /// </summary>
        public string PlaybackEndMarker
        {
            get => playbackEndMarker;
            set
            {
                if (value != null && sourceTimeline.TryGetMarker(value,
                    out Marker marker)) 
                    PlaybackEnd = marker.Position - Timeline.MarkerBreak;
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

        private readonly Dictionary<string, AnimationLayer> layers = 
            new Dictionary<string, AnimationLayer>();

        private bool resetPositionToStartOnPlaybackStart = true;

        private readonly Timeline sourceTimeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="Animation"/> 
        /// base class.
        /// </summary>
        /// <param name="sourceTimeline">
        /// The timeline, which is used by this <see cref="Animation"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thown when <paramref name="sourceTimeline"/> is null.
        /// </exception>
        public Animation(Timeline sourceTimeline)
        {
            this.sourceTimeline = sourceTimeline ?? 
                throw new ArgumentNullException(nameof(sourceTimeline));
            position = PlaybackStart = this.sourceTimeline.Start;
            PlaybackEnd = this.sourceTimeline.End;

            //It is assumed that the timeline.Layers collection has no
            //duplicates (due to the constructor of the TimelineLayer
            //class, which would throw an exception in case layers with 
            //duplicate identifiers were found).
            foreach (TimelineLayer sourceLayer in sourceTimeline)
                layers[sourceLayer.Identifier] =
                    new AnimationLayer(this, sourceLayer);
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
            Position = sourceTimeline.Start;
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
        public void Update(TimeSpan delta)
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
        /// Attempts to get an <see cref="AnimationLayer"/> from the current
        /// <see cref="Animation"/> instance.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the <see cref="AnimationLayer"/>.
        /// </param>
        /// <param name="layer">
        /// The requested <see cref="AnimationLayer"/> instance or null.
        /// </param>
        /// <returns>
        /// <c>true</c> if an <see cref="AnimationLayer"/> with the specified
        /// <paramref name="identifier"/> was found, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        public bool TryGetLayer(string identifier, out AnimationLayer layer)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            return layers.TryGetValue(identifier, out layer);
        }

        /// <summary>
        /// Gets an <see cref="AnimationLayer"/> from the current
        /// <see cref="Animation"/> instance.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the <see cref="AnimationLayer"/>.
        /// </param>
        /// <returns>
        /// The requested <see cref="AnimationLayer"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="AnimationLayer"/> with the specified
        /// <paramref name="identifier"/> was found.
        /// </exception>
        public AnimationLayer GetLayer(string identifier)
        {
            if (TryGetLayer(identifier, out AnimationLayer layer)) 
                return layer;
            else throw new ArgumentException("No layer with the specified " +
              "identifier was found.");
        }

        /// <summary>
        /// Gets a marker via its identifier.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the marker.
        /// </param>
        /// <returns>
        /// The <see cref="Marker"/> instance with the specified identifier.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no marker with the specified 
        /// <paramref name="identifier"/> was defined.
        /// </exception>
        /// <remarks>
        /// This method performs a binary search; therefore, this method is an 
        /// O(log n) operation, where n is <see cref="ICollection{T}.Count"/>
        /// of <see cref="Markers"/>. Internally, the
        /// <see cref="TryGetMarker(string, out Marker)"/> method is used.
        /// </remarks>
        public Marker GetMarker(string identifier)
        {
            return sourceTimeline.GetMarker(identifier);
        }

        /// <summary>
        /// Attempts to get a marker via its identifier.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the marker.
        /// </param>
        /// <param name="marker">
        /// The requested marker or null, if the method returns <c>false</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if a marker with the specified 
        /// <paramref name="identifier"/> was found, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <remarks>
        /// This method performs a binary search; therefore, this method is an 
        /// O(log n) operation, where n is <see cref="ICollection{T}.Count"/>
        /// of <see cref="Markers"/>. Internally, the
        /// <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, 
        /// out TValue)"/> method is used.
        /// </remarks>
        public bool TryGetMarker(string identifier, out Marker marker)
        {
            return sourceTimeline.TryGetMarker(identifier, out marker);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator"/> instance.</returns>
        public IEnumerator<AnimationLayer> GetEnumerator()
        {
            return layers.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{T}"/> instance.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return layers.Values.GetEnumerator();
        }
    }
}
