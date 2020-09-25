using System;
using System.Collections.Generic;
using System.Numerics;

namespace Eterra.Common
{
    /// <summary>
    /// Represents a hierarchical collection of 
    /// <see cref="Node{ParameterCollection}"/> instances.
    /// </summary>
    public class Scene : Node<ParameterCollection>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Scene"/> class.
        /// </summary>
        /// <param name="sceneParameters">
        /// The <see cref="ParameterCollection"/> instance that provides the
        /// parameters of the new <see cref="Scene"/> instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="sceneParameters"/> is null.
        /// </exception>
        public Scene(ParameterCollection sceneParameters)
            : base(sceneParameters) { }

        /// <summary>
        /// Removes nodes from the current <see cref="Scene"/> instance which 
        /// don't match with a 
        /// their <see cref="Node{T}.Children"/> into their 
        /// <see cref="Node{T}.Parent"/>.
        /// </summary>
        /// <param name="requireAllIdentifiers">
        /// <c>true</c> to specify that every <see cref="ParameterIdentifier"/>
        /// from the <paramref name="requiredIdentifiers"/> needs to be defined
        /// in a <see cref="Node{T}"/> for it to be retained,
        /// <c>false</c> if at least one of the 
        /// <see cref="ParameterIdentifier"/> from the 
        /// <paramref name="requiredIdentifiers"/> needs to be defined in a
        /// <see cref="Node{T}"/> to be retained.
        /// </param>
        /// <param name="requiredIdentifiers">
        /// The list of <see cref="ParameterIdentifier"/> which either have
        /// to be present completely in a <see cref="Node{T}"/> instance for it
        /// to be retained in the <see cref="Scene"/> (if 
        /// <paramref name="requireAllIdentifiers"/> is <c>true</c>) or
        /// where at least one needs to be present in a <see cref="Node{T}"/>
        /// for it to be retained in the <see cref="Scene"/> (if 
        /// <paramref name="requireAllIdentifiers"/> is <c>false</c>).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="requiredIdentifiers"/>
        /// is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="ParameterIdentifier"/> is provided.
        /// </exception>
        public void DissolveNodesWithout(bool requireAllIdentifiers,
            params ParameterIdentifier[] requiredIdentifiers)
        {
            if (requiredIdentifiers == null)
                throw new ArgumentNullException(nameof(
                    requiredIdentifiers));
            if (requiredIdentifiers.Length == 0)
                throw new ArgumentException("At least one parameter " +
                    "identifier needs to be specified!");

            DissolveNodes(delegate (Node<ParameterCollection> node)
            {
                foreach (ParameterIdentifier identifier in
                    requiredIdentifiers)
                {
                    if (node.Value.ContainsKey(identifier))
                    {
                        if (!requireAllIdentifiers) return false;
                    }
                    else
                    {
                        if (requireAllIdentifiers) return true;
                    }
                }
                return true;
            }, InheritParametersFromParent);
        }

        /// <summary>
        /// Removes nodes from the current <see cref="Scene"/> instance which 
        /// match a certain selector and move their 
        /// <see cref="Node{T}.Children"/> into their 
        /// <see cref="Node{T}.Parent"/>.
        /// </summary>
        /// <param name="isRemovalCandidate">
        /// The function that, when provided with one of the
        /// <see cref="Node{T}"/> instances from this <see cref="Scene"/>, 
        /// decides whether to remove the instance from this 
        /// <see cref="Scene"/> (<c>true</c>) or to leave it in (<c>false</c>).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="isRemovalCandidate"/> is null.
        /// </exception>
        public void DissolveNodes(
            Func<Node<ParameterCollection>, bool> isRemovalCandidate)
        {
            DissolveNodes(isRemovalCandidate, InheritParametersFromParent);
        }

