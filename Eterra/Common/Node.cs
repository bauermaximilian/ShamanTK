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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Eterra.Common
{
    /// <summary>
    /// Converts a node value of type <typeparamref name="T"/> into a 
    /// value of type <typeparamref name="TargetT"/>.
    /// </summary>
    /// <typeparam name="SourceT">
    /// The current value type of the nodes.
    /// </typeparam>
    /// <typeparam name="TargetT">
    /// The target type, into which the node value of type
    /// <typeparamref name="T"/> will be converted into.
    /// </typeparam>
    /// <param name="currentNodeValue">
    /// The value of the node which is currently handled by the method
    /// using this delegate.
    /// </param>
    /// <returns>
    /// A new instance of the <typeparamref name="TargetT"/> class.
    /// </returns>
    public delegate TargetT NodeConvertValue<SourceT, TargetT>(
        SourceT currentNodeValue) where TargetT : class;

    /// <summary>
    /// Creates an absolute node value of type 
    /// <typeparamref name="TargetT"/> from the value of the current
    /// node of the original type <typeparamref name="T"/> and the 
    /// previously flattened parent value (of the current node).
    /// </summary>
    /// <typeparam name="SourceT">
    /// The current value type of the nodes.
    /// </typeparam>
    /// <typeparam name="TargetT">
    /// The target type, into which the node value of type
    /// <typeparamref name="T"/> will be converted and flattened into.
    /// </typeparam>
    /// <param name="currentNodeValue">
    /// The value of the node which is currently handled by the method
    /// using this delegate.
    /// </param>
    /// <param name="parentNodeValue">
    /// The previously converted (and "flattened") value of parent node
    /// of the currently handled node, or null if the currently handled
    /// node is the root node in the node hierarchy (or the first node
    /// in the process).
    /// </param>
    /// <returns>
    /// A new instance of the <typeparamref name="TargetT"/> class.
    /// </returns>
    public delegate TargetT NodeFlattenValue<SourceT, TargetT>(
        SourceT currentNodeValue, TargetT parentNodeValue)
        where TargetT : class;

    /// <summary>
    /// Represents a single element in a hierarchy of nodes.
    /// </summary>
    public class Node<T> : ICloneable, IEnumerable<Node<T>>
        where T : class
    {
        private class NodeEnumerator : IEnumerator<Node<T>>
        {
            public Node<T> Current { get; private set; }

            object IEnumerator.Current => throw new NotImplementedException();

            private readonly Node<T> startingNode;
            private readonly Queue<Node<T>> nodeQueue = new Queue<Node<T>>();

            public NodeEnumerator(Node<T> startingNode)
            {
                this.startingNode = startingNode ??
                    throw new ArgumentNullException(nameof(startingNode));

                Reset();
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                if (nodeQueue.Count > 0)
                {
                    Current = nodeQueue.Dequeue();
                    foreach (Node<T> child in Current.Children)
                        nodeQueue.Enqueue(child);
                    return true;
                }
                else return false;
            }

            public void Reset()
            {
                nodeQueue.Clear();
                nodeQueue.Enqueue(startingNode);
                MoveNext();
            }
        }

        private static readonly InvalidOperationException
            ReadOnlyViolationException = new InvalidOperationException(
            "The hierarchy is read-only and can't be modified.");

        /// <summary>
        /// Gets a value indicating whether the current <see cref="Node{T}"/>
        /// and all other child nodes of <see cref="Root"/> are read-only and 
        /// can't be modified (<c>true</c>) or not and can be modified 
        /// (<c>false</c>).
        /// This property only affects the <see cref="Node{T}"/> hierarchy
        /// and not the <see cref="Value"/> of the individual nodes.
        /// </summary>
        public bool IsReadOnly => IsRoot ? isReadOnly : Root.IsReadOnly;
        private readonly bool isReadOnly;

        /// <summary>
        /// Gets a boolean indicating whether the current 
        /// <see cref="Node{T}"/> node is the root of the node hierarchy 
        /// (<c>true</c>) or if it's a non-root element of a hierarchy 
        /// (<c>false</c>).
        /// </summary>
        public bool IsRoot => Parent == null;

        /// <summary>
        /// Gets a read-only collection of the children of the current
        /// <see cref="Node{T}"/> node.
        /// </summary>
        public IReadOnlyCollection<Node<T>> Children => readOnlyChildren;
        private readonly ReadOnlyCollection<Node<T>> readOnlyChildren;
        private readonly List<Node<T>> children =
            new List<Node<T>>();

        /// <summary>
        /// Gets the root <see cref="Node{T}"/> of the hierarchy the
        /// current node is contained in or a reference of itself if 
        /// <see cref="IsRoot"/> is <c>true</c>.
        /// </summary>
        public Node<T> Root { get; }

        /// <summary>
        /// Gets the parent <see cref="Node{T}"/> or null, if the
        /// current element is the root element of the hierarchy.
        /// </summary>
        public Node<T> Parent { get; }

        /// <summary>
        /// Gets the value of the current <see cref="Node{T}"/> node.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Gets the depth of the current <see cref="Node{T}"/> instance
        /// in the hierarchy in relation to <see cref="Root"/>.
        /// For <see cref="Root"/>, this value is 0.
        /// </summary>
        public int Depth { get; } = 0;

        private int rootHighestIndex = 0;

        /// <summary>
        /// Gets the index of the node inside the tree.
        /// This value is assigned when the node is created and doesn't
        /// change, even if nodes with an index smaller than the current one
        /// get removed.
        /// This can therefore be used as a faster alternative to the 
        /// <see cref="object.GetHashCode"/> method.
        /// </summary>
        internal int Index { get; }

        /// <summary>
        /// Gets the currently highest index in the <see cref="Node{T}"/>
        /// hierarchy.
        /// </summary>
        internal int HighestIndex => Root.rootHighestIndex;

        private Node()
        {
            Value = default;
            readOnlyChildren =
                new ReadOnlyCollection<Node<T>>(children);
            Root = this;
            Index = 0;
            isReadOnly = false;
        }

        /// <summary>
        /// Initializes a new <see cref="Node{T}"/> instance with the
        /// current element as root.
        /// </summary>
        /// <param name="value">
        /// The value of the root node.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="value"/> is null.
        /// </exception>
        public Node(T value) : this()
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        private Node(T value, Node<T> parent) : this(value)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            Root = parent.Root;
            Depth = parent.Depth + 1;

            //Assign the currently highest index, which is stored in the root,
            //as index of the current node and increment that value by one.
            Index = Root.rootHighestIndex++;
        }

        /// <summary>
        /// Initializes a new read-only root <see cref="Node{T}"/> instance 
        /// from another root <see cref="Node{T}"/>.
        /// </summary>
        /// <param name="otherRoot">
        /// The other node which will be the root node of the new node tree.
        /// </param>
        /// <param name="clone">
        /// <c>true</c> to clone the child nodes recursively,
        /// <c>false</c> to just copy the node instances from the collection
        /// into the new node.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="otherRoot"/> is null.
        /// </exception>
        protected Node(Node<T> otherRoot, bool clone) : this()
        {
            if (otherRoot == null)
                throw new ArgumentNullException(nameof(otherRoot));

            Value = otherRoot.Value;

            foreach (Node<T> childNode in otherRoot.children)
            {
                if (clone)
                    children.Add(childNode.CloneRecursively(childNode));
                else
                    children.Add(childNode);
            }

            //Copies the highest index from the previous root node.
            rootHighestIndex = otherRoot.rootHighestIndex;

            isReadOnly = true;
        }

        /// <summary>
        /// Adds a new child node to the current 
        /// <see cref="Node{T}"/>.
        /// </summary>
        /// <param name="value">
        /// The value of the child node.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Node{T}"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="value"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsReadOnly"/> is <c>true</c>.
        /// </exception>
        public Node<T> AddChild(T value)
        {
            if (IsReadOnly) throw ReadOnlyViolationException;

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Node<T> childIdem = new Node<T>(value, this);
            children.Add(childIdem);
            return childIdem;
        }

        /// <summary>
        /// Removes a child node from the current 
        /// <see cref="Node{ValueT}"/>.
        /// </summary>
        /// <param name="node">The node instance to be removed.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="node"/> was found and 
        /// removed, <c>false</c> if the instance wasn't present in 
        /// <see cref="Children"/> and no changes were made.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="node"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsReadOnly"/> is <c>true</c>.
        /// </exception>
        public bool RemoveChild(Node<T> node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (IsReadOnly) throw ReadOnlyViolationException;

            return children.Remove(node);
        }

        /// <summary>
        /// Assigns a new value to <see cref="Value"/>.
        /// </summary>
        /// <param name="newValue">
        /// The new value for <see cref="Value"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="newValue"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <see cref="IsReadOnly"/> is <c>true</c>.
        /// </exception>
        public void SetValue(T newValue)
        {
            if (IsReadOnly) throw ReadOnlyViolationException;

            Value = newValue ?? 
                throw new ArgumentNullException(nameof(newValue));
        }

        /// <summary>
        /// Traverses the node hierarchy depth-first and executes an action
        /// for each occurred node.
        /// </summary>
        /// <param name="action">
        /// The action to be executed for each node.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="action"/> is null.
        /// </exception>
        public void TraverseDepthFirst(Action<Node<T>> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            action(this);
            foreach (Node<T> child in Children) TraverseDepthFirst(action);
        }

        /// <summary>
        /// Traverses the node hierarchy breadth-first and executes an action
        /// for each occurred node.
        /// </summary>
        /// <param name="action">
        /// The action to be executed for each node.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="action"/> is null.
        /// </exception>
        public void TraverseBreadthFirst(Action<Node<T>> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Queue<Node<T>> queue = new Queue<Node<T>>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                Node<T> node = queue.Dequeue();
                action(node);
                foreach (Node<T> child in node.Children) queue.Enqueue(child);
            }
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{Node{T}}"/> for the 
        /// current instance, which traverses through the <see cref="Node{T}"/>
        /// hierarchy from the current node through all child nodes
        /// breadth-first.
        /// </summary>
        /// <returns>
        /// A new <see cref="IEnumerator{Node{T}}"/> instance.
        /// </returns>
        public IEnumerator<Node<T>> GetEnumerator()
        {
            return new NodeEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a string that represents the <see cref="Value"/>.
        /// </summary>
        /// <returns>
        /// A string that represents the <see cref="Value"/>.
        /// </returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        /// <summary>
        /// Creates a deep copy of the current <see cref="Node{T}"/>
        /// and all <see cref="Children"/> and the <see cref="Value"/> of
        /// each node converted from <typeparamref name="T"/> to
        /// <typeparamref name="TargetT"/>.
        /// The new returned hierarchy starts at the current 
        /// <see cref="Node{T}"/> instance, which will be root in that new
        /// <see cref="Node{TargetT}"/> hierarchy.
        /// </summary>
        /// <typeparam name="TargetT">
        /// The target type of the <see cref="Value"/> in each new
        /// <see cref="Node"/> of the converted hierarchy.
        /// </typeparam>
        /// <param name="convertValue">
        /// The delegate which converts an instance of the type 
        /// <typeparamref name="T"/> to an instance of the type
        /// <typeparamref name="TargetT"/>.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Node{TargetT}"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="convertValue"/> is null.
        /// </exception>
        /// <exception cref="ApplicationException">
        /// Is thrown when the <paramref name="convertValue"/> delegate
        /// returns a null value.
        /// </exception>
        public Node<TargetT> Convert<TargetT>(
            NodeConvertValue<T, TargetT> convertValue)
            where TargetT : class
        {
            return ConvertRecursively(null, convertValue);
        }

        /// <summary>
        /// Creates a flattened deep copy of the values of the current 
        /// <see cref="Node{T}"/> and all <see cref="Children"/>, converted 
        /// from <typeparamref name="T"/> to <typeparamref name="TargetT"/>.
        /// The process assumes the current <see cref="Node{T}"/> to be the
        /// absolute root, to which the <see cref="Children"/> are handled as
        /// "first-degree relative".
        /// </summary>
        /// <typeparam name="TargetT">
        /// The target type of the <see cref="Value"/> in each new
        /// <see cref="Node{T}"/> of the converted hierarchy.
        /// </typeparam>
        /// <param name="flattenValue">
        /// The delegate which converts an instance of the type 
        /// <typeparamref name="T"/> to an instance of the type
        /// <typeparamref name="TargetT"/>.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Node{TargetT}"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="convertValue"/> is null.
        /// </exception>
        /// <exception cref="ApplicationException">
        /// Is thrown when the <paramref name="flattenValue"/> delegate
        /// returns a null value.
        /// </exception>
        public List<TargetT> Flatten<TargetT>(
            NodeFlattenValue<T, TargetT> flattenValue)
            where TargetT : class
        {
            if (flattenValue == null)
                throw new ArgumentNullException(nameof(flattenValue));

            List<TargetT> flattenedValues = new List<TargetT>();

            FlattenRecursively<TargetT>(null, flattenValue, flattenedValues);

            return flattenedValues;
        }

        private void FlattenRecursively<TargetT>(TargetT parentValue,
            NodeFlattenValue<T, TargetT> flattenNode,
            List<TargetT> values)
            where TargetT : class
        {
            if (flattenNode == null)
                throw new ArgumentNullException(nameof(flattenNode));
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            TargetT currentValue = flattenNode(Value, parentValue);
            if (currentValue == null)
                throw new ApplicationException("The flatten method returned " +
                    "a null value.");

            values.Add(currentValue);

            foreach (Node<T> child in Children)
                child.FlattenRecursively<TargetT>(currentValue, flattenNode,
                    values);
        }

        private Node<TargetT> ConvertRecursively<TargetT>(Node<TargetT> parent,
            NodeConvertValue<T, TargetT> convertValue)
            where TargetT : class
        {
            if (convertValue == null)
                throw new ArgumentNullException(nameof(convertValue));

            TargetT convertedValue = convertValue(Value);
            if (convertedValue == null)
                throw new ApplicationException("The conversion method " +
                    "returned a null value.");

            Node<TargetT> convertedNode;
            if (parent == null)
                convertedNode = new Node<TargetT>(convertedValue);
            else convertedNode = new Node<TargetT>(convertedValue, parent);

            foreach (Node<T> child in Children)
                convertedNode.children.Add(child.ConvertRecursively(
                    convertedNode, convertValue));
            return convertedNode;
        }

        /// <summary>
        /// Creates a deep copy of the current <see cref="Node{T}"/>
        /// and all <see cref="Children"/>. The <see cref="Value"/> of each
        /// node is only copied, not cloned.
        /// The <see cref="Parent"/> node in the cloned instance of this
        /// <see cref="Node{T}"/> is discarded.
        /// </summary>
        /// <returns>
        /// A new instance of the <see cref="Node{T}"/> class.
        /// </returns>
        public Node<T> Clone()
        {
            return CloneRecursively(null);
        }

        private Node<T> CloneRecursively(Node<T> parent)
        {
            Node<T> newNode = new Node<T>(Value, parent);
            foreach (Node<T> child in Children)
                newNode.children.Add(child.CloneRecursively(newNode));
            return newNode;
        }

        /// <summary>
        /// Creates a deep copy of the current <see cref="Node{T}"/>
        /// and all <see cref="Children"/>.
        /// The <see cref="Parent"/> node in the cloned instance of this
        /// <see cref="Node{T}"/> is discarded.
        /// </summary>
        /// <returns>
        /// A new instance of the <see cref="Node{T}"/> class.
        /// </returns>
        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Creates a read-only view or copy of the current 
        /// <see cref="Node{T}"/> and all <see cref="Children"/>.
        /// Modifications of the <see cref="Value"/> of each node are not
        /// affected and might still be possible if the instance class is not
        /// immutable. The <see cref="Parent"/> node in the cloned instance of 
        /// this <see cref="Node{T}"/> is discarded.
        /// </summary>
        /// <param name="clone">
        /// <c>true</c> to create a deep copy of this
        /// <see cref="Node{T}"/> and all <see cref="Children"/> only
        /// with the values of <typeparamref name="T"/> not being
        /// cloned, <c>false</c> to create a "view" of the current
        /// <see cref="Node{T}"/> which changes with the original instance.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Node{T}"/> class.
        /// </returns>
        public Node<T> ToReadOnly(bool clone)
        {
            return new Node<T>(this, clone);
        }

        /// <summary>
        /// Exports the current <see cref="Node{T}"/> and all its
        /// <see cref="Children"/> to a <see cref="byte"/> buffer.
        /// </summary>
        /// <param name="elementExporter">
        /// The function delegate which writes a value of type
        /// <typeparamref name="T"/> to a <see cref="byte"/> array.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="elementExporter"/> is null.
        /// </exception>
        /// <exception cref="ApplicationException">
        /// Is thrown when the <paramref name="elementExporter"/> fails for
        /// one of the elements in the current <see cref="Node{T}"/> 
        /// hierarchy instance.
        /// </exception>
        public byte[] ToBuffer(Func<T, byte[]> elementExporter)
        {
            if (elementExporter == null)
                throw new ArgumentNullException(nameof(elementExporter));

            using (MemoryStream stream = new MemoryStream())
            {
                uint nodeCount = 0;
                TraverseDepthFirst(n => nodeCount++);
                stream.WriteUnsignedInteger(nodeCount);

                Node<T> lastParent = null;
                int lastDepth = 0;

                TraverseBreadthFirst(delegate (Node<T> node)
                {
                    bool nextBranch = false;
                    bool deeper = false;

                    if (lastParent != node.Parent)
                    {
                        lastParent = node.Parent;
                        nextBranch = true;
                    }

                    if (lastDepth < node.Depth)
                    {
                        lastDepth = node.Depth;
                        deeper = true;//Hgnnnn...
                    }

                    stream.WriteBool(nextBranch);
                    stream.WriteBool(deeper);

                    byte[] elementBuffer;
                    try
                    {
                        elementBuffer = elementExporter(node.Value);
                        if (elementBuffer == null)
                            throw new Exception("The exporter returned null.");
                    }
                    catch (Exception exc)
                    {
                        throw new ApplicationException("The export of one " +
                            "of the node values failed.", exc);
                    }

                    stream.WriteBuffer(elementBuffer, true);
                });

                stream.Flush();
                return stream.GetBuffer();
            }
        }

        /// <summary>
        /// Imports a <see cref="Node{T}"/> hierarchy from a <see cref="byte"/>
        /// buffer.
        /// </summary>
        /// <param name="buffer">
        /// The source buffer to read the data from.
        /// </param>
        /// <param name="elementImporter">
        /// The element importer function delegate, which uses a specified
        /// source <see cref="byte"/> buffer to import a single element of the 
        /// type <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="Node{T}"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="buffer"/> or 
        /// <paramref name="elementImporter"/> are null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the data in the stream had an invalid format or
        /// length.
        /// </exception>
        public static Node<T> FromBuffer(byte[] buffer,
            Func<byte[], T> elementImporter)
        {
            Node<T> hierarchy = new Node<T>();
            hierarchy.ImportBuffer(buffer, elementImporter);
            return hierarchy;
        }

        /// <summary>
        /// Imports a <see cref="Node{T}"/> hierarchy from a <see cref="byte"/>
        /// buffer into the current node, overwriting the <see cref="Value"/> 
        /// and adding any parsed child nodes to the <see cref="Children"/> of 
        /// the current instance.
        /// </summary>
        /// <param name="buffer">
        /// The source buffer to read the data from.
        /// </param>
        /// <param name="elementImporter">
        /// The element importer function delegate, which uses a specified
        /// source <see cref="byte"/> buffer to import a single element of the 
        /// type <typeparamref name="T"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="buffer"/> or 
        /// <paramref name="elementImporter"/> are null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the data in the stream had an invalid format or
        /// length.
        /// </exception>
        protected void ImportBuffer(byte[] buffer, 
            Func<byte[], T> elementImporter)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (elementImporter == null)
                throw new ArgumentNullException(nameof(elementImporter));

            if (IsReadOnly) throw ReadOnlyViolationException;

            using (MemoryStream stream = new MemoryStream(buffer))
            {
                T ReadNode(out bool newBranch, out bool deeper)
                {
                    newBranch = stream.ReadBool();
                    deeper = stream.ReadBool();

                    byte[] elementBuffer = stream.ReadBuffer();
                    try
                    {
                        T element = elementImporter(elementBuffer);
                        if (element == null)
                            throw new Exception("The importer returned null.");
                        else return element;
                    }
                    catch (Exception exc)
                    {
                        if (exc is EndOfStreamException)
                            exc = new Exception("The buffer ended before " +
                                "the element could be read completely.");

                        throw new FormatException("The import of one of " +
                            "the elements failed.", exc);
                    }
                }

                uint nodeCount;//The root node isn't included in this count.
                try { nodeCount = stream.ReadUnsignedInteger() - 1; }
                catch (EndOfStreamException)
                {
                    throw new FormatException("The buffer ended before " +
                        "the node count could be read.");
                }

                //Empty, but "valid" buffers won't cause an exception and leave
                //the hierarchy unmodified.
                if (nodeCount < 0) return;

                try { Value = ReadNode(out _, out _); }
                catch (EndOfStreamException)
                {
                    throw new FormatException("The buffer ended before " +
                        "the first node could be read.");
                }
                Node<T> currentParent = this;

                for (int i = 0; i < nodeCount; i++)
                {
                    try
                    {
                        T val = ReadNode(out bool newBranch, out bool deeper);

                        //If deeper is true, newBranch is true as well - 
                        //but going deeper is the right thing to do in
                        //that case and newBranch should be ignored then,
                        //especially when currentParent is root and doesn't
                        //have the required parent for the newBranch routine
                        if (deeper)
                        {
                            if (currentParent.Children.Count > 0)
                                currentParent = currentParent.Children.First();
                            else throw new Exception("Invalid depth " +
                                "instruction for node without children.");
                        }
                        else if (newBranch)
                        {
                            //Try to find the node right next to the 
                            //current node and use that as new current node
                            bool foundCurrent = false;
                            bool updatedToAdjacent = false;
                            foreach (Node<T> adjacentNode in
                                currentParent.Parent.Children)
                            {
                                if (foundCurrent)
                                {
                                    currentParent = adjacentNode;
                                    updatedToAdjacent = true;
                                }
                                if (adjacentNode == currentParent)
                                    foundCurrent = true;
                            }
                            if (foundCurrent)
                                throw new Exception("Current node " +
                                    "wasn't found in parents' children " +
                                    "collection.");
                            if (!updatedToAdjacent)
                                throw new Exception("There were no " +
                                    "nodes \"right\" to the current.");
                        }
                        currentParent.AddChild(val);
                    }
                    catch (Exception exc)
                    {
                        if (exc is EndOfStreamException)
                            exc = new Exception("The buffer ended before " +
                                "the hierarchy could be read completely.");

                        throw new FormatException("Error while loading node " +
                            "#" + i + ".", exc);
                    }
                }
            }
        }
    }
}