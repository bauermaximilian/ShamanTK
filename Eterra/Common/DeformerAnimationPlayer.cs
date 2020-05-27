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
using System.Collections.ObjectModel;
using System.Numerics;

namespace Eterra.Common
{
    /// <summary>
    /// Provides an animation player/mixer for <see cref="Timeline"/>
    /// instances which, in combination with a <see cref="Skeleton"/>,
    /// can produce <see cref="Deformer"/> instances to animate meshes 
    /// (especially characters).
    /// </summary>
    public class DeformerAnimationPlayer
    {
        /// <summary>
        /// Defines the suffix which is appended to a
        /// <see cref="Bone.Identifier"/>, when no 
        /// <see cref="AnimationPlayerLayer{T}"/> with <see cref="Matrix4x4"/>
        /// as type is found in the <see cref="Timeline"/>.
        /// If the resulting identifier (e.g. leftHand -> leftHand_position) 
        /// specifies an <see cref="AnimationPlayerLayer{T}"/> with 
        /// <see cref="Vector3"/> as type, the transformation 
        /// <see cref="Matrix4x4"/> is calculated using this value
        /// (and the other components like scale and rotation).
        /// </summary>
        public const string PositionLayerSuffix = 
            "_" + TimelineLayer.IdentifierPosition;

        /// <summary>
        /// Defines the suffix which is appended to a
        /// <see cref="Bone.Identifier"/>, when no 
        /// <see cref="AnimationPlayerLayer{T}"/> with <see cref="Matrix4x4"/>
        /// as type is found in the <see cref="Timeline"/>.
        /// If the resulting identifier (e.g. leftHand -> leftHand_scale) 
        /// specifies an <see cref="AnimationPlayerLayer{T}"/> with 
        /// <see cref="Vector3"/> as type, the transformation 
        /// <see cref="Matrix4x4"/> is calculated using this value
        /// (and the other components like position and rotation).
        /// </summary>
        public const string ScaleLayerSuffix =
            "_" + TimelineLayer.IdentifierScale;

        /// <summary>
        /// Defines the suffix which is appended to a
        /// <see cref="Bone.Identifier"/>, when no 
        /// <see cref="AnimationPlayerLayer{T}"/> with <see cref="Matrix4x4"/>
        /// as type is found in the <see cref="Timeline"/>.
        /// If the resulting identifier (e.g. leftHand -> leftHand_rotation) 
        /// specifies an <see cref="AnimationPlayerLayer{T}"/> with 
        /// <see cref="Quaternion"/> as type, the transformation 
        /// <see cref="Matrix4x4"/> is calculated using this value
        /// (and the other components like position and scale).
        /// </summary>
        public const string RotationLayerSuffix =
            "_" + TimelineLayer.IdentifierRotation;

        /// <summary>
        /// Provides a mapping between a <see cref="Bone"/> and an 
        /// (optional) <see cref="DeformerAnimationPlayer"/>.
        /// </summary>
        private class BoneLayerAttachment
        {
            public bool HasBoneIndex => bone.HasIndex;

            public byte? BoneIndex => bone.Index;

            public bool HasBoneIdentifier => bone.HasIdentifier;

            public string BoneIdentifier => bone.Identifier;

            public bool HasWorkingAttachment => IsUsable;

            private readonly Bone bone;

            private readonly DeformerAnimationPlayer deformerPlayer;

            private IAnimationLayer<Matrix4x4> transformationLayer,
                transformationLayerOverlay;

            private IAnimationLayer<Vector3> positionLayer,
                positionLayerOverlay;
            private IAnimationLayer<Vector3> scaleLayer,
                scaleLayerOverlay;
            private IAnimationLayer<Quaternion> rotationLayer,
                rotationLayerOverlay;

            //The following boolean getters should be self-explanatory with 
            //the documentation of the Bone class and should make it
            //easier to understand the code where they are used
            private bool IsUsable => HasTransformationLayer ||
                HasSeperateTransformationLayers;

            private bool CouldBeUsable => bone.HasIdentifier;

            private bool HasTransformationLayer =>
                transformationLayer != null 
                && transformationLayerOverlay != null;

            private bool HasPositionLayer =>
                (positionLayer != null && positionLayerOverlay != null);

            private bool HasScaleLayer =>
                (scaleLayer != null && scaleLayerOverlay != null);

            private bool HasRotationLayer =>
                (rotationLayer != null && rotationLayerOverlay != null);

            private bool HasSeperateTransformationLayers =>
                HasPositionLayer || HasScaleLayer || HasRotationLayer;

