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
using System.Collections.ObjectModel;
using System.Numerics;

namespace ShamanTK.Common
{
    /// <summary>
    /// Provides an animation player/mixer for <see cref="Timeline"/>
    /// instances which, in combination with a <see cref="Skeleton"/>,
    /// can produce <see cref="Deformer"/> instances to animate meshes 
    /// (especially characters).
    /// </summary>
    public class DeformerAnimation
    {
        /// <summary>
        /// Provides a mapping between a <see cref="Bone"/> and an 
        /// (optional) <see cref="DeformerAnimation"/>.
        /// </summary>
        private class BoneLayerAttachment
        {
            private readonly Bone bone;

            private readonly DeformerAnimation deformerPlayer;

            private AnimationParameter<Matrix4x4> transformationChannel,
                transformationChannelOverlay;

            private AnimationParameter<Vector3> positionChannel,
                positionChannelOverlay;
            private AnimationParameter<Vector3> scaleChannel,
                scaleChannelOverlay;
            private AnimationParameter<Quaternion> rotationChannel,
                rotationChannelOverlay;

            private bool HasTransformationChannel =>
                transformationChannel != null
                && transformationChannelOverlay != null;

            private bool HasPositionChannel =>
                (positionChannel != null && positionChannelOverlay != null);

            private bool HasScaleChannel =>
                (scaleChannel != null && scaleChannelOverlay != null);

            private bool HasRotationChannel =>
                (rotationChannel != null && rotationChannelOverlay != null);

            private bool HasSeperateTransformationChannels =>
                HasPositionChannel || HasScaleChannel || HasRotationChannel;

            private Animation Animation => deformerPlayer.Animation;

            private Animation OverlayAnimation =>
                deformerPlayer.OverlayAnimation;

            public bool HasBoneIndex => bone.HasIndex;

            public byte? BoneIndex => bone.Index;

            public bool HasBoneIdentifier => bone.HasIdentifier;

            public string BoneIdentifier => bone.Identifier;

            public bool HasWorkingAttachment => HasTransformationChannel ||
                HasSeperateTransformationChannels;            

            /// <summary>
            /// Initializes a new instance of the 
            /// <see cref="BoneLayerAttachment"/> class.
            /// </summary>
            /// <param name="bone">
            /// The bone, which should be associated with the current instance.
            /// </param>
            /// <param name="deformerPlayer">
            /// The deformer player, which contains the 
            /// <see cref="DeformerAnimation.Animation"/> and the 
            /// <see cref="DeformerAnimation.OverlayAnimation"/>, which will 
            /// be used for this instance.
            /// </param>
            /// <exception cref="ArgumentNullException">
            /// Is thrown when <paramref name="bone"/> or
            /// <paramref name="deformerPlayer"/> are null.
            /// </exception>
            public BoneLayerAttachment(
                Bone bone, DeformerAnimation deformerPlayer)
            {
                this.bone = bone ??
                    throw new ArgumentNullException(nameof(bone));
                this.deformerPlayer = deformerPlayer ??
                    throw new ArgumentNullException(nameof(deformerPlayer));

                if (bone.HasIdentifier && Animation.TryGetLayer(
                    bone.Identifier, out AnimationLayer animationLayer) &&
                    OverlayAnimation.TryGetLayer(
                    bone.Identifier, out AnimationLayer overlayAnimationLayer))
                {
                    if (animationLayer.TryGetParameter<Matrix4x4>(
                        ParameterIdentifier.Transformation,
                        out var transformationChannel)
                        && overlayAnimationLayer.TryGetParameter<Matrix4x4>(
                        ParameterIdentifier.Transformation,
                        out var transformationChannelOverlay))
                    {
                        this.transformationChannel = transformationChannel;
                        this.transformationChannelOverlay =
                            transformationChannelOverlay;
                    }
                    else
                    {
                        if (animationLayer.TryGetParameter<Vector3>(
                            ParameterIdentifier.Position,
                            out var positionChannel) &&
                            overlayAnimationLayer.TryGetParameter<Vector3>(
                                ParameterIdentifier.Position,
                                out var positionChannelOverlay))
                        {
                            this.positionChannel = positionChannel;
                            this.positionChannelOverlay =
                                positionChannelOverlay;
                        }

                        if (animationLayer.TryGetParameter<Vector3>(
                            ParameterIdentifier.Scale,
                            out var scaleChannel) &&
                            overlayAnimationLayer.TryGetParameter<Vector3>(
                                ParameterIdentifier.Scale,
                                out var scaleChannelOverlay))
                        {
                            this.scaleChannel = scaleChannel;
                            this.scaleChannelOverlay =
                                scaleChannelOverlay;
                        }

                        if (animationLayer.TryGetParameter<Quaternion>(
                            ParameterIdentifier.Rotation,
                            out var rotationChannel) &&
                            overlayAnimationLayer.TryGetParameter<Quaternion>(
                                ParameterIdentifier.Rotation,
                                out var rotationChannelOverlay))
                        {
                            this.rotationChannel = rotationChannel;
                            this.rotationChannelOverlay =
                                rotationChannelOverlay;
                        }
                    }
                }
            }

