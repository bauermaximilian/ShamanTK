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

using Eterra.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Eterra.Common
{
    /// <summary>
    /// Provides a collection of <see cref="Keyframe"/> instances, 
    /// hierarchically structured into contained <see cref="TimelineLayer"/> 
    /// instances per object and <see cref="TimelineChannel"/> instances 
    /// per object parameter.
    /// Can be used to create an <see cref="Animation"/>.
    /// </summary>
    public class Timeline : IEnumerable<TimelineLayer>
    {
        /// <summary>
        /// Gets a sorted list of all markers.
        /// </summary>
        public ICollection<Marker> Markers => markers.Values;

        /// <summary>
        /// Gets a collection of all layers.
        /// </summary>
        public ICollection<TimelineLayer> Layers => layers.Values;

        /// <summary>
        /// Gets the length of the current <see cref="Timeline"/>
        /// (the distance between <see cref="Start"/> and <see cref="End"/>).
        /// </summary>
        public TimeSpan Length { get; }

        /// <summary>
        /// Gets the position of the first keyframe or marker, which defines 
        /// the start of the current <see cref="Timeline"/>.
        /// </summary>
        public TimeSpan Start { get; }

        /// <summary>
        /// Gets the position of the last keyframe or marker, which defines 
        /// the end of the current <see cref="Timeline"/>.
        /// </summary>
        public TimeSpan End { get; }

        //Changes to the markers need to be applied to both collections below!
        private readonly Dictionary<string, Marker> markers
            = new Dictionary<string, Marker>();
        private readonly SortedList<TimeSpan, Marker> markerPositions
            = new SortedList<TimeSpan, Marker>();

        private readonly Dictionary<string, TimelineLayer> layers
            = new Dictionary<string, TimelineLayer>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Timeline"/> class.
        /// </summary>
        /// <param name="layers">
        /// The <see cref="TimelineLayer"/> instances to be added to the new
        /// <see cref="Timeline"/> instance.
        /// The <see cref="TimelineLayer.Identifier"/> of each instance must
        /// be unique within the specified <paramref name="layers"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="layers"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="layers"/> contains more than 
        /// one instance that shares the same 
        /// <see cref="TimelineLayer.Identifier"/>.
        /// </exception>
        public Timeline(params TimelineLayer[] layers)
            : this(layers, new Marker[0])
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Timeline"/> class.
        /// </summary>
        /// <param name="layers">
        /// The <see cref="TimelineLayer"/> instances to be added to the new
        /// <see cref="Timeline"/> instance.
        /// The <see cref="TimelineLayer.Identifier"/> of each instance must
        /// be unique within the specified <paramref name="layers"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="layers"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <paramref name="layers"/> contains more than one
        /// instance that shares the same 
        /// <see cref="TimelineLayer.Identifier"/> (within the
        /// <paramref name="layers"/> enumeration).
        /// </exception>
        public Timeline(IEnumerable<TimelineLayer> layers)
            : this(layers, new List<Marker>()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Timeline"/> class.
        /// </summary>
        /// <param name="layers">
        /// The <see cref="TimelineLayer"/> instances to be added to the new
        /// <see cref="Timeline"/> instance.
        /// The <see cref="TimelineLayer.Identifier"/> of each instance must
        /// be unique within the specified <paramref name="layers"/>.
        /// </param>
        /// <param name="markers">
        /// The <see cref="Marker"/> instances to be added to the new
        /// <see cref="Timeline"/> instance.
        /// The <see cref="Marker.Identifier"/> of each instance must
        /// be unique within the specified <paramref name="markers"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="layers"/> or
        /// <paramref name="markers"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when either the <paramref name="layers"/> or
        /// the <paramref name="markers"/> contain more than one instance that
        /// share the same <see cref="TimelineLayer.Identifier"/> (within the
        /// <paramref name="layers"/> enumeration) or the same 
        /// <see cref="Marker.Identifier"/> (within the 
        /// <paramref name="markers"/> enumeration).
        /// </exception>
        public Timeline(IEnumerable<TimelineLayer> layers,
            IEnumerable<Marker> markers)
        {
            if (layers == null)
                throw new ArgumentNullException(nameof(layers));
            if (markers == null)
                throw new ArgumentNullException(nameof(markers));

            foreach (TimelineLayer timelineLayer in layers)
            {
                if (timelineLayer == null) continue;
                if (this.layers.ContainsKey(timelineLayer.Identifier))
                    throw new ArgumentException("The enumeration of " +
                        "timeline layers contains at least two instances " +
                        "with the same identifier.");
                else this.layers[timelineLayer.Identifier] = timelineLayer;
            }

            foreach (Marker marker in markers)
            {
                if (marker == null) continue;
                if (this.markers.ContainsKey(marker.Identifier))
                    throw new ArgumentException("The enumeration of " +
                        "markers contains at least two instances with the " +
                        "same identifier.");
                else
                {
                    this.markers[marker.Identifier] = marker;
                    markerPositions[marker.Position] = marker;
                }
            }

            TimeSpan start = markerPositions.Count > 0 ?
                markerPositions.Keys[0] : TimeSpan.MaxValue;
            TimeSpan end = markerPositions.Count > 0 ?
                markerPositions.Keys[markerPositions.Count - 1]
                : TimeSpan.MinValue;

            foreach (TimelineLayer layer in layers)
            {
                if (layer.Start < start) start = layer.Start;
                if (layer.End > end) end = layer.End;
            }

            //The position of the first marker or keyframe.
            Start = start != TimeSpan.MaxValue ? start : TimeSpan.Zero;
            //The position of the last marker or keyframe.
            End = end != TimeSpan.MinValue ? end : TimeSpan.Zero;
            //The distance between the start and the end alias timeline length.
            Length = end - start;
        }

        /// <summary>
        /// Gets a <see cref="TimelineLayer"/> from the current 
        /// <see cref="Timeline"/>.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the requested <see cref="TimelineLayer"/>.
        /// </param>
        /// <returns>
        /// The requested <see cref="TimelineLayer"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="TimelineLayer"/> with the specified
        /// <paramref name="identifier"/> exists in the current
        /// <see cref="Timeline"/>.
        /// </exception>
        public TimelineLayer GetLayer(string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (layers.TryGetValue(identifier, out TimelineLayer layer))
                return layer;
            else throw new ArgumentException("The timeline doesn't " +
                    "contain a layer with the specified identifier.");
        }

        /// <summary>
        /// Attempts to get a <see cref="TimelineLayer"/> from the current 
        /// <see cref="Timeline"/>.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the requested <see cref="TimelineLayer"/>.
        /// </param>
        /// <param name="layer">
        /// The requested <see cref="TimelineLayer"/> or null.
        /// </param>
        /// <returns>
        /// <c>true</c> if a <see cref="TimelineLayer"/> with the specified
        /// <paramref name="identifier"/> was found, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        public bool TryGetLayer(string identifier, out TimelineLayer layer)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            return layers.TryGetValue(identifier, out layer);
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
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));
            if (TryGetMarker(identifier, out Marker marker))
                return marker;
            else throw new ArgumentException("No marker with the specified " +
                "identifier was found.");
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
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));
            return markers.TryGetValue(identifier, out marker);
        }

        /// <summary>
        /// Gets the index of a <see cref="Marker"/> instance in the sorted 
        /// <see cref="Markers"/> list.
        /// </summary>
        /// <param name="identifier">The identifier of the marker.</param>
        /// <returns>The index of the marker.</returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no marker with the specified 
        /// <paramref name="identifier"/> was defined in the current
        /// <see cref="Timeline"/>.
        /// </exception>
        /// <remarks>
        /// This method performs two binary searches; therefore, this method 
        /// is an O(2*log n) operation, where n is 
        /// <see cref="ICollection{T}.Count"/> of <see cref="Markers"/>.
        /// </remarks>
        public int GetMarkerIndex(string identifier)
        {
            if (TryGetMarker(identifier, out Marker marker))
                return markerPositions.IndexOfKey(marker.Position);
            else throw new ArgumentException("No marker with the specified " +
                "identifier exists.");
        }

        /// <summary>
        /// Gets the index of a <see cref="Marker"/> instance in the sorted 
        /// <see cref="Markers"/> list.
        /// </summary>
        /// <param name="identifier">The identifier of the marker.</param>
        /// <returns>
        /// <c>true</c> if a <see cref="Marker"/> with the specified
        /// <paramref name="identifier"/> was found and its index was assigned
        /// to <paramref name="index"/>, <c>false</c> otherwise (index is
        /// set to <c>default</c> then).
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no marker with the specified 
        /// <paramref name="identifier"/> was defined in the current
        /// <see cref="Timeline"/>.
        /// </exception>
        /// <remarks>
        /// This method performs two binary searches; therefore, this method 
        /// is an O(2*log n) operation, where n is 
        /// <see cref="ICollection{T}.Count"/> of <see cref="Markers"/>.
        /// </remarks>
        public bool TryGetMarkerIndex(string identifier, out int index)
        {
            if (TryGetMarker(identifier, out Marker marker))
            {
                index = markerPositions.IndexOfKey(marker.Position);
                return true;
            }
            else
            {
                index = default;
                return false;
            }
        }

        /// <summary>
        /// Loads a <see cref="Timeline"/> instance in the internal format 
        /// used by <see cref="NativeFormatHandler"/>.
        /// </summary>
        /// <param name="stream">
        /// The source stream.
        /// </param>
        /// <param name="expectFormatHeader">
        /// <c>true</c> if a header, as defined in 
        /// <see cref="NativeFormatHandler"/>, is expected to occur at the
        /// position of the <paramref name="stream"/>, <c>false</c> if the 
        /// current position of the <paramref name="stream"/> is directly at 
        /// the beginning of the resource data.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Timeline"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="Stream.CanRead"/> of 
        /// <paramref name="stream"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the data in the stream had an invalid format.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// resource could be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        internal static Timeline Load(Stream stream,
            bool expectFormatHeader)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("The specified stream is not " +
                    "readable.");

            if (expectFormatHeader)
            {
                ResourceType resourceType =
                    NativeFormatHandler.ReadEntryHeader(stream);
                if (resourceType != ResourceType.Timeline)
                    throw new FormatException("The specified resource " +
                        "was no texture resource.");
            }

            List<Marker> markers = new List<Marker>();
            List<TimelineLayer> timelineLayers = new List<TimelineLayer>();

            uint markerCount = stream.ReadUnsignedInteger();
            for (int mi = 0; mi < markerCount; mi++)
            {
                byte[] markerBuffer = stream.ReadBuffer();

                try { markers.Add(Marker.FromBytes(markerBuffer)); }
                catch (Exception exc)
                {
                    throw new FormatException("The marker #" + mi +
                        " couldn't be loaded.", exc);
                }
            }

            uint layerCount = stream.ReadUnsignedInteger();
            for (int li = 0; li < layerCount; li++)
            {
                string timelineLayerIdentifier = stream.ReadString();
                uint channelCount = stream.ReadUnsignedInteger();
                List<TimelineChannel> timelineChannels = 
                    new List<TimelineChannel>();

                for (int ci = 0; ci < channelCount; ci++)
                {
                    string channelIdentifierName = stream.ReadString();
                    string channelIdentifierTypeConstraintName = 
                        stream.ReadString();
                    string channelValueTypeName = stream.ReadString();
                    InterpolationMethod channelInterpolationMethod =
                        (InterpolationMethod)stream.ReadSignedInteger();
                    uint channelKeyframeCount = stream.ReadUnsignedInteger();

                    Type channelValueType = Type.GetType(channelValueTypeName, 
                        false);
                    Type channelIdentifierTypeConstraint = Type.GetType(
                        channelIdentifierTypeConstraintName, false);

                    if (channelValueType == null)
                        throw new FormatException("The type of one or more " +
                            "channels isn't available or supported.");
                    if (channelValueType == null)
                        throw new FormatException("The type constraint of " +
                            "one or more channels isn't available " +
                            "or supported.");

                    ChannelIdentifier channelIdentifier =
                        ChannelIdentifier.Create(channelIdentifierName,
                        channelIdentifierTypeConstraint);
                    if (!channelIdentifier.MatchesConstraint(channelValueType))
                        throw new FormatException("The value type of the " +
                            "channel doesn't match with the type constraint " +
                            "defined by the identifier.");

                    if (!Enum.IsDefined(typeof(InterpolationMethod),
                        channelInterpolationMethod))
                        throw new FormatException("The interpolation method " +
                            "of the layer is invalid.");

                    List<Keyframe> keyframes = new List<Keyframe>();
                    uint keyframeSizeBytes;
                    Type keyframeType;

                    try
                    {
                        keyframeType = typeof(Keyframe<>).MakeGenericType(
                            channelValueType);
                        keyframeSizeBytes = (uint)Marshal.SizeOf(keyframeType);
                    }
                    catch (Exception exc)
                    {
                        throw new FormatException("The value type of the " +
                            "timeline channel #" + ci + " is invalid.", exc);
                    }

                    for (int ki = 0; ki < channelKeyframeCount; ki++)
                    {
                        byte[] keyframeBuffer = stream.ReadBuffer(
                            keyframeSizeBytes);

                        Keyframe keyframe;
                        try
                        {
                            keyframe = Keyframe.FromBytes(channelValueType,
                                keyframeBuffer);
                        }
                        catch (Exception exc)
                        {
                            throw new FormatException("The keyframe with the " +
                                "index " + ki + " is invalid.", exc);
                        }
                        keyframes.Add(keyframe);
                    }

                    Type channelType = typeof(TimelineChannel<>)
                        .MakeGenericType(channelValueType);

                    timelineChannels.Add(
                        (TimelineChannel)Activator.CreateInstance(
                        channelType, channelIdentifier, 
                        channelInterpolationMethod, keyframes));
                }

                timelineLayers.Add(new TimelineLayer(timelineLayerIdentifier,
                    timelineChannels));
            }

            try { return new Timeline(timelineLayers, markers); }
            catch (Exception exc)
            {
                throw new FormatException("The loaded timeline layers and " +
                    "markers couldn't be used to initialize a valid " +
                    "timeline.", exc);
            }
        }

        /// <summary>
        /// Saves the current <see cref="Timeline"/> instance in the internal 
        /// format used by <see cref="NativeFormatHandler"/>.
        /// </summary>
        /// <param name="stream">
        /// The target stream.
        /// </param>
        /// <param name="includeFormatHeader">
        /// <c>true</c> to include the format header (as specified in
        /// <see cref="NativeFormatHandler"/>), <c>false</c> to start right
        /// off with the resource data.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="Stream.CanWrite"/> of 
        /// <paramref name="stream"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <paramref name="stream"/> was disposed.
        /// </exception>
        internal void Save(Stream stream, bool includeFormatHeader)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("The specified stream is not " +
                    "writable.");

            if (includeFormatHeader)
                NativeFormatHandler.WriteEntryHeader(ResourceType.Timeline,
                    stream);

            stream.WriteUnsignedInteger((uint)markers.Count);
            foreach (Marker marker in Markers)
            {
                stream.WriteBuffer(marker.ToBytes(), true);
            }

            stream.WriteUnsignedInteger((uint)layers.Count);
            foreach (TimelineLayer layer in Layers)
            {
                stream.WriteString(layer.Identifier);
                stream.WriteUnsignedInteger((uint)layer.ChannelCount);
                
                foreach (TimelineChannel channel in layer)
                {
                    stream.WriteString(channel.Identifier.Identifier);
                    stream.WriteString(
                        channel.Identifier.ValueTypeConstraint.FullName);
                    stream.WriteString(channel.ValueType.FullName);
                    stream.WriteSignedInteger(
                        (int)channel.InterpolationMethod);
                    stream.WriteUnsignedInteger((uint)channel.KeyframeCount);

                    foreach (Keyframe keyframe in channel)
                    {
                        byte[] keyframeBuffer = keyframe.ToBytes();
                        stream.WriteBuffer(keyframeBuffer, false);
                    }
                }                
            }
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{T}"/> instance.</returns>
        public IEnumerator<TimelineLayer> GetEnumerator()
        {
            return Layers.GetEnumerator();
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator"/> instance.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Layers).GetEnumerator();
        }
    }
}
