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
using System.Runtime.InteropServices;

namespace Eterra.Common
{
    /// <summary>
    /// Defines how a single <see cref="Vertex"/> is influenced by an array 
    /// of <see cref="Matrix4x4"/>. For all vertices in a <see cref="Mesh"/>,
    /// this can be used as the binding between a character rig and the mesh.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct DeformerAttachments
    {
        /// <summary>
        /// Gets or sets the first of four indicies that define which of 
        /// the matrices in a deformation array will transform the 
        /// <see cref="Vertex"/> this <see cref="DeformerAttachments"/> 
        /// are associated to. Often equivalent to the index of a character
        /// rigs bone.
        /// </summary>
        public byte AttachmentOneIndex { get; }

        /// <summary>
        /// Gets or sets the second of the four indicies that define which of 
        /// the matrices in a deformation array will transform the 
        /// <see cref="Vertex"/> this <see cref="DeformerAttachments"/> 
        /// are associated to. Often equivalent to the index of a character
        /// rigs bone.
        /// </summary>
        public byte AttachmentTwoIndex { get; }

        /// <summary>
        /// Gets or sets the third of the four indicies that define which of
        /// the matrices in a deformation array will transform the 
        /// <see cref="Vertex"/> this <see cref="DeformerAttachments"/> 
        /// are associated to. Often equivalent to the index of a character
        /// rigs bone.
        /// </summary>
        public byte AttachmentThreeIndex { get; }

        /// <summary>
        /// Gets or sets the last of the four indicies that define which of
        /// the matrices in a deformation array will transform the 
        /// <see cref="Vertex"/> this <see cref="DeformerAttachments"/> 
        /// are associated to. Often equivalent to the index of a character
        /// rigs bone.
        /// </summary>
        public byte AttachmentFourIndex { get; }

        /// <summary>
        /// Gets or sets the influence between 0 (0%) and 255 (100%) of the 
        /// deformation matrix with the index defined in 
        /// <see cref="AttachmentOneIndex"/>.
        /// </summary>
        public byte AttachmentOneWeight { get; }

        /// <summary>
        /// Gets or sets the influence between 0 (0%) and 255 (100%) of the 
        /// deformation matrix with the index defined in 
        /// <see cref="AttachmentTwoIndex"/>.
        /// </summary>
        public byte AttachmentTwoWeight { get; }

        /// <summary>
        /// Gets or sets the influence between 0 (0%) and 255 (100%) of the 
        /// deformation matrix with the index defined in 
        /// <see cref="AttachmentThreeIndex"/>.
        /// </summary>
        public byte AttachmentThreeWeight { get; }

        /// <summary>
        /// Gets or sets the influence between 0 (0%) and 255 (100%) of the 
        /// deformation matrix with the index defined in 
        /// <see cref="AttachmentFourIndex"/>.
        /// </summary>
        public byte AttachmentFourWeight { get; }

        /// <summary>
        /// Initializes a new <see cref="DeformerAttachments"/> instance.
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
        public DeformerAttachments(
            byte attachmentOneIndex, byte attachmentOneWeight, 
            byte attachmentTwoIndex, byte attachmentTwoWeight,
            byte attachmentThreeIndex, byte attachmentThreeWeight, 
            byte attachmentFourIndex, byte attachmentFourWeight)
        {
            AttachmentOneIndex = attachmentOneIndex;
            AttachmentTwoIndex = attachmentTwoIndex;
            AttachmentThreeIndex = attachmentThreeIndex;
            AttachmentFourIndex = attachmentFourIndex;

            AttachmentOneWeight = attachmentOneWeight;
            AttachmentTwoWeight = attachmentTwoWeight;
            AttachmentThreeWeight = attachmentThreeWeight;
            AttachmentFourWeight = attachmentFourWeight;
        }

        /// <summary>
        /// Initializes a new <see cref="DeformerAttachments"/> instance from
        /// a collection where the attachment IDs and weights are stored as
        /// tuples.
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
        /// Is thrown when <paramref name="attachmentList"/> contained more than
        /// four elements and <paramref name="ignoreOvermuch"/> 
        /// is <c>false</c>.
        /// </exception>
        public static DeformerAttachments FromList(
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

            return new DeformerAttachments(
                attachmentOneIndex, attachmentOneWeight,
                attachmentTwoIndex, attachmentTwoWeight,
                attachmentThreeIndex, attachmentThreeWeight,
                attachmentFourIndex, attachmentFourWeight);
        }

        /// <summary>
        /// Creates a new <see cref="DeformerAttachments"/> instance by 
        /// using the first 4 property bytes as attachment indicies and the 
        /// last 4 property bytes as attachment weights.
        /// </summary>
        /// <param name="properties">
        /// The properties instance from the parent <see cref="Vertex"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="DeformerAttachments"/> instance.
        /// </returns>
        public static DeformerAttachments FromVertexProperties(
            VertexPropertyData properties)
        {
            return new DeformerAttachments(properties.P1, properties.P5,
                properties.P2, properties.P6, properties.P3, properties.P7,
                properties.P4, properties.P8);
        }

        /// <summary>
        /// Creates a new <see cref="VertexPropertyData"/> instance from the
        /// current <see cref="DeformerAttachments"/> instance by putting the
        /// attachment indicies into the first 4 bytes of the resulting
        /// <see cref="VertexPropertyData"/> instance and the attachment 
        /// weights into the following, last 4 bytes.
        /// </summary>
        /// <returns>
        /// A new <see cref="VertexPropertyData"/> instance.
        /// </returns>
        public VertexPropertyData ToVertexPropertyData()
        {
            return new VertexPropertyData(AttachmentOneIndex,
                AttachmentTwoIndex, AttachmentThreeIndex, AttachmentFourIndex,
                AttachmentOneWeight, AttachmentTwoWeight,
                AttachmentThreeWeight, AttachmentFourWeight);
        }

        /// <summary>
        /// Checks if a specific attachment slot is assigned.
        /// </summary>
        /// <param name="slotIndex">
        /// The index of the attachment slot. Valid values are 0, 1, 2 and 3.
        /// </param>
        /// <returns>
        /// <c>true</c> if the slot is assigned (has a weight greater than 0),
        /// <c>false</c> if the slot is not assigned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="slotIndex"/> is less than 0
        /// or greater than 3.
        /// </exception>
        public bool IsAssigned(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: return AttachmentOneWeight != 0;
                case 1: return AttachmentTwoWeight != 0;
                case 2: return AttachmentThreeWeight != 0;
                case 3: return AttachmentFourWeight != 0;
                default: throw new ArgumentOutOfRangeException("slotIndex");
            }
        }
    }
}