            /// <summary>
            /// Calculates the current relative animated transformation 
            /// <see cref="Matrix4x4"/> of the associated <see cref="Bone"/>.
            /// </summary>
            /// <param name="boneOffset">
            /// The bone offset.
            /// </param>
            /// <returns>
            /// A new <see cref="Matrix4x4"/> instance or 
            /// <see cref="Matrix4x4.Identity"/> if 
            /// <see cref="HasWorkingAttachment"/> is <c>false</c>.
            /// </returns>
            public Matrix4x4 GetCurrentRelativeValue(out Matrix4x4 boneOffset)
            {
                bool transformationMatricesContainAnAnimatedValue = false;
                Matrix4x4 transformation = Matrix4x4.Identity, 
                    transformationOverlay = Matrix4x4.Identity;

                boneOffset = bone.Offset;

                if (HasTransformationChannel)
                {
                    transformation = 
                        transformationChannel.CurrentValue;
                    transformationOverlay = 
                        transformationChannelOverlay.CurrentValue;
                    transformationMatricesContainAnAnimatedValue = true;
                }
                else
                {
                    // In the following, you can see the probably longest 
                    // variable name in this project.
                    bool atLeastOneTransformationComponentWasFound = false;

                    Vector3 position = Vector3.Zero, 
                        positionOverlay = Vector3.Zero;
                    Vector3 scale = Vector3.One,
                        scaleOverlay = Vector3.One;
                    Quaternion rotation = Quaternion.Identity,
                        rotationOverlay = Quaternion.Identity;
                    
                    if (HasPositionChannel)
                    {
                        position = positionChannel.CurrentValue;
                        positionOverlay = 
                            positionChannelOverlay.CurrentValue;
                        atLeastOneTransformationComponentWasFound = true;
                    }
                    
                    if (HasScaleChannel)
                    {
                        scale = scaleChannel.CurrentValue;
                        scaleOverlay = scaleChannelOverlay.CurrentValue;
                        atLeastOneTransformationComponentWasFound = true;
                    }

                    if (HasRotationChannel)
                    {
                        rotation = rotationChannel.CurrentValue;
                        rotationOverlay =
                            rotationChannelOverlay.CurrentValue;
                        atLeastOneTransformationComponentWasFound = true;
                    }
                    
                    if (atLeastOneTransformationComponentWasFound)
                    {
                        transformation = MathHelper.CreateTransformation(
                            position, scale, rotation);
                        transformationOverlay = 
                            MathHelper.CreateTransformation(positionOverlay, 
                            scaleOverlay, rotationOverlay);
                        transformationMatricesContainAnAnimatedValue = true;
                    }
                }
                
                if (transformationMatricesContainAnAnimatedValue)
                {
                    Matrix4x4 animatedTransformation = Matrix4x4.Lerp(
                        transformation, transformationOverlay,
                        deformerPlayer.OverlayInfluence);

                    return animatedTransformation;
                }
                else
                {
                    return Matrix4x4.Identity;
                }
            }

            public override string ToString()
            {
                return bone.ToString() +
                    (HasWorkingAttachment ? " [Attached]" : " [Unattached]");
            }
        }

        private readonly Node<BoneLayerAttachment> animationHierarchy;

        /// <summary>
        /// Gets the main <see cref="Common.Animation"/> instance, which 
        /// provides the various <see cref="AnimationParameter"/> instances for 
        /// the <see cref="Bone"/> elements in a <see cref="Skeleton"/>.
        /// </summary>
        public Animation Animation { get; }

        /// <summary>
        /// Gets the secondary <see cref="Common.Animation"/>, which can be
        /// controlled seperately, but shares the base animation data with
        /// <see cref="Animation"/>. It can be blended with the main
        /// <see cref="Animation"/>  by adjusting the amount defined in
        /// <see cref="OverlayInfluence"/>.
        /// </summary>
        public Animation OverlayAnimation { get; }

        /// <summary>
        /// Gets or sets the influence of the influence of the 
        /// <see cref="OverlayAnimation"/> on the main <see cref="Animation"/>
        /// as a value between 0.0 and 1.0. Out-of-range values will be clamped
        /// automatically.
        /// </summary>
        public float OverlayInfluence
        {
            get => overlayInfluence;
            set => overlayInfluence = Math.Min(Math.Max(value, 0f), 1f);
        }
        private float overlayInfluence;

