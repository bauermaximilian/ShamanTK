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
        /// <param name="index1">
        /// The deformer index of the first attachment.
        /// </param>
        /// <param name="weight1">
        /// The weight of the first attachment.
        /// </param>
        /// <param name="index2">
        /// The deformer index of the second attachment.
        /// </param>
        /// <param name="weight2">
        /// The weight of the second attachment.
        /// </param>
        /// <param name="index3">
        /// The deformer index of the third attachment.
        /// </param>
        /// <param name="weight3">
        /// The weight of the third attachment.
        /// </param>
        /// <param name="index4">
        /// The deformer index of the fourth attachment.
        /// </param>
        /// <param name="weight4">
        /// The weight of the fourth attachment.
        /// </param>
        public DeformerAttachments(byte index1, byte weight1, byte index2, 
            byte weight2, byte index3, byte weight3, byte index4, byte weight4)
        {
            AttachmentOneIndex = index1;
            AttachmentTwoIndex = index2;
            AttachmentThreeIndex = index3;
            AttachmentFourIndex = index4;

            AttachmentOneWeight = weight1;
            AttachmentTwoWeight = weight2;
            AttachmentThreeWeight = weight3;
            AttachmentFourWeight = weight4;
        }

        /// <summary>
        /// Initializes a new <see cref="DeformerAttachments"/> instance.
        /// </summary>
        /// <param name="attachments">
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
        /// Is thrown when <paramref name="attachments"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="attachments"/> contained more than
        /// four elements and <paramref name="ignoreOvermuch"/> 
        /// is <c>false</c>.
        /// </exception>
        public DeformerAttachments(IList<Tuple<byte, byte>> attachments,
            bool ignoreOvermuch)
        {
            if (attachments == null)
                throw new ArgumentNullException(nameof(attachments));

            if (attachments.Count > 0)
            {
                AttachmentOneIndex = attachments[0].Item1;
                AttachmentOneWeight = attachments[0].Item2;
            }
            else
            {
                AttachmentOneIndex = 0;
                AttachmentOneWeight = 0;
            }

            if (attachments.Count > 1)
            {
                AttachmentTwoIndex = attachments[1].Item1;
                AttachmentTwoWeight = attachments[1].Item2;
            }
            else
            {
                AttachmentTwoIndex = 0;
                AttachmentTwoWeight = 0;
            }

            if (attachments.Count > 2)
            {
                AttachmentThreeIndex = attachments[2].Item1;
                AttachmentThreeWeight = attachments[2].Item2;
            }
            else
            {
                AttachmentThreeIndex = 0;
                AttachmentThreeWeight = 0;
            }

            if (attachments.Count > 3)
            {
                AttachmentFourIndex = attachments[3].Item1;
                AttachmentFourWeight = attachments[3].Item2;
            }
            else
            {
                AttachmentFourIndex = 0;
                AttachmentFourWeight = 0;
            }

            if (attachments.Count > 4 && !ignoreOvermuch)
                throw new ArgumentException("The specified attachment " +
                    "collection contains more than the maximum of four " +
                    "supported elements.");
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
