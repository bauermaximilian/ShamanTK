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
using System.Collections.Generic;
using System.Numerics;

namespace ShamanTK.Common
{
    /// <summary>
    /// Represents a path consisting of connected segments of lines.
    /// </summary>
    public class PathLinear
    {
        private readonly SortedList<float, Vector3> pathSegments =
            new SortedList<float, Vector3>();

        /// <summary>
        /// Gets the length of the path.
        /// </summary>
        public float Length { get; }

        /// <summary>
        /// Gets a value indicating whether the path was closed during 
        /// initialisation so that the path is a loop (<c>true</c>) or
        /// whether the last point is not connected to the first point
        /// (<c>false</c>).
        /// </summary>
        public bool IsClosed { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PathLinear"/> class.
        /// </summary>
        /// <param name="pathPoints">
        /// The points which will be used to create the path.
        /// </param>
        /// <param name="closePath">
        /// <c>true</c> to connect the last point with the first point using
        /// an additional line segment so that the path loops, 
        /// <c>false</c> otherwise.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="pathPoints"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="pathPoints"/>
        /// parameter contains less than two elements.
        /// </exception>
        public PathLinear(IEnumerable<Vector3> pathPoints, bool closePath)
        {
            if (pathPoints == null)
                throw new ArgumentNullException(nameof(pathPoints));

            Vector3 firstPathPoint = Vector3.Zero;
            Vector3 previousPathPoint = Vector3.Zero;
            Length = 0;

            foreach (Vector3 pathPoint in pathPoints)
            {
                if (pathSegments.Count == 0) firstPathPoint = pathPoint;
                else Length += (pathPoint - previousPathPoint).Length();

                previousPathPoint = pathPoint;

                pathSegments.Add(Length, pathPoint);
            }

            if (closePath && pathSegments.Count > 0)
            {
                Length += (firstPathPoint - previousPathPoint).Length();
                pathSegments.Add(Length, firstPathPoint);
            }

            if (pathSegments.Count < 2)
                throw new ArgumentException("A path needs at least two " +
                    "path points.");
        }

        /// <summary>
        /// Gets the absolute position of a point on the path.
        /// </summary>
        /// <param name="pathOffset">
        /// The offset distance from the beginning of the path.
        /// </param>
        /// <returns>A new <see cref="Vector3"/>.</returns>
        public Vector3 GetPosition(float pathOffset)
        {
            (Vector3 start, Vector3 end, float ratio) = GetSegment(pathOffset);

            if (ratio == 0) return start;
            else if (ratio == 1) return end;
            else return start + ((end - start) * ratio);
        }

        /// <summary>
        /// Gets the closest offset distance on the path to an absolute 
        /// position.
        /// </summary>
        /// <param name="position">
        /// The absolute position of the point to be queried.
        /// </param>
        /// <param name="startOffset">
        /// The start offset, from which the search should be performed.
        /// </param>
        /// <returns>
        /// A <see cref="float"/>.
        /// </returns>
        public float GetOffset(Vector3 position, float startOffset = 0)
        {
            int startSegmentIndex = 
                GetNearestPathPointIndex(ref startOffset);

            float lastDistanceToPath = float.MaxValue;
            float closestOffset = 0;

            for (int i = startSegmentIndex; (i + 1) < pathSegments.Count; i++)
            {
                Vector3 segmentStart = pathSegments.Values[i];
                Vector3 segmentEnd = pathSegments.Values[i + 1];

                (float distance, float ratio) = GetDistanceToSegment(
                    position, segmentStart, segmentEnd);

                if (distance < lastDistanceToPath)
                {
                    lastDistanceToPath = distance;
                    ratio = Math.Max(Math.Min(ratio, 1), 0);

                    float segmentStartOffset = pathSegments.Keys[i];
                    float segmentEndOffset = pathSegments.Keys[i + 1];

                    float relativeOffset =
                        (segmentEndOffset - segmentStartOffset) * ratio;

                    closestOffset = segmentStartOffset + relativeOffset;
                }
            }

            return closestOffset;
        }

        private (float distance, float ratio) GetDistanceToSegment(
            Vector3 position, Vector3 start, Vector3 end)
        {
            // Source: https://geomalgorithms.com/a02-_lines.html

            Vector3 v = end - start;
            Vector3 w = position - start;

            float c1 = Vector3.Dot(w, v);
            float c2 = Vector3.Dot(v, v);
            float b = c1 / c2;
            Vector3 Pb = start + b * v;

            if (c1 <= 0) return ((position - start).Length(), b);
            else if (c2 <= c1) return ((position - end).Length(), b);            
            else return ((position - Pb).Length(), b);
        }                

        private (Vector3 start, Vector3 end, float ratio) GetSegment(
            float pathOffset)
        {
            int pointIndex = GetNearestPathPointIndex(ref pathOffset);

            Vector3 start, end;

            if ((pointIndex + 1) < pathSegments.Count)
            {
                start = pathSegments.Values[pointIndex];
                end = pathSegments.Values[pointIndex + 1];
            }
            else
            {
                start = pathSegments.Values[0];
                end = pathSegments.Values[1];
            }

            float segmentLength = pathSegments.Keys[pointIndex + 1] -
                    pathSegments.Keys[pointIndex];
            float relativePathOffset = pathOffset - 
                pathSegments.Keys[pointIndex];
            float ratio = relativePathOffset / segmentLength;

            return (start, end, ratio);
        }

        private int GetNearestPathPointIndex(ref float pathOffset)
        {
            if (pathOffset > Length)
                pathOffset %= Length;
            else if (pathOffset < 0)
                pathOffset = MathHelper.BringToRange(pathOffset, 
                    Length);

            int lower = 0;
            int upper = pathSegments.Count - 1;
            while (lower <= upper)
            {
                int middle = lower + (upper - lower) / 2;
                int compareResult = pathOffset.CompareTo(
                    pathSegments.Keys[middle]);
                if (compareResult == 0) return middle;
                else if (compareResult < 0) upper = middle - 1;
                else lower = middle + 1;
            }

            return Math.Max(Math.Min(lower, upper), 0);
        }
    }
}