            /// <summary>
            /// Initializes a new instance of the 
            /// <see cref="BoneLayerAttachment"/> class.
            /// </summary>
            /// <param name="bone">
            /// The bone, which should be associated with the current instance.
            /// </param>
            /// <param name="deformerPlayer">
            /// The deformer player, which contains the 
            /// <see cref="AnimationPlayer"/> and the 
            /// <see cref="OverlayAnimationPlayer"/>, which will be used for
            /// this instance.
            /// </param>
            /// <exception cref="ArgumentNullException">
            /// Is thrown when <paramref name="bone"/> or
            /// <paramref name="deformerPlayer"/> are null.
            /// </exception>
            public BoneLayerAttachment(
                Bone bone, DeformerAnimationPlayer deformerPlayer)
            {
                this.bone = bone ??
                    throw new ArgumentNullException(nameof(bone));
                this.deformerPlayer = deformerPlayer ??
                    throw new ArgumentNullException(nameof(deformerPlayer));

                UpdateCachedPlayerLayers();
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

                if (HasTransformationLayer)
                {
                    transformation = 
                        transformationLayer.CurrentValue;
                    transformationOverlay = 
                        transformationLayerOverlay.CurrentValue;
                    transformationMatricesContainAnAnimatedValue = true;
                }
                else
                {
                    //In the following, you see the longest variable name
                    //in this project.
                    bool atLeastOneTransformationComponentWasFound = false;

                    Vector3 position = Vector3.Zero, 
                        positionOverlay = Vector3.Zero;
                    Vector3 scale = Vector3.One,
                        scaleOverlay = Vector3.One;
                    Quaternion rotation = Quaternion.Identity,
                        rotationOverlay = Quaternion.Identity;
                    
                    if (HasPositionLayer)
                    {
                        position = positionLayer.CurrentValue;
                        positionOverlay = 
                            positionLayerOverlay.CurrentValue;
                        atLeastOneTransformationComponentWasFound = true;
                    }
                    
                    if (HasScaleLayer)
                    {
                        scale = scaleLayer.CurrentValue;
                        scaleOverlay = scaleLayerOverlay.CurrentValue;
                        atLeastOneTransformationComponentWasFound = true;
                    }

                    if (HasRotationLayer)
                    {
                        rotation = rotationLayer.CurrentValue;
                        rotationOverlay =
                            rotationLayerOverlay.CurrentValue;
                        atLeastOneTransformationComponentWasFound = true;
                    }
                    
                    if (atLeastOneTransformationComponentWasFound)
                    {
                        /*transformation = Matrix4x4.CreateTranslation(position)
                            * Matrix4x4.CreateFromQuaternion(rotation)
                            * Matrix4x4.CreateScale(scale);*/
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

            /// <summary>
            /// Updates the <see cref="IAnimationLayer{T}"/> instances, which
            /// are cached in the current instance.
            /// This method should only be called when the 
            /// <see cref="Timeline"/> of the <see cref="AnimationPlayer"/> 
            /// (and the <see cref="OverlayAnimationPlayer"/>) was changed by
            /// adding (or replacing) a <see cref="TimelineLayer"/> with the
            /// same identifier as this <see cref="BoneIdentifier"/>. If this
            /// method is not called after a modification like described above,
            /// the <see cref="GetCurrentRelativeValue"/> will not be able to
            /// get the values of the new layer and just return the 
            /// the offset of the base <see cref="Bone"/> then.
            /// </summary>
            public void UpdateCachedPlayerLayers()
            {
                if (!IsUsable && CouldBeUsable)
                {
                    if (deformerPlayer.AnimationPlayer.LayerIdentifiers
                        .Contains(bone.Identifier))
                    {
                        try
                        {
                            transformationLayer = 
                                deformerPlayer.AnimationPlayer
                                .GetLayer<Matrix4x4>(bone.Identifier);
                            transformationLayerOverlay =
                                deformerPlayer.OverlayAnimationPlayer
                                .GetLayer<Matrix4x4>(bone.Identifier);
                            return;
                        }
                        catch { return; }
                    }

                    string positionLayerIdentifier = bone.Identifier +
                        PositionLayerSuffix;
                    string scaleLayerIdentifier = bone.Identifier +
                        ScaleLayerSuffix;
                    string rotationLayerIdentifier = bone.Identifier +
                        RotationLayerSuffix;

                    if (deformerPlayer.AnimationPlayer.LayerIdentifiers
                        .Contains(positionLayerIdentifier))
                    {
                        try
                        {
                            positionLayer = deformerPlayer.AnimationPlayer
                                .GetLayer<Vector3>(
                                positionLayerIdentifier);
                            positionLayerOverlay = 
                                deformerPlayer.OverlayAnimationPlayer
                                .GetLayer<Vector3>(
                                positionLayerIdentifier);
                        }
                        catch { return; }
                    }

                    if (deformerPlayer.AnimationPlayer.LayerIdentifiers
                        .Contains(scaleLayerIdentifier))
                    {
                        try
                        {
                            scaleLayer = deformerPlayer.AnimationPlayer
                                .GetLayer<Vector3>(
                                scaleLayerIdentifier);
                            scaleLayerOverlay = 
                                deformerPlayer.OverlayAnimationPlayer
                                .GetLayer<Vector3>(
                                scaleLayerIdentifier);
                        }
                        catch { return; }
                    }

                    if (deformerPlayer.AnimationPlayer.LayerIdentifiers
                        .Contains(rotationLayerIdentifier))
                    {
                        try
                        {
                            rotationLayer = deformerPlayer.AnimationPlayer
                                .GetLayer<Quaternion>(
                                rotationLayerIdentifier);
                            rotationLayerOverlay = 
                                deformerPlayer.OverlayAnimationPlayer
                                .GetLayer<Quaternion>(
                                rotationLayerIdentifier);
                        }
                        catch { return; }
                    }
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
        /// A collection of <see cref="BoneLayerAttachment"/>, where the
        /// key is the <see cref="Bone.Identifier"/> and the values are
        /// the <see cref="Bone"/> instances with that identifier.
        /// </summary>
        /// <remarks>
        /// The idea behind this seperate dictionary is that a change in a
        /// timeline layer with a specific identifier can trigger the 
        /// invocation of the 
        /// <see cref="BoneLayerAttachment.UpdateCachedPlayerLayers"/> method 
        /// without the need to traverse through the whole 
        /// <see cref="animationHierarchy"/> to find the affected bones.
        /// A <see cref="List{T}"/> is used as value as the 
        /// <see cref="Bone.Identifier"/> isn't necessarily unique - 
        /// more bones with the same identifier can be associated with the
        /// layer with that identifier.
        /// </remarks>
        private readonly ReadOnlyDictionary<string, List<BoneLayerAttachment>> 
            flatAnimationHierarchy;

        /// <summary>
        /// Gets the main <see cref="Common.AnimationPlayer"/> instance, which 
        /// provides the various <see cref="IAnimationLayer"/> instances for 
        /// the <see cref="Bone"/> elements in a <see cref="Skeleton"/>.
        /// </summary>
        public AnimationPlayer AnimationPlayer { get; }

        /// <summary>
        /// Gets the secondary <see cref="AnimationPlayer"/>, which can be
        /// controlled seperately (but has the same <see cref="Timeline"/>)
        /// and blended with the animation of the main 
        /// <see cref="AnimationPlayer"/> by the amount defined in
        /// <see cref="OverlayInfluence"/>.
        /// </summary>
        public AnimationPlayer OverlayAnimationPlayer { get; }

        /// <summary>
        /// Gets or sets the influence of the 
        /// <see cref="OverlayAnimationPlayer"/> on the overall animation as
        /// a value between 0.0 and 1.0. Out-of-range values will be clamped
        /// automatically.
        /// </summary>
        public float OverlayInfluence
        {
            get => overlayInfluence;
            set => overlayInfluence = Math.Min(Math.Max(value, 0f), 1f);
        }
        private float overlayInfluence;

        /// <summary>
        /// Gets a value indicating whether the <see cref="AnimationPlayer"/> 
        /// or the <see cref="OverlayAnimationPlayer"/> are currently playing
        /// (<c>true</c>) or if none of them is playing (<c>false</c>).
        /// </summary>
        public bool IsPlaying => AnimationPlayer.IsPlaying ||
            OverlayAnimationPlayer.IsPlaying;

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="DeformerAnimationPlayer"/> class.
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
        /// no effect on this <see cref="DeformerAnimationPlayer"/> instance.
        /// </remarks>
        public DeformerAnimationPlayer(Timeline timeline, Skeleton skeleton)
        {
            if (skeleton == null)
                throw new ArgumentNullException(nameof(skeleton));

            AnimationPlayer = new AnimationPlayer(timeline);
            OverlayAnimationPlayer = new AnimationPlayer(timeline);

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

            flatAnimationHierarchy =
                new ReadOnlyDictionary<string, List<BoneLayerAttachment>>(
                    flatHierarchy);
        }

        //These are remains of an old concept, which allowed timelines to be
        //modified after they were added to a player.
        //Will be removed in a future version.
        private void TimelineLayerCreated(object sender, string e)
        {
            if (flatAnimationHierarchy.ContainsKey(e))
            {
                foreach (BoneLayerAttachment attachment
                    in flatAnimationHierarchy[e])
                    attachment.UpdateCachedPlayerLayers();
            }
        }

        /// <summary>
        /// Updates the <see cref="AnimationPlayer.Position"/> of the 
        /// <see cref="AnimationPlayer"/> and 
        /// <see cref="OverlayAnimationPlayer"/>.
        /// </summary>
        /// <param name="delta">
        /// The amount of time which elapsed since the last invocation of this
        /// method.
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

            AnimationPlayer.Update(delta);
            OverlayAnimationPlayer.Update(delta);
        }

        /// <summary>
        /// Calculates the <see cref="Deformer"/> at the current position
        /// of the <see cref="AnimationPlayer"/> and the 
        /// <see cref="OverlayAnimationPlayer"/>.
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