        /// <summary>
        /// Removes nodes from the current <see cref="Scene"/> instance which 
        /// match a certain selector and move their 
        /// <see cref="Node{T}.Children"/> into their 
        /// <see cref="Node{T}.Parent"/>.
        /// </summary>
        /// <param name="isRemovalCandidate">
        /// The function that, when provided with one of the
        /// <see cref="Node{T}"/> instances from this <see cref="Scene"/>, 
        /// decides whether to remove the instance from this 
        /// <see cref="Scene"/> (<c>true</c>) or to leave it in (<c>false</c>).
        /// </param>
        /// <param name="childNodeTransformer">
        /// The transform function, that will be applied to every child of
        /// the specified <paramref name="node"/> before they are moved into
        /// this nodes' <see cref="Children"/> and removed from the 
        /// specified <paramref name="node"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="isRemovalCandidate"/> or
        /// <paramref name="childNodeTransformer"/> are null.
        /// </exception>
        public void DissolveNodes(
            Func<Node<ParameterCollection>, bool> isRemovalCandidate,
            Action<Node<ParameterCollection>> childNodeTransformer)
        {
            if (isRemovalCandidate == null)
                throw new ArgumentNullException(nameof(isRemovalCandidate));
            if (childNodeTransformer == null)
                throw new ArgumentNullException(nameof(childNodeTransformer));

            var remainingNodesStack = new Stack<Node<ParameterCollection>>();

            remainingNodesStack.Push(this);

            while (remainingNodesStack.Count > 0)
            {
                Node<ParameterCollection> currentNode =
                    remainingNodesStack.Pop();

                if (!currentNode.IsRoot && isRemovalCandidate(currentNode))
                {
                    currentNode.Parent.RemoveChild(currentNode,
                        childNodeTransformer);
                    remainingNodesStack.Push(currentNode.Parent);
                }

                foreach (var child in currentNode.Children)
                    remainingNodesStack.Push(child);
            }
        }

        /// <summary>
        /// Deletes nodes (and their <see cref="Node{T}.Children"/>) 
        /// from the current <see cref="Scene"/> instance which match a 
        /// certain selector.
        /// </summary>
        /// <param name="isRemovalCandidate">
        /// The function that, when provided with one of the
        /// <see cref="Node{T}"/> instances from this <see cref="Scene"/>, 
        /// decides whether to remove the instance from this 
        /// <see cref="Scene"/> (<c>true</c>) or to leave it in (<c>false</c>).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="isRemovalCandidate"/> is null.
        /// </exception>
        /// <remarks>
        /// This method will call the 
        /// <see cref="OnChildRemoved(Node{ParameterCollection})"/> method
        /// for every <see cref="Node{T}"/> selected and removed by the
        /// <paramref name="isRemovalCandidate"/>, but not for eventual 
        /// children of these nodes.
        /// </remarks>
        public void DeleteNodes(
            Func<Node<ParameterCollection>, bool> isRemovalCandidate)
        {
            if (isRemovalCandidate == null)
                throw new ArgumentNullException(nameof(isRemovalCandidate));

            Stack<Node<ParameterCollection>> remainingNodesStack =
                new Stack<Node<ParameterCollection>>();

            remainingNodesStack.Push(this);

            while (remainingNodesStack.Count > 0)
            {
                var currentNode = remainingNodesStack.Pop();

                if (currentNode.Children.Count > 0)
                {
                    foreach (var child in currentNode.Children)
                        remainingNodesStack.Push(child);
                }
                else
                {
                    if (!currentNode.IsRoot && isRemovalCandidate(currentNode))
                    {
                        if (currentNode.Children.Count == 0)
                        {
                            currentNode.Parent.RemoveChild(currentNode);

                            if (!currentNode.Parent.IsRoot)
                                remainingNodesStack.Push(currentNode.Parent);
                        }
                    }
                }
            }
        }

        protected override void OnChildAdded(
            Node<ParameterCollection> newChild)
        {
            base.OnChildAdded(newChild);
        }

        protected override void OnChildRemoved(
            Node<ParameterCollection> removedChild)
        {
            base.OnChildRemoved(removedChild);
        }

