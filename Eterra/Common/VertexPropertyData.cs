using System;
using System.Runtime.InteropServices;

namespace Eterra.Common
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
    }
}
