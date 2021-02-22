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
    /// <see cref="Timeline"/> are derived.
    /// </summary>
    public class Animation : IEnumerable<AnimationLayer>
    {
        /// <summary>
        /// Gets or sets the distance, which is subtracted from the 
        /// <see cref="PlaybackLength"/> when it's assigned using
        /// <see cref="SetPlaybackRange(string, bool)"/>. This value is also
        /// used in the same way to limit the playback to the maximum 
        /// <see cref="Timeline.Length"/>.
        /// </summary>
        public static TimeSpan PlaybackMargin { get; set; } = 
            TimeSpan.FromSeconds(1 / 25.0);

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
                
            }
        }
        private TimeSpan position;

        /// <summary>
        /// Gets a sorted list of all markers.
        /// </summary>
        public ICollection<Marker> Markers => Timeline.Markers;

        /// <summary>
        /// Gets or sets a value indicating whether the current playback loops
        /// between <see cref="PlaybackStart"/> and 
        /// <see cref="PlaybackRangeEnd"/> (<c>true</c>) or whether the 
        /// playback stops after it reached the <see cref="PlaybackRangeEnd"/>
        /// (<c>false</c>).
        /// </summary>
        public bool LoopPlayback { get; set; }

        /// <summary>
        /// Gets or sets the start of the playback range, from which the 
        /// playback starts initially, after being stopped or during a looped 
        /// playback.
        /// By default, this is equal to the <see cref="Timeline.Start"/>.
        /// </summary>
        public TimeSpan PlaybackStart { get; set; }

        /// <summary>
        /// Gets or sets the length of the playback (starting from
        /// <see cref="PlaybackStart"/>), after which the playback stops,
        /// or rewinds to <see cref="PlaybackStart"/> during a looped
        /// playback.
        /// By default, this is equal to the <see cref="Timeline.Length"/>.
        /// </summary>
        public TimeSpan PlaybackLength { get; set; }

        private readonly Dictionary<string, AnimationLayer> layers = 
            new Dictionary<string, AnimationLayer>();

        /// <summary>
        /// Gets the <see cref="Timeline"/> which provides the data for the
        /// current <see cref="Animation"/>.
        /// </summary>
        public Timeline Timeline { get; }

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
            Timeline = sourceTimeline ?? 
                throw new ArgumentNullException(nameof(sourceTimeline));

            ResetPlaybackRange();

            // It is assumed that the timeline.Layers collection has no
            // duplicates (as the constructor of the TimelineLayer would 
            // throw an exception in case layers with duplicate identifiers 
            // were found).
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
            IsPlaying = true;
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
            Position = PlaybackStart;
            IsPlaying = false;
        }

        /// <summary>
        /// Resets the <see cref="PlaybackStart"/> and 
        /// <see cref="PlaybackLength"/> to their default values.
        /// </summary>
        public void ResetPlaybackRange()
        {
            PlaybackStart = position = Timeline.Start;
            PlaybackLength = Timeline.Length;
        }

        /// <summary>
        /// Updates the <see cref="PlaybackStart"/> and 
        /// <see cref="PlaybackLength"/> using <see cref="Marker"/>s and
        /// resets the <see cref="Position"/> to the new 
        /// <see cref="PlaybackStart"/>.
        /// </summary>
        /// <param name="startMarkerIdentifier">
        /// The identifier of the <see cref="Marker"/> that should be used for
        /// the <see cref="PlaybackStart"/>.
        /// </param>
        /// <param name="untilNextMarker">
        /// <c>true</c> to set the <see cref="PlaybackLength"/> using the
        /// position of the <see cref="Marker"/> after the specified
        /// <paramref name="startMarkerIdentifier"/> so that the playback will
        /// stop or loop right before the next <see cref="Marker"/>, 
        /// <c>false</c> to set the <see cref="PlaybackLength"/> so that the 
        /// playback will run until the end of the whole animation.
        /// If there is no <see cref="Marker"/> after the specified 
        /// <paramref name="startMarkerIdentifier"/>, this parameter has no
        /// effect.
        /// </param>
        /// <returns>
        /// <c>true</c> when a <see cref="Marker"/> with the specified
        /// <paramref name="startMarkerIdentifier"/> was found and the
        /// <see cref="PlaybackStart"/>, <see cref="PlaybackLength"/> and
        /// <see cref="Position"/> were updated, <c>false</c> when no 
        /// <see cref="Marker"/> with that 
        /// <paramref name="startMarkerIdentifier"/> was found and no changes
        /// to <see cref="PlaybackStart"/>, <see cref="PlaybackLength"/> and
        /// <see cref="Position"/> were made.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="startMarkerIdentifier"/> is null.
        /// </exception>
        public bool SetPlaybackRange(string startMarkerIdentifier, 
            bool untilNextMarker)
        {
            if (startMarkerIdentifier == null)
                throw new ArgumentNullException(nameof(startMarkerIdentifier));

            if (Timeline.TryGetMarkerIndex(startMarkerIdentifier, 
                out int startMarkerIndex))
            {
                Marker startMarker = Timeline.GetMarker(startMarkerIndex);
                PlaybackStart = startMarker.Position;                

                if (untilNextMarker && Timeline.TryGetMarker(
                    startMarkerIndex + 1, out Marker nextMarker))
                {
                    PlaybackLength = nextMarker.Position -
                        startMarker.Position - PlaybackMargin;
                }
                else
                {
                    PlaybackLength = Timeline.End - startMarker.Position
                        - PlaybackMargin;
                }

                Position = PlaybackStart + PlaybackMargin;

                return true;
            }
            else return false;
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

            TimeSpan playbackEndMaximum = 
                (Timeline.Length - PlaybackMargin);
            TimeSpan playbackEnd = PlaybackStart + PlaybackLength;

            if (playbackEnd > playbackEndMaximum)
            {
                playbackEnd = playbackEndMaximum;
            }

            if (IsPlaying)
            {                
                TimeSpan newPosition = Position + delta;
                if (newPosition > playbackEnd)
                {
                    if (LoopPlayback)
                    {
                        TimeSpan offsetFromStart = new TimeSpan(
                            ((newPosition - playbackEnd).Ticks %
                            PlaybackLength.Ticks));

                        Position = PlaybackStart + offsetFromStart;
                    }
                    else
                    {
                        Position = playbackEnd;
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
            return Timeline.GetMarker(identifier);
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
            return Timeline.TryGetMarker(identifier, out marker);
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