        static void InheritParametersFromParent(
            Node<ParameterCollection> childNode)
        {
            if (childNode == null)
                throw new ArgumentNullException(nameof(childNode));

            Node<ParameterCollection> parentNode = childNode.Parent;

            if (childNode.IsRoot || parentNode.IsRoot) return;

            if (!parentNode.Value.TryGetValue(ParameterIdentifier.Position,
                out Vector3 parentPosition)) parentPosition = Vector3.Zero;
            if (!parentNode.Value.TryGetValue(ParameterIdentifier.Scale,
                out Vector3 parentScale)) parentScale = Vector3.One;
            if (!parentNode.Value.TryGetValue(ParameterIdentifier.Rotation,
                out Quaternion parentRotation))
                parentRotation = Quaternion.Identity;

            Matrix4x4 parentTransformation = MathHelper.CreateTransformation(
                parentPosition, parentScale, parentRotation);

            if (!childNode.Value.TryGetValue(ParameterIdentifier.Position,
                    out Vector3 childPosition)) childPosition = Vector3.Zero;
            if (!childNode.Value.TryGetValue(ParameterIdentifier.Scale,
                out Vector3 childScale)) childScale = Vector3.One;
            if (!childNode.Value.TryGetValue(ParameterIdentifier.Rotation,
                out Quaternion childRotation))
                childRotation = Quaternion.Identity;

            Matrix4x4 childTransformation =
                MathHelper.CreateTransformation(childPosition, childScale,
                childRotation);

            Matrix4x4 absoluteChildTransformation =
                MathHelper.CombineTransformations(parentTransformation,
                childTransformation);

            string nodeName = string.IsNullOrEmpty(childNode.Value.Name) ?
                "" : $"'{childNode.Value.Name}'";

            if (Matrix4x4.Decompose(absoluteChildTransformation,
                out Vector3 absoluteChildScale,
                out Quaternion absoluteChildRotation,
                out Vector3 absoluteChildPosition))
            {
                childNode.Value[ParameterIdentifier.Position] =
                    absoluteChildPosition;
                childNode.Value[ParameterIdentifier.Scale] =
                    absoluteChildScale;
                childNode.Value[ParameterIdentifier.Rotation] =
                    absoluteChildRotation;

                foreach (var parentParameter in parentNode.Value)
                {
                    if (!childNode.Value.ContainsKey(parentParameter.Key) &&
                        parentParameter.Key !=
                        ParameterIdentifier.Transformation &&
                        parentParameter.Key !=
                        ParameterIdentifier.TransformationGlobal)
                    {
                        childNode.Value[parentParameter.Key] =
                            parentParameter.Value;
                    }
                }
            }
            else Log.Warning("Decomposing absolute child " +
              $"transformation failed for node {nodeName}.",
              nameof(Scene));
        }

        static void CompleteTransformationParameters(
            Node<ParameterCollection> node,
            bool preferTransformationMatrixAsSource,
            bool throwOnMissingParentGlobalTransform)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            bool transformationGiven = node.Value.TryGetValue(
                ParameterIdentifier.Transformation,
                out Matrix4x4 transformation);

            bool positionGiven = node.Value.TryGetValue(
                ParameterIdentifier.Position, out Vector3 position);
            bool scaleGiven = node.Value.TryGetValue(
                ParameterIdentifier.Scale, out Vector3 scale);
            bool rotationGiven = node.Value.TryGetValue(
                ParameterIdentifier.Rotation, out Quaternion rotation);

            if (transformationGiven && ((!positionGiven && !scaleGiven &&
                !rotationGiven) || preferTransformationMatrixAsSource))
            {
                Matrix4x4.Decompose(transformation, out scale, out rotation,
                    out position);
            }
            else
            {
                if (!positionGiven) position = Vector3.Zero;
                if (!scaleGiven) scale = Vector3.One;
                if (!rotationGiven) rotation = Quaternion.Identity;

                transformation = MathHelper.CreateTransformation(position,
                    scale, rotation);
            }

            node.Value[ParameterIdentifier.Position] = position;
            node.Value[ParameterIdentifier.Scale] = scale;
            node.Value[ParameterIdentifier.Rotation] = rotation;
            node.Value[ParameterIdentifier.Transformation] = transformation;

            if (node.IsRoot)
            {
                node.Value[ParameterIdentifier.TransformationGlobal] =
                    transformation;
            }
            else
            {
                if (node.Parent.Value.TryGetValue(
                    ParameterIdentifier.TransformationGlobal,
                    out Matrix4x4 parentTransformationGlobal))
                {
                    node.Value[ParameterIdentifier.TransformationGlobal] =
                        MathHelper.CombineTransformations(
                            parentTransformationGlobal, transformation);
                }
                else if (throwOnMissingParentGlobalTransform)
                    throw new InvalidOperationException("The parent node " +
                    "doesn't contain a value for the parameter " +
                    $"'{nameof(ParameterIdentifier.TransformationGlobal)}'.");
            }
        }
    }
}
