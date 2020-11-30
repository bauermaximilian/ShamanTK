using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ShamanTK.Common
{
    /// <summary>
    /// Defines the available formats for a <see cref="VertexPropertyData"/>
    /// instance within a <see cref="Vertex"/>, which is provided by the
    /// parent <see cref="IO.MeshData"/>.
    /// </summary>
    public enum VertexPropertyDataFormat
    {
        /// <summary>
        /// The vertex properties are not assigned with data to be used 
        /// during rendering.
        /// </summary>
        None,
        /// <summary>
        /// The vertex properties are deformer attachments in the format of
        /// the <see cref="Common.DeformerAttachments"/> struct.
        /// </summary>
        DeformerAttachments,
        /// <summary>
        /// The first 4 bytes of the vertex properties provide a color, the
        /// last 4 bytes provide the illumination of the vertex. The color and
        /// the light color are in the format of the <see cref="Color"/> 
        /// struct.
        /// </summary>
        ColorLight
    }

    /// <summary>
    /// Provides a fixed-size collection of bytes for a vertex that can be used
    /// to store custom vertex properties. The meaning of the stored data is
    /// defined in the contained collection.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct VertexPropertyData
    {
        /// <summary>
        /// Gets the value of the first property byte of the 
        /// associated <see cref="Vertex"/>.
        /// </summary>
        public byte P1 { get; }

        /// <summary>
        /// Gets the value of the second property byte of the 
        /// associated <see cref="Vertex"/>.
        /// </summary>
        public byte P2 { get; }

        /// <summary>
        /// Gets the value of the third property byte of the 
        /// associated <see cref="Vertex"/>.
        /// </summary>
        public byte P3 { get; }

        /// <summary>
        /// Gets the value of the fourth property byte of the 
        /// associated <see cref="Vertex"/>.
        /// </summary>
        public byte P4 { get; }

        /// <summary>
        /// Gets the value of the fifth property byte of the 
        /// associated <see cref="Vertex"/>.
        /// </summary>
        public byte P5 { get; }

        /// <summary>
        /// Gets the value of the sixth property byte of the 
        /// associated <see cref="Vertex"/>.
        /// </summary>
        public byte P6 { get; }

        /// <summary>
        /// Gets the value of the seventh property byte of the 
        /// associated <see cref="Vertex"/>.
        /// </summary>
        public byte P7 { get; }

        /// <summary>
        /// Gets the value of the eight property byte of the 
        /// associated <see cref="Vertex"/>.
        /// </summary>
        public byte P8 { get; }

        /// <summary>
        /// Initializes a new <see cref="VertexPropertyData"/> instance.
        /// </summary>
        /// <param name="propertyBytes">
        /// An array of a maximum of 8 bytes that will be used to populate
        /// the new instance. If the array is smaller, the remaining property
        /// bytes are initialized with 0.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="propertyBytes"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the length of <paramref name="propertyBytes"/>
        /// is greater than 8.
        /// </exception>
        public VertexPropertyData(byte[] propertyBytes)
        {
            if (propertyBytes == null)
                throw new ArgumentNullException(nameof(propertyBytes));

            P1 = propertyBytes.Length > 0 ? propertyBytes[0] : (byte)0;
            P2 = propertyBytes.Length > 1 ? propertyBytes[1] : (byte)0;
            P3 = propertyBytes.Length > 2 ? propertyBytes[2] : (byte)0;
            P4 = propertyBytes.Length > 3 ? propertyBytes[3] : (byte)0;
            P5 = propertyBytes.Length > 4 ? propertyBytes[4] : (byte)0;
            P6 = propertyBytes.Length > 5 ? propertyBytes[5] : (byte)0;
            P7 = propertyBytes.Length > 6 ? propertyBytes[6] : (byte)0;
            P8 = propertyBytes.Length > 7 ? propertyBytes[7] : (byte)0;

            if (propertyBytes.Length > 8)
                throw new ArgumentException("The specified array is too big " +
                    "to be stored in a single instance completely.");
        }

        /// <summary>
        /// Initializes a new <see cref="VertexPropertyData"/> instance.
        /// </summary>
        /// <param name="p1">The value of the first property byte.</param>
        /// <param name="p2">The value of the second property byte.</param>
        /// <param name="p3">The value of the third property byte.</param>
        /// <param name="p4">The value of the fourth property byte.</param>
        /// <param name="p5">The value of the fifth property byte.</param>
        /// <param name="p6">The value of the sixth property byte.</param>
        /// <param name="p7">The value of the seventh property byte.</param>
        /// <param name="p8">The value of the eight property byte.</param>
        public VertexPropertyData(byte p1, byte p2, byte p3, byte p4,
            byte p5, byte p6, byte p7, byte p8)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
            P4 = p4;
            P5 = p5;
            P6 = p6;
            P7 = p7;
            P8 = p8;
        }

        /// <summary>
        /// Initializes a new <see cref="VertexPropertyData"/> instance
        /// in the <see cref="VertexPropertyDataFormat.DeformerAttachments"/>
        /// format.
        /// </summary>
        /// <param name="attachmentOneIndex">
        /// The deformer index of the first attachment.
        /// </param>
        /// <param name="attachmentOneWeight">
        /// The weight of the first attachment.
        /// </param>
        /// <param name="attachmentTwoIndex">
        /// The deformer index of the second attachment.
        /// </param>
        /// <param name="attachmentTwoWeight">
        /// The weight of the second attachment.
        /// </param>
        /// <param name="attachmentThreeIndex">
        /// The deformer index of the third attachment.
        /// </param>
        /// <param name="attachmentThreeWeight">
        /// The weight of the third attachment.
        /// </param>
        /// <param name="attachmentFourIndex">
        /// The deformer index of the fourth attachment.
        /// </param>
        /// <param name="attachmentFourWeight">
        /// The weight of the fourth attachment.
        /// </param>
        /// <returns>A new <see cref="VertexPropertyData"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when one of the specified indicies is less than 0 or
        /// greater than <see cref="byte.MaxValue"/>.
        /// </exception>
        /// <remarks>
        /// The weights are automatically clamped - both individually (so that
        /// no weight exceeds 100%) and in total (so that the sum of all 
        /// weights doesn't exceed 100%).
        /// </remarks>
        public static VertexPropertyData CreateAsDeformerAttachment(
            uint attachmentOneIndex, uint attachmentTwoIndex, 
            uint attachmentThreeIndex, uint attachmentFourIndex,
            float attachmentOneWeight, float attachmentTwoWeight,
            float attachmentThreeWeight, float attachmentFourWeight)
        {
            if (attachmentOneIndex < byte.MinValue ||
                attachmentOneIndex > byte.MaxValue)
                throw new ArgumentOutOfRangeException(
                    nameof(attachmentOneIndex));
            if (attachmentTwoIndex < byte.MinValue ||
                attachmentTwoIndex > byte.MaxValue)
                throw new ArgumentOutOfRangeException(
                    nameof(attachmentTwoIndex));
            if (attachmentThreeIndex < byte.MinValue ||
                attachmentThreeIndex > byte.MaxValue)
                throw new ArgumentOutOfRangeException(
                    nameof(attachmentThreeIndex));
            if (attachmentFourIndex < byte.MinValue ||
                attachmentFourIndex > byte.MaxValue)
                throw new ArgumentOutOfRangeException(
                    nameof(attachmentFourIndex));

            byte p1 = (byte)attachmentOneIndex;
            byte p2 = (byte)attachmentTwoIndex;
            byte p3 = (byte)attachmentThreeIndex;
            byte p4 = (byte)attachmentFourIndex;

            float weightSum = attachmentOneWeight + attachmentTwoWeight +
                attachmentThreeWeight + attachmentFourWeight;
            // If the sum of all weights is less than 1, the weights shouldn't
            // be "increased" so that they would reach 1 in total (as this 
            // is probably not what the model artist intended), while a sum
            // greater than 1 is most certainly an error and needs to be
            // corrected to avoid graphical glitches.
            float weightDampeningFactor = 
                weightSum == 0 ? 1 : Math.Max(1, (1.0f / weightSum));

            byte p5 = (byte)(byte.MaxValue * Math.Min(1, Math.Max(0, 
                attachmentOneWeight * weightDampeningFactor)));
            byte p6 = (byte)(byte.MaxValue * Math.Min(1, Math.Max(0, 
                attachmentTwoWeight * weightDampeningFactor)));
            byte p7 = (byte)(byte.MaxValue * Math.Min(1, Math.Max(0, 
                attachmentThreeWeight * weightDampeningFactor)));
            byte p8 = (byte)(byte.MaxValue * Math.Min(1, Math.Max(0, 
                attachmentFourWeight * weightDampeningFactor)));

            return new VertexPropertyData(p1, p2, p3, p4, p5, p6, p7, p8);
        }

        /// <summary>
        /// Initializes a new <see cref="VertexPropertyData"/> instance
        /// in the <see cref="VertexPropertyDataFormat.DeformerAttachments"/>
        /// format.
        /// </summary>
        /// <param name="attachmentIndicies"></param>
        /// <param name="attachmentWeights"></param>
        /// <returns>A new <see cref="VertexPropertyData"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when one of the specified indicies is less than 0 or
        /// greater than <see cref="byte.MaxValue"/>.
        /// </exception>
        /// <remarks>
        /// The weights are automatically clamped - both individually (so that
        /// no weight exceeds 100%) and in total (so that the sum of all 
        /// weights doesn't exceed 100%).
        /// </remarks>
        public static VertexPropertyData CreateAsDeformerAttachment(
            (uint, uint, uint, uint)attachmentIndicies,
            (float, float, float, float)attachmentWeights)
        {
            return CreateAsDeformerAttachment(attachmentIndicies.Item1,
                attachmentIndicies.Item2, attachmentIndicies.Item3,
                attachmentIndicies.Item4, attachmentWeights.Item1,
                attachmentWeights.Item2, attachmentWeights.Item3,
                attachmentWeights.Item4);
        }

        /// <summary>
        /// Initializes a new <see cref="VertexPropertyData"/> instance
        /// in the <see cref="VertexPropertyDataFormat.DeformerAttachments"/>
        /// format.
        /// </summary>
        /// <param name="attachmentList">
        /// A collection of attachments as tuples, where the first item of each
        /// tuple is the deformer index and the second item the weight.
        /// </param>
        /// <param name="ignoreOvermuch">
        /// <c>true</c> to only apply the first four elements and ignore any
        /// additional elements after the fourth one, <c>false</c> to throw
        /// an <see cref="ArgumentException"/> if the collection contains
        /// more than four elements.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="attachmentList"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="attachmentList"/> contained more 
        /// thanv four elements and <paramref name="ignoreOvermuch"/> 
        /// is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// While this method does not allow individual weights to exceed
        /// 100% (or be less than 0%), it doesn't ensure that the sum of
        /// all weights doesn't exceed 100%. To avoid graphical glitches,
        /// the input data for the attachment list needs to be sanitized
        /// before it's being passed to this method.
        /// </remarks>
        public static VertexPropertyData CreateAsDeformerAttachment(
            IList<Tuple<byte, byte>> attachmentList, bool ignoreOvermuch)
        {
            if (attachmentList == null)
                throw new ArgumentNullException(nameof(attachmentList));

            byte attachmentOneIndex = 0, attachmentOneWeight = 0,
                attachmentTwoIndex = 0, attachmentTwoWeight = 0,
                attachmentThreeIndex = 0, attachmentThreeWeight = 0,
                attachmentFourIndex = 0, attachmentFourWeight = 0;

            if (attachmentList.Count > 0)
            {
                attachmentOneIndex = attachmentList[0].Item1;
                attachmentOneWeight = attachmentList[0].Item2;
            }

            if (attachmentList.Count > 1)
            {
                attachmentTwoIndex = attachmentList[1].Item1;
                attachmentTwoWeight = attachmentList[1].Item2;
            }

            if (attachmentList.Count > 2)
            {
                attachmentThreeIndex = attachmentList[2].Item1;
                attachmentThreeWeight = attachmentList[2].Item2;
            }

            if (attachmentList.Count > 3)
            {
                attachmentFourIndex = attachmentList[3].Item1;
                attachmentFourWeight = attachmentList[3].Item2;
            }

            if (attachmentList.Count > 4 && !ignoreOvermuch)
                throw new ArgumentException("The specified attachment " +
                    "collection contains more than the maximum of four " +
                    "supported elements.");

            return new VertexPropertyData(
                attachmentOneIndex, attachmentOneWeight,
                attachmentTwoIndex, attachmentTwoWeight,
                attachmentThreeIndex, attachmentThreeWeight,
                attachmentFourIndex, attachmentFourWeight);
        }

        /// <summary>
        /// Initializes a new <see cref="VertexPropertyData"/> instance
        /// in the <see cref="VertexPropertyDataFormat.ColorLight"/>
        /// format.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="light"></param>
        /// <returns>A new <see cref="VertexPropertyData"/> instance.</returns>
        public static VertexPropertyData CreateAsColorLight(Color color,
            Color light)
        {
            return new VertexPropertyData(color.R, color.G, color.B,
                color.Alpha, light.R, light.G, light.B, light.Alpha);
        }
    }
}