        /// <summary>
        /// Gets a value indicating whether the <see cref="Animation"/> 
        /// or the <see cref="OverlayAnimation"/> are currently playing
        /// (<c>true</c>) or if none of them are playing (<c>false</c>).
        /// </summary>
        public bool IsPlaying => Animation.IsPlaying ||
            OverlayAnimation.IsPlaying;

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="DeformerAnimation"/> class.
        /// </summary>
        /// <param name="timeline">
        /// The timeline which contains the animation for the
        /// <paramref name="skeleton"/>.
        /// </param>
        /// <param name="skeleton">
        /// The skeleton which defines the structure and relation of the
        /// bones and is used to turn the relative animated values of
        /// the <paramref name="timeline"/> to absolute transformations.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="timeline"/> or
        /// <paramref name="skeleton"/> are null.
        /// </exception>
        /// <remarks>
        /// The <paramref name="skeleton"/> is used to create an internal,
        /// hierarchical structure of <see cref="AnimationPlayerLayer{T}"/>
        /// and <see cref="Bone"/> instances. Changes to the
        /// <paramref name="skeleton"/> after this constructor will have
        /// no effect on this <see cref="DeformerAnimation"/> instance.
        /// </remarks>
        public DeformerAnimation(Timeline timeline, Skeleton skeleton)
        {
            if (skeleton == null)
                throw new ArgumentNullException(nameof(skeleton));

            Animation = new Animation(timeline);
            OverlayAnimation = new Animation(timeline);

            Dictionary<string, List<BoneLayerAttachment>> flatHierarchy 
                = new Dictionary<string, List<BoneLayerAttachment>>();

            animationHierarchy = skeleton.Convert(delegate (Bone bone)
            {
                BoneLayerAttachment attachment =
                    new BoneLayerAttachment(bone, this);

                if (bone.HasIdentifier)
                {
                    if (flatHierarchy.ContainsKey(bone.Identifier))
                        flatHierarchy[bone.Identifier].Add(attachment);
                    else flatHierarchy.Add(bone.Identifier,
                        new List<BoneLayerAttachment>() { attachment });
                }

                return attachment;
            });
        }

        /// <summary>
        /// Updates the <see cref="Animation.Position"/> of the 
        /// <see cref="Animation"/> and <see cref="OverlayAnimation"/>.
        /// </summary>
        /// <param name="delta">
        /// The amount of time the animations should progress.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <see cref="DisposableBase.IsDisposed"/> is 
        /// <c>true</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="delta"/> is negative.
        /// </exception>
        public virtual void Update(TimeSpan delta)
        {
            if (delta < TimeSpan.Zero)
                throw new ArgumentException("A negative value for time " +
                    "delta is not supported.");

            Animation.Update(delta);
            OverlayAnimation.Update(delta);
        }

        /// <summary>
        /// Calculates the <see cref="Deformer"/> at the current position
        /// of the <see cref="Animation"/> and the 
        /// <see cref="OverlayAnimation"/>.
        /// </summary>
        /// <returns>
        /// A new instance of the <see cref="Deformer"/> class.
        /// </returns>
        /// <remarks>
        /// If the <see cref="Skeleton"/> contains <see cref="Bone"/> instances
        /// which share the same <see cref="Bone.Index"/>, no exception is 
        /// thrown - the old value is just replaced by the one which comes
        /// after in the depth-first traversal.
        /// </remarks>
        public Deformer GetCurrentDeformer()
        {
            Matrix4x4[] deformers = 
                new Matrix4x4[animationHierarchy.HighestIndex + 1];

            CalculateAbsoluteTransformation(Matrix4x4.Identity,
                animationHierarchy, deformers);

            return Deformer.Create(deformers, false);
        }

        private void CalculateAbsoluteTransformation(
            in Matrix4x4 parentTransformation, 
            Node<BoneLayerAttachment> node,
            Matrix4x4[] deformersArrayTarget)
        {
            Matrix4x4 currentTransformation =
                node.Value.GetCurrentRelativeValue(out Matrix4x4 boneOffset);

            Matrix4x4 absoluteTransformation =
                currentTransformation * parentTransformation;

            if (node.Value.HasBoneIndex)
                deformersArrayTarget[node.Value.BoneIndex.Value] =
                    boneOffset * absoluteTransformation;

            foreach (Node<BoneLayerAttachment> childNode in node.Children)
                CalculateAbsoluteTransformation(absoluteTransformation, 
                    childNode, deformersArrayTarget);
        }
    }
}
