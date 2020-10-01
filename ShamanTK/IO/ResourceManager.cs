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

using ShamanTK.Common;
using ShamanTK.Graphics;
using ShamanTK.Sound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;

namespace ShamanTK.IO
{
    /// <summary>
    /// Provides functionality to import and export application resources and
    /// to load imported data structures into buffers.
    /// </summary>
    /// <remarks>
    /// It's important to understand that neither a image nor a mesh can
    /// be just loaded from a file and then directly drawn. Even though
    /// the functions of this class attempt to make that process as simple as 
    /// possible, it's probably useful to know what happens "behind the 
    /// scenes".
    /// With most resources, the first step is importing the resource file
    /// using a <see cref="IResourceFormatHandler"/> from an 
    /// <see cref="IFileSystem"/> into a data structure (like 
    /// <see cref="MeshData"/> or <see cref="TextureData"/>). Alternatively,
    /// the data structure could also be created programatically. No matter
    /// where it came from, however, the next step is to take that resource 
    /// data instance from the (probably managed) memory into the dark, 
    /// unmanaged depths of your memory banks (like the GPU memory).
    /// This is done by creating a new buffer (like a <see cref="MeshBuffer"/>
    /// or a <see cref="TextureBuffer"/>) which is big enough to hold the
    /// previously loaded resource data, and then start filling the buffer
    /// step by step with the data. This process is automatically done by the
    /// <see cref="ResourceManager"/> after the 
    /// <see cref="ShamanApplicationBase"/> finished redrawing itself. After 
    /// all the data was copied into the buffer, it's completely initialized 
    /// and can be used. 
    /// The state of this operation can always be checked using the
    /// <see cref="SyncTask"/> instance, which was returned upon the start
    /// of the resource loading - adding a so-called "task finalizer" with 
    /// <see cref="SyncTask{T}.AddFinalizer(Action{T}, Action{Exception})"/>
    /// ensures the parts of your application "waiting" for the usable buffer
    /// (or an error) will get notified of the completion of the process.
    /// Additionally to that, the <see cref="LoadingTaskCompleted"/> event
    /// could be used to get notified upon completion of all loading tasks.
    /// Of course, creating a data structure and putting it into a buffer can
    /// be done manually too - it just should be ensured that the buffer was 
    /// completely filled with data to avoid any weird undefined behaviour.
    /// </remarks>
    public class ResourceManager : IDisposable
    {
        #region Internally used task implementations
        /// <summary>
        /// Provides the base class from which classes are derived that perform
        /// a resource loading/buffering task in a 
        /// <see cref="ShamanApplicationBase"/> without blocking update or 
        /// drawing operations.
        /// </summary>
        /// <typeparam name="DataT">
        /// The type of data, which is used to generate a buffer of type
        /// <typeparamref name="BufferT"/>.
        /// </typeparam>
        /// <typeparam name="BufferT">
        /// The buffer, which is generated using a previously loaded or 
        /// provided instance of type <typeparamref name="DataT"/>.
        /// </typeparam>
        /// <remarks>
        /// Every <see cref="BufferSyncTask{DataT, BufferT}"/> has two stages.
        /// The first stage (which can be skipped by providing an instance of
        /// <typeparamref name="DataT"/> for stage two) loads the resource of
        /// type <typeparamref name="DataT"/> in another thread. 
        /// The <see cref="SyncTaskScheduler"/>, which is managed by the 
        /// <see cref="ShamanApplicationBase"/>, continiously initiates a 
        /// state-check in the current <see cref="SyncTask{T}"/>. If the async 
        /// <typeparamref name="DataT"/>-loader-thread is finished 
        /// successfully, the task continues with stage two - the buffering.
        /// The buffering is performed in small steps, which shouldn't take
        /// much time and can be performed alongside the application update
        /// logic without stalling it. If the whole buffer is populated,
        /// the task is finished and the result can be safely retrieved via
        /// <see cref="SyncTask{T}.Value"/> (after checking
        /// <see cref="SyncTask.CurrentState"/>).
        /// </remarks>
#if !USE_LOCKS_IN_BUFFERSYNCTASK
        private abstract class BufferSyncTask<DataT, BufferT>
            : SyncTask<BufferT>
            where DataT : class
            where BufferT : class
        {
            /// <summary>
            /// Defines the priority, with which the
            /// <see cref="dataRetriever"/> thread is executed.
            /// </summary>
            private const ThreadPriority dataRetrieverPriority =
                ThreadPriority.Lowest;

            /// <summary>
            /// The <see cref="Thread"/>, that will load and store a valid
            /// <see cref="DataT"/> instance into
            /// <see cref="dataRetrieverResult"/> and change the
            /// <see cref="dataRetrieverState"/> to
            /// <see cref="SyncTaskState.Finished"/> - or, when the operation
            /// failed, store the ocurred <see cref="Exception"/> into
            /// <see cref="dataRetrieverException"/> and set 
            /// <see cref="dataRetrieverState"/> to
            /// <see cref="SyncTaskState.Failed"/>.
            /// </summary>
            private readonly Thread dataRetriever;

            //The state field is always assigned last, but checked first -
            //with the volatile keyword, this should provide a reliable way
            //of checking whether the data retriever is already finished
            //or not without causing too much overhead or "freezes" in the
            //application loop/logic.
            //However, there's still some testing required to prove that this 
            //will actually not cause any weird behaviour in some rare cases
            //and is really the better alternative (compared to an 
            //implementation using lock(...) {...}).
            private volatile SyncTaskState dataRetrieverState
                = SyncTaskState.Idle;
            private volatile DataT dataRetrieverResult;
            private volatile Exception dataRetrieverException;

            protected ResourceManager ResourceManager { get; }

            /// <summary>
            /// Initializes a new instance of the 
            /// <see cref="BufferSyncTask{DataT, BufferT}"/> class,
            /// which imports a resource of type <typeparamref name="DataT"/> 
            /// and uses that instance to create and incrementally populate a 
            /// buffer of type <typeparamref name="BufferT"/>.
            /// </summary>
            /// <param name="path">
            /// The path, which will be imported as a resource of type
            /// <typeparamref name="DataT"/>.
            /// </param>
            /// <param name="resourceManager">
            /// The <see cref="ResourceManager"/> instance providing access to 
            /// the graphics and sound units which might be needed for buffer
            /// generation.
            /// </param>
            /// <exception cref="ArgumentNullException">
            /// is thrown when <paramref name="resourceManager"/> is null.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// Is thrown when the specified <paramref name="path"/> is not
            /// absolute.
            /// </exception>
            protected BufferSyncTask(ResourcePath path, 
                ResourceManager resourceManager) : base("RESOURCES")
            {
                if (!path.IsAbsolute)
                    throw new ArgumentException("The specified path " +
                        "was not a non-empty, absolute file path.");
                ResourceManager = resourceManager ??
                    throw new ArgumentNullException(nameof(resourceManager));

                dataRetriever = new Thread(delegate ()
                {
                    try
                    {
                        DataT data = resourceManager.ImportResourceFile<DataT>(path);
                        dataRetrieverResult = data;
                        dataRetrieverState = SyncTaskState.Finished;
                    }
                    catch (Exception exc)
                    {
                        dataRetrieverException = new Exception("The " +
                            "resource couldn't be imported.", exc);
                        dataRetrieverState = SyncTaskState.Failed;
                    }
                })
                { Priority = dataRetrieverPriority };
            }

            /// <summary>
            /// Initializes a new instance of the 
            /// <see cref="BufferSyncTask{DataT, BufferT}"/> class,
            /// which executes a <see cref="Func{DataT}"/> in another 
            /// background thread to retrieve the <see cref="DataT"/> instance
            /// required to create and incrementally populate a buffer of type
            /// <typeparamref name="BufferT"/>.
            /// </summary>
            /// <param name="bufferDataRetriever">
            /// The delegate, which will return the 
            /// <typeparamref name="DataT"/> which will be used to create and
            /// populate the buffer. Any exceptions thrown by that delegate
            /// will be caught and cause the current 
            /// <see cref="BufferSyncTask{DataT, BufferT}"/> to fail
            /// in a controlled manner.
            /// </param>
            /// <param name="resourceManager">
            /// The <see cref="ResourceManager"/> instance providing access to 
            /// the graphics and sound units which might be needed for buffer
            /// generation.
            /// </param>
            /// <exception cref="ArgumentNullException">
            /// Is thrown when <paramref name="bufferDataRetriever"/> or
            /// <paramref name="resourceManager"/> are null.
            /// </exception>
            protected BufferSyncTask(Func<DataT> bufferDataRetriever,
                ResourceManager resourceManager) : base(typeof(DataT).Name)
            {
                if (bufferDataRetriever == null)
                    throw new ArgumentNullException(
                        nameof(bufferDataRetriever));
                ResourceManager = resourceManager ??
                    throw new ArgumentNullException(nameof(resourceManager));

                dataRetriever = new Thread(delegate ()
                {
                    try
                    {
                        dataRetrieverResult = bufferDataRetriever();
                        if (dataRetrieverResult == null)
                            throw new Exception("The data retriever for the " +
                                "buffer generation returned null as data.");
                        dataRetrieverState = SyncTaskState.Finished;
                    }
                    catch (Exception exc)
                    {
                        dataRetrieverException = new Exception("The " +
                            "resource couldn't be initialized.", exc);
                        dataRetrieverState = SyncTaskState.Failed;
                    }
                })
                { Priority = dataRetrieverPriority };
            }

            /// <summary>
            /// Executes the generator stepwise by first starting and waiting
            /// for the <typeparamref name="DataT"/> object to be generated
            /// asynchronously, and then by creating and stepwise populating
            /// the new <typeparamref name="BufferT"/> until it's ready to use.
            /// </summary>
            /// <returns>
            /// <c>null</c> until <see cref="SyncTask.CurrentState"/> of this
            /// instance is <see cref="SyncTaskState.Finished"/>, then 
            /// <see cref="SyncTask{T}.Value"/> of this instance is returned
            /// (every time this method is called).
            /// </returns>
            protected override BufferT ContinueGenerator()
            {
                switch (dataRetrieverState)
                {
                    case SyncTaskState.Idle:
                        dataRetrieverState = SyncTaskState.Running;
                        dataRetriever.Start();
                        return null;
                    case SyncTaskState.Running: return null;
                    case SyncTaskState.Failed: throw dataRetrieverException;
                    case SyncTaskState.Finished:
                        try
                        {
                            return ContinueBufferGeneration(
                                dataRetrieverResult);
                        }
                        catch (Exception exc)
                        {
                            throw new Exception("The imported resource " +
                                "couldn't be buffered.", exc);
                        }
                    default:
                        throw new InvalidOperationException("Invalid " +
                            "generator state.");
                }
            }

            /// <summary>
            /// Begins, continues and finishes the initialisation and 
            /// population of a new <typeparamref name="BufferT"/> instance 
            /// using the previously loaded <typeparamref name="DataT"/>
            /// resource.
            /// </summary>
            /// <param name="data">The previously loaded resource data.</param>
            /// <returns></returns>
            /// <exception cref="Exception">
            /// Is thrown when something goes wrong bad enough that the
            /// buffer couldn't be finished properly. Will cause this
            /// <see cref="SyncTask"/> to fail in a controlled way.
            /// </exception>
            protected abstract BufferT ContinueBufferGeneration(DataT data);
        }
#else
        //Alternative BufferSyncTask implementation using locks. 
#warning The current alternative BufferSyncTask implementation is untested!
        private abstract class BufferSyncTask<DataT, BufferT>
            : SyncTask<BufferT>
            where BufferT : class
            where DataT : class
        {
            private readonly Action dataRetriever;
            private bool dataRetrieverInvoked = false;
            private bool dataRetrieverTerminated = false;

            private readonly object dataRetrieverLockObject = new object();
            private DataT dataRetrieverResult;
            private Exception dataRetrieverException;

            protected Platform ParentPlatform { get; }

            protected BufferSyncTask(ResourcePath path, Platform platform)
                : base(path.Path.IsEmpty ? null : path.Path.GetFileName(false))
            {
                if (!path.IsAbsolute)
                    throw new ArgumentException("The specified path " +
                        "was not a non-empty, absolute file path.");
                ParentPlatform = platform ??
                    throw new ArgumentNullException(nameof(platform));

                dataRetriever = delegate ()
                {
                    try
                    {
                        DataT data = platform.Resources.Import<DataT>(path);

                        lock (dataRetrieverLockObject)
                            dataRetrieverResult = data;
                    }
                    catch (Exception exc)
                    {
                        lock (dataRetrieverLockObject)
                            dataRetrieverException = exc;
                    }
                };
            }

            protected BufferSyncTask(Func<DataT> bufferDataRetriever, 
                Platform platform) : base(typeof(DataT).Name)
            {
                if (bufferDataRetriever == null)
                    throw new ArgumentNullException(
                        nameof(bufferDataRetriever));
                ParentPlatform = platform ??
                    throw new ArgumentNullException(nameof(platform));

                dataRetriever = delegate ()
                {
                    DataT dataRetrieverResult = bufferDataRetriever();
                    lock (dataRetrieverLockObject)
                        this.dataRetrieverResult = dataRetrieverResult;
                };
            }

            protected override BufferT ContinueGenerator()
            {
                if (!dataRetrieverInvoked)
                {
                    dataRetrieverInvoked = true;
                    Task.Run(dataRetriever);
                }
                else if (!dataRetrieverTerminated)
                {
                    lock (dataRetrieverLockObject)
                    {
                        if (dataRetrieverResult != null ||
                            dataRetrieverException != null)
                            dataRetrieverTerminated = true;
                    }
                }
                else
                {
                    if (dataRetrieverException != null)
                        throw dataRetrieverException;
                    else return ContinueBufferGeneration(dataRetrieverResult);
                }
                return null;
            }

            protected abstract BufferT ContinueBufferGeneration(DataT data);
        }
#endif

        private class MeshTask : BufferSyncTask<MeshData, MeshBuffer>
        {
            public class BufferGenerator
            {
                private const int VertexBufferSize = 1024;
                private const int FaceBufferSize = 1024;

                private MeshBuffer buffer;
                private int currentVertexOffset = 0,
                    currentFaceOffset = 0;

                public MeshBuffer ContinueBufferGeneration(
                    IGraphics graphics, MeshData data)
                {
                    if (graphics == null)
                        throw new ArgumentNullException(nameof(graphics));
                    if (data == null)
                        throw new ArgumentNullException(nameof(data));

                    if (buffer == null)
                        buffer = graphics.CreateMeshBuffer(
                            data.VertexCount, data.FaceCount,
                            data.VertexPropertyDataFormat);
                    else if (currentVertexOffset < data.VertexCount)
                        buffer.UploadVertices(data, ref currentVertexOffset,
                            VertexBufferSize);
                    else if (currentFaceOffset < data.FaceCount)
                        buffer.UploadFaces(data, ref currentFaceOffset,
                            FaceBufferSize);
                    else return buffer;

                    return null;
                }
            }

            private readonly BufferGenerator bufferGenerator =
                new BufferGenerator();

            public MeshTask(ResourcePath path, ResourceManager resourceManager)
                : base(path, resourceManager) { }

            public MeshTask(MeshData data, ResourceManager resourceManager)
                : base(() => data, resourceManager) { }

            public MeshTask(Func<MeshData> meshDataGenerator,
                ResourceManager resourceManager)
                : base(meshDataGenerator, resourceManager) { }

            protected override MeshBuffer ContinueBufferGeneration(
                MeshData data)
            {
                return bufferGenerator.ContinueBufferGeneration(
                    ResourceManager.graphicsUnit, data);
            }
        }

        private class TextureTask : BufferSyncTask<TextureData, TextureBuffer>
        {
            public class BufferGenerator
            {
                private TextureBuffer buffer;
                private int currentRowOffset = 0;

                private readonly TextureFilter textureFilter;

                public BufferGenerator(TextureFilter textureFilter)
                {
                    if (Enum.IsDefined(typeof(TextureFilter), textureFilter))
                        this.textureFilter = textureFilter;
                    else throw new ArgumentException("The specified texture " +
                        "filter is invalid.");
                }

                public TextureBuffer ContinueBufferGeneration(
                    IGraphics graphics, TextureData data)
                {
                    if (graphics == null)
                        throw new ArgumentNullException(nameof(graphics));
                    if (data == null)
                        throw new ArgumentNullException(nameof(data));

                    if (buffer == null)
                        buffer = graphics.CreateTextureBuffer(data.Size, 
                            textureFilter);
                    else if (currentRowOffset < data.Size.Height)
                        buffer.Upload(data, ref currentRowOffset, 16);
                    else return buffer;

                    return null;
                }
            }

            private readonly BufferGenerator bufferGenerator;

            /// <exception cref="ArgumentException">
            /// Is thrown when the specified <paramref name="textureFilter"/>
            /// is invalid.
            /// </exception>
            public TextureTask(ResourcePath path, 
                ResourceManager resourceManager, TextureFilter textureFilter)
                : base(path, resourceManager)
            {
                bufferGenerator = new BufferGenerator(textureFilter);
            }

            /// <exception cref="ArgumentException">
            /// Is thrown when the specified <paramref name="textureFilter"/>
            /// is invalid.
            /// </exception>
            public TextureTask(TextureData data, 
                ResourceManager resourceManager, TextureFilter textureFilter)
                : base(() => data, resourceManager)
            {
                bufferGenerator = new BufferGenerator(textureFilter);
            }

            protected override TextureBuffer ContinueBufferGeneration(
                TextureData data)
            {
                return bufferGenerator.ContinueBufferGeneration(
                    ResourceManager.graphicsUnit, data);
            }
        }

        private class SoundTask : BufferSyncTask<SoundDataStream, SoundSource>
        {
            public SoundTask(ResourcePath path,
                ResourceManager resourceManager) : base(path, resourceManager)
            { }

            public SoundTask(SoundDataStream data,
                ResourceManager resourceManager)
                : base(() => data, resourceManager) { }

            private SoundSource soundSource;

            protected override SoundSource ContinueBufferGeneration(
                SoundDataStream data)
            {
                if (soundSource == null)
                {
                    soundSource = ResourceManager.soundUnit.CreateSoundSource(
                        data, true);
                    return soundSource;

                }
                else return soundSource;
            }
        }

        private class SceneTask : BufferSyncTask<Scene, Scene>
        {
            public SceneTask(ResourcePath path, 
                ResourceManager resourceManager) : base(path, resourceManager)
            { }

            protected override Scene ContinueBufferGeneration(
                Scene data) => data;
        }

        private class SpriteFontTask
            : BufferSyncTask<SpriteFontData, SpriteFont>
        {
            private readonly MeshTask.BufferGenerator meshGen
                = new MeshTask.BufferGenerator();
            private readonly TextureTask.BufferGenerator textureGen;

            private static readonly MeshData meshData = MeshData.CreatePlane(
                Vector3.Zero, Vector3.UnitY, Vector3.UnitX);

            private MeshBuffer mesh;
            private TextureBuffer texture;

            private SpriteFont spriteFont;

            public SpriteFontTask(ResourcePath path, 
                ResourceManager resourceManager, TextureFilter textureFilter) 
                : base(path, resourceManager)
            {
                textureGen = new TextureTask.BufferGenerator(textureFilter);
            }

            public SpriteFontTask(SpriteFontData data,
                ResourceManager resourceManager, TextureFilter textureFilter)
                : base(() => data, resourceManager)
            {
                textureGen = new TextureTask.BufferGenerator(textureFilter);
            }

            protected override SpriteFont ContinueBufferGeneration(
                SpriteFontData data)
            {
                if (spriteFont != null) return spriteFont;
                else if (mesh == null)
                    mesh = meshGen.ContinueBufferGeneration(
                        ResourceManager.graphicsUnit, meshData);
                else if (texture == null)
                    texture = textureGen.ContinueBufferGeneration(
                        ResourceManager.graphicsUnit, data.Texture);
                else
                {
                    try
                    {
                        return spriteFont = new SpriteFont(mesh, texture,
                            data.CharacterMap, data.SizePx, true);
                    }
                    finally
                    {
                        data.Dispose();
                    }
                }

                return null;
            }
        }
        #endregion

        #region Common exceptions thrown by Platform methods
        private readonly static ArgumentException
            ExceptionRelativeResourcePath = new ArgumentException(
                "The specified path is relative and can't " +
                "automatically be resolved to an absolute path, which " +
                "is required to unambigiously identify a resource file " +
                "in a file system.");
        #endregion

        /// <summary>
        /// Gets the <see cref="FileSystem"/>, on which the current 
        /// <see cref="ResourceManager"/> operates on.
        /// </summary>
        public IFileSystem FileSystem { get; }

        /// <summary>
        /// Gets a boolean which indicates whether the current 
        /// <see cref="ResourceManager"/> supports exporting resources
        /// (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsWritable => FileSystem.IsWritable;

        /// <summary>
        /// Gets the amount of pending resource loading tasks.
        /// </summary>
        public int LoadingTasksPending => loadingTasks.PendingTasks;

        /// <summary>
        /// Occurs after a single loading task was completed (either finished
        /// successfully or failed) and the 
        /// <see cref="PendingLoadingTasksCount"/> property changed.
        /// </summary>
        public event EventHandler LoadingTaskCompleted;

        /// <summary>
        /// Occurs after all loading tasks were completed (either finished
        /// successfully or failed) and the 
        /// <see cref="PendingLoadingTasksCount"/> property changed to 0.
        /// </summary>
        public event EventHandler LoadingTasksCompleted;

        /// <summary>
        /// Occurs after a loading task was initiated and the 
        /// <see cref="PendingLoadingTasksCount"/> property changed.
        /// </summary>
        public event EventHandler LoadingTaskAdded;

        private readonly IResourceFormatHandler[] formatHandlers;
        private readonly SyncTaskScheduler loadingTasks =
            new SyncTaskScheduler();
        private readonly IGraphics graphicsUnit;
        private readonly ISound soundUnit;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceManager"/>
        /// class.
        /// </summary>
        /// <param name="fileSystem">
        /// The file system the new <see cref="ResourceManager"/> should 
        /// operate on.
        /// </param>
        /// <param name="formatHandlers">
        /// An enumeration of <see cref="IResourceFormatHandler"/> instances,
        /// which extend the range of supported formats for import and export.
        /// </param>
        /// <param name="graphicsUnit">
        /// The graphics unit, which should be used to buffer 
        /// graphics resources.
        /// </param>
        /// <param name="soundUnit">
        /// The sound unit, which should be used to buffer sound resources.
        /// Can be null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fileSystem"/>,
        /// <paramref name="formatHandlers"/> or 
        /// <paramref name="graphicsUnit"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="formatHandlers"/> contains
        /// null values, duplicates or an instance of the 
        /// <see cref="NativeFormatHandler"/> class.
        /// </exception>
        internal ResourceManager(IFileSystem fileSystem,
            IEnumerable<IResourceFormatHandler> formatHandlers,
            IGraphics graphicsUnit, ISound soundUnit)
        {
            if (formatHandlers == null)
                throw new ArgumentNullException(nameof(formatHandlers));

            this.graphicsUnit = graphicsUnit ??
                throw new ArgumentNullException(nameof(graphicsUnit));
            this.soundUnit = soundUnit;

            FileSystem = fileSystem ??
                throw new ArgumentNullException(nameof(fileSystem));

            List<IResourceFormatHandler> handlers =
                new List<IResourceFormatHandler>()
                { new NativeFormatHandler(), new BasicFormatsHandler() };
            foreach (IResourceFormatHandler handler in formatHandlers)
            {
                if (handler == null)
                    throw new ArgumentException("The specified enumeration " +
                        "of format handlers contained a null value.");
                else if (handlers.Contains(handler))
                    throw new ArgumentException("The specified enumeration " +
                        "of format handlers contained duplicate values.");
                else if (handler.GetType() == typeof(NativeFormatHandler))
                    throw new ArgumentException("The native format handlers " +
                        "are already present and mustn't be added twice.");
                else handlers.Add(handler);
            }

            this.formatHandlers = handlers.ToArray();

            loadingTasks.LoadingTaskAdded += (s, a) => 
                LoadingTaskAdded?.Invoke(this, a);
            loadingTasks.LoadingTaskCompleted += (s, a) =>
               LoadingTaskCompleted?.Invoke(this, a);
            loadingTasks.LoadingTasksCompleted += (s, a) =>
               LoadingTasksCompleted?.Invoke(this, a);
        }

        /// <summary>
        /// Checks if the current <see cref="ResourceManager"/> supports 
        /// exporting files of a specific type.
        /// </summary>
        /// <param name="fileExtension">
        /// The extension of the file (case insensitive, without any preceding 
        /// periods - just like <see cref="FileSystemPath.GetFileExtension"/>).
        /// </param>
        /// <returns>
        /// <c>true</c> if files of the specified format can be imported with
        /// <see cref="Import(FileSystemPath, string)"/>, <c>false</c> 
        /// otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fileExtension"/> is null.
        /// </exception>
        public bool SupportsImport(string fileExtension)
        {
            if (fileExtension == null)
                throw new ArgumentNullException(nameof(fileExtension));
            try
            {
                GetResourceFormatHandler(fileExtension.ToLowerInvariant(),
                    false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the current <see cref="ResourceManager"/> is writable 
        /// and supports exporting files of a specific type.
        /// </summary>
        /// <param name="fileExtension">
        /// The extension of the file (case insensitive, without any preceding 
        /// periods - just like <see cref="FileSystemPath.GetFileExtension"/>).
        /// </param>
        /// <returns>
        /// <c>true</c> if files of the specified format can be exported with
        /// <see cref="Export(object, FileSystemPath, string, bool)"/> and 
        /// <see cref="IsWritable"/> is true, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fileExtension"/> is null.
        /// </exception>
        public bool SupportsExport(string fileExtension)
        {
            if (fileExtension == null)
                throw new ArgumentNullException(nameof(fileExtension));
            if (!IsWritable) return false;
            try
            {
                GetResourceFormatHandler(fileExtension.ToLowerInvariant(),
                    true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Imports a resource file into a resource data structure, which can
        /// be buffered or used in other use cases afterwards.
        /// </summary>
        /// <param name="path">
        /// The path of the resource which should be imported.
        /// </param>
        /// <returns>
        /// The imported resource as an instance of type 
        /// <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="path"/> is not absolute.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when the file type specified by the file extension in
        /// <paramref name="path"/> is neither supported natively nor by any 
        /// of the available resource handlers.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Is thrown when the <paramref name="path"/> is valid, but couldn't
        /// be resolved into an existing resource of type
        /// <typeparamref name="T"/>. This exception is also thrown when
        /// a resource with the specified <paramref name="path"/> exists,
        /// but has a different <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the specified resource file had an invalid format.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the specified resource file couldn't be accessed
        /// due to an error in the file system.
        /// </exception>
        /// <exception cref="ApplicationException">
        /// Is thrown when the <see cref="IResourceFormatHandler"/> for the
        /// format of the specified resource file failed unexpectedly.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the <see cref="FileSystem"/> of the current
        /// <see cref="ResourceManager"/> instance was disposed and can no
        /// longer be used for importing resources.
        /// </exception>
        public T ImportResourceFile<T>(ResourcePath path)
        {
            if (!path.IsAbsolute)
                throw new ArgumentException("The specified resource path " +
                    "isn't absolute.");

            try
            {
                IResourceFormatHandler formatHandler =
                    GetResourceFormatHandler(path.Path, false);
                T importedResource = formatHandler.Import<T>(this, path);
                if (importedResource == null)
                    throw new ApplicationException("The returned value " +
                        "of the imported object was null.");
                else return importedResource;
            }
            catch (Exception exc)
            {
                if (exc is ArgumentException)
                    throw new ArgumentException("The specified path was " +
                        "invalid in the current context.", exc);
                else if (exc is NotSupportedException)
                    throw new NotSupportedException("The specific format of " +
                        "the requested resource was not supported.", exc);
                else if (exc is FileNotFoundException)
                    throw new FileNotFoundException("The requested resource " +
                        "in the requested type wasn't found.", exc);
                else if (exc is FormatException)
                    throw new FormatException("The format of the requested " +
                        "resource (or parts of it) were invalid.", exc);
                else if (exc is IOException)
                    throw new IOException("The resource couldn't be read " +
                        "completely due to an I/O error.", exc);
                else if (exc is ObjectDisposedException)
                    throw new ObjectDisposedException("The resource file " +
                        "system was closed or disposed.", exc);
                else throw new ApplicationException("The resource format " +
                    "handler failed unexpectedly.", exc);
            }
        }

        /// <summary>
        /// Exports a resource data structure into a resource file.
        /// </summary>
        /// <param name="resource">
        /// The resource data instance to be exported.
        /// </param>
        /// <param name="path">
        /// The <see cref="FileSystemPath"/> of the new resource file.
        /// </param>
        /// <param name="overwrite">
        /// <c>true</c> to overwrite an existing resource with the same
        /// <see cref="ResourcePath"/> (leaving other elements with the same 
        /// <see cref="ResourcePath.Path"/> but a different 
        /// <see cref="ResourcePath.Query"/> unmodified), <c>false</c> to throw
        /// an <see cref="InvalidOperationException"/> a resource with the 
        /// extact same <see cref="ResourcePath"/> already exists.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="resource"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="path"/> is not absolute.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when the file format of the specified file is neither
        /// supported natively nor by any of the available resource handlers.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when <paramref name="overwrite"/> is <c>false</c> and
        /// a resource file with the specified <paramref name="path"/> already 
        /// exists.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the specified resource file couldn't be accessed.
        /// </exception>
        /// <exception cref="ApplicationException">
        /// Is thrown when the <see cref="IResourceFormatHandler"/> for the
        /// format of the specified resource file failed unexpectedly.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the <see cref="FileSystem"/> of the current
        /// <see cref="ResourceManager"/> instance was disposed and can no
        /// longer be used for importing resources.
        /// </exception>
        public void ExportResourceFile(object resource, ResourcePath path, 
            bool overwrite)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            if (!path.IsAbsolute)
                throw new ArgumentException("The specified resource path " +
                    "isn't absolute.");

            //TODO: Add proper exception handling like in the Import method.
            try
            {
                IResourceFormatHandler formatHandler =
                    GetResourceFormatHandler(path.Path, true);
                formatHandler.Export(resource, this, path, overwrite);
            }
            catch (ArgumentException) { throw; }
            catch (NotSupportedException) { throw; }
            catch (ObjectDisposedException) { throw; }
            catch (InvalidOperationException) { throw; }
            catch (IOException) { throw; }
            catch (Exception exc)
            {
                throw new ApplicationException("The format handler " +
                    "failed unexpectedly during the export of a " +
                    "resource.", exc);
            }
        }

        /// <summary>
        /// Begins loading a mesh into graphic memory, which can be used for 
        /// drawing on the <see cref="Canvas"/> in the <see cref="Redraw"/>
        /// event once the loading process is finished.
        /// </summary>
        /// <param name="data">
        /// The data of the mesh.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{MeshBuffer}"/> instance, which is 
        /// managed by the current <see cref="ShamanTK"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="data"/> is null.
        /// </exception>
        /// <remarks>
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<MeshBuffer> LoadMesh(MeshData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            SyncTask<MeshBuffer> task = new MeshTask(data, this);
            loadingTasks.Enqueue(task);
            return task;
        }

        /// <summary>
        /// Begins loading a mesh into graphic memory, which can be used for 
        /// drawing on the <see cref="Canvas"/> in the <see cref="Redraw"/>
        /// event once the loading process is finished.
        /// </summary>
        /// <param name="dataGenerator">
        /// A delegate to a method that returns a valid <see cref="MeshData"/>
        /// instance to be buffered. This delegate will be executed in a 
        /// seperate thread.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{MeshBuffer}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="dataGenerator"/> is null.
        /// </exception>
        /// <remarks>
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<MeshBuffer> LoadMesh(Func<MeshData> dataGenerator)
        {
            if (dataGenerator == null)
                throw new ArgumentNullException(nameof(dataGenerator));

            SyncTask<MeshBuffer> task = new MeshTask(dataGenerator, this);
            loadingTasks.Enqueue(task);
            return task;
        }

        /// <summary>
        /// Begins loading a mesh into graphic memory, which can be used for 
        /// drawing on the <see cref="Canvas"/> in the <see cref="Redraw"/>
        /// event once the loading process is finished.
        /// </summary>
        /// <param name="meshPath">
        /// The path to the data of the mesh.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{MeshBuffer}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="meshPath"/> is not absolute.
        /// </exception>
        /// <remarks>
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<MeshBuffer> LoadMesh(ResourcePath meshPath)
        {
            if (!meshPath.IsAbsolute) throw ExceptionRelativeResourcePath;

            SyncTask<MeshBuffer> task = new MeshTask(meshPath, this);
            loadingTasks.Enqueue(task);
            return task;
        }

        /// <summary>
        /// Begins loading a texture into graphic memory, which can be used for 
        /// drawing on the <see cref="Canvas"/> in the <see cref="Redraw"/>
        /// event once the loading process is finished.
        /// </summary>
        /// <param name="data">
        /// The data of the texture.
        /// </param>
        /// <param name="textureFilter">
        /// The texture filter which will be used to interpolate the texture
        /// when drawn.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{TextureBuffer}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="data"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="textureFilter"/> 
        /// is invalid.
        /// </exception>
        /// <remarks>
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<TextureBuffer> LoadTexture(TextureData data,
            TextureFilter textureFilter = TextureFilter.Linear)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            SyncTask<TextureBuffer> task = new TextureTask(data, this, 
                textureFilter);
            loadingTasks.Enqueue(task);
            return task;
        }

        /// <summary>
        /// Begins loading a texture into graphic memory, which can be used for 
        /// drawing on the <see cref="Canvas"/> in the <see cref="Redraw"/>
        /// event once the loading process is finished.
        /// </summary>
        /// <param name="texturePath">
        /// The path to the data of the texture.
        /// </param>
        /// <param name="textureFilter">
        /// The texture filter which will be used to interpolate the texture
        /// when drawn.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{TextureBuffer}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="texturePath"/> is not absolute or
        /// when the specified <paramref name="textureFilter"/> is invalid.
        /// </exception>
        /// <remarks>
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<TextureBuffer> LoadTexture(ResourcePath texturePath,
            TextureFilter textureFilter = TextureFilter.Linear)
        {
            if (!texturePath.IsAbsolute) throw ExceptionRelativeResourcePath;

            SyncTask<TextureBuffer> task = new TextureTask(texturePath, this,
                textureFilter);
            loadingTasks.Enqueue(task);
            return task;
        }

        /// <summary>
        /// Begins loading a sound into memory, which can be played back
        /// once it's loaded.
        /// </summary>
        /// <param name="dataStream">
        /// The data of the sound.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{TextureBuffer}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="dataStream"/> is null.
        /// </exception>
        /// <remarks>
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<SoundSource> LoadSound(SoundDataStream dataStream)
        {
            if (dataStream == null)
                throw new ArgumentNullException(nameof(dataStream));

            SyncTask<SoundSource> task = new SoundTask(dataStream, this);
            loadingTasks.Enqueue(task);
            return task;
        }

        /// <summary>
        /// Begins loading a sound into memory, which can be played back
        /// once it's loaded.
        /// </summary>
        /// <param name="soundPath">
        /// The path to the data of the sound.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{SoundSource}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="soundPath"/> is not absolute.
        /// </exception>
        /// <remarks>
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<SoundSource> LoadSound(ResourcePath soundPath)
        {
            if (!soundPath.IsAbsolute) throw ExceptionRelativeResourcePath;

            SyncTask<SoundSource> task = new SoundTask(soundPath, this);
            loadingTasks.Enqueue(task);
            return task;
        }

        /// <summary>
        /// Begins converting a "normal" system-installed font into a sprite 
        /// font with <see cref="FontRasterizationParameters.Default"/> and 
        /// loads it into graphic memory, which can be used to create a 
        /// <see cref="SpriteText"/>, that can then be drawn to a 
        /// <see cref="Canvas"/> in the <see cref="Redraw"/> event once 
        /// the loading process is finished.
        /// </summary>
        /// <param name="fontFamily">
        /// The font family name of the installed system font, which is 
        /// combined with the <see cref="FileSystemPath"/> alias defined by
        /// <see cref="FontRasterizationParameters.SystemFontFileAlias"/> and 
        /// the <see cref="ResourceQuery"/> generated by 
        /// <paramref name="fontStyle"/> to form a valid 
        /// <see cref="ResourcePath"/>, which is then used with the
        /// <see cref="LoadSpriteFont(ResourcePath, TextureFilter)"/>.
        /// </param>
        /// <param name="textureFilter">
        /// The texture filter which will be used to interpolate the texture
        /// when drawn.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{SpriteFont}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="fontPath"/> is not absolute or when
        /// the specified <paramref name="textureFilter"/> is invalid.
        /// </exception>
        /// <remarks>
        /// After converting the specified parameters into a 
        /// <see cref="ResourcePath"/> utilizing the functionality of 
        /// <see cref="FontRasterizationParameters"/>, this method uses the 
        /// <see cref="LoadSpriteFont(ResourcePath, TextureFilter)"/> method
        /// with a special <see cref="ResourcePath"/> to support importing
        /// system fonts, if the current <see cref="ResourceManager"/> contains
        /// a <see cref="IResourceFormatHandler"/> that supports that by 
        /// providing support for the file extension for the alias file
        /// specified in 
        /// <see cref="FontRasterizationParameters.SystemFontFileAlias"/>.
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<SpriteFont> LoadGenericFont(string fontFamily,
            TextureFilter textureFilter = TextureFilter.Linear)
        {
            if (fontFamily == null)
                throw new ArgumentNullException(nameof(fontFamily));

            return LoadSpriteFont(
                FontRasterizationParameters.Default.ToSystemFontResourcePath(
                fontFamily), textureFilter);
        }

        /// <summary>
        /// Begins converting a "normal" system-installed font into a sprite 
        /// font and loads it into graphic memory, which can be 
        /// used to create a <see cref="SpriteText"/>, that can then be drawn 
        /// to a <see cref="Canvas"/> in the <see cref="Redraw"/> event once 
        /// the loading process is finished.
        /// </summary>
        /// <param name="fontFamily">
        /// The font family name of the installed system font, which is 
        /// combined with the <see cref="FileSystemPath"/> alias defined by
        /// <see cref="FontRasterizationParameters.SystemFontFileAlias"/> and 
        /// the <see cref="ResourceQuery"/> generated by 
        /// <paramref name="rasterizationParameters"/> to form a valid 
        /// <see cref="ResourcePath"/>, which is then used with the
        /// <see cref="LoadSpriteFont(ResourcePath, TextureFilter)"/>.
        /// </param>
        /// <param name="rasterizationParameters">
        /// The style parameters, which are converted to a standardized 
        /// <see cref="ResourceQuery"/> and combined with the provided
        /// <paramref name="fontFilePath"/> to form a valid
        /// <see cref="ResourcePath"/>, which is then used with the
        /// <see cref="LoadSpriteFont(ResourcePath, TextureFilter)"/>
        /// method.
        /// </param>
        /// <param name="textureFilter">
        /// The texture filter which will be used to interpolate the texture
        /// when drawn.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{SpriteFont}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="rasterizationParameters"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="fontPath"/> is not absolute or when
        /// the specified <paramref name="textureFilter"/> is invalid.
        /// </exception>
        /// <remarks>
        /// After converting the specified parameters into a 
        /// <see cref="ResourcePath"/> utilizing the functionality of 
        /// <see cref="FontRasterizationParameters"/>, this method uses the 
        /// <see cref="LoadSpriteFont(ResourcePath, TextureFilter)"/> method
        /// with a special <see cref="ResourcePath"/> to support importing
        /// system fonts, if the current <see cref="ResourceManager"/> contains
        /// a <see cref="IResourceFormatHandler"/> that supports that by 
        /// providing support for the file extension for the alias file
        /// specified in 
        /// <see cref="FontRasterizationParameters.SystemFontFileAlias"/>.
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<SpriteFont> LoadGenericFont(string fontFamily,
            FontRasterizationParameters rasterizationParameters, 
            TextureFilter textureFilter = TextureFilter.Linear)
        {
            if (fontFamily == null)
                throw new ArgumentNullException(nameof(fontFamily));
            if (rasterizationParameters == null)
                throw new ArgumentNullException(
                    nameof(rasterizationParameters));

            return LoadSpriteFont(
                rasterizationParameters.ToSystemFontResourcePath(fontFamily), 
                textureFilter);
        }

        /// <summary>
        /// Begins converting a "normal" font into a sprite font using
        /// <see cref="FontRasterizationParameters.Default"/> and loads it 
        /// into graphic memory, which can be used to create a 
        /// <see cref="SpriteText"/>, that can then be drawn to a 
        /// <see cref="Canvas"/> in the <see cref="Redraw"/> event once the 
        /// loading process is finished.
        /// </summary>
        /// <param name="fontPath">
        /// The path to the generic font data resource.
        /// </param>
        /// <param name="textureFilter">
        /// The texture filter which will be used to interpolate the texture
        /// when drawn.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{SpriteFont}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="fontPath"/> is not absolute or when
        /// the specified <paramref name="textureFilter"/> is invalid.
        /// </exception>
        /// <remarks>
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<SpriteFont> LoadGenericFont(FileSystemPath fontPath,
            TextureFilter textureFilter = TextureFilter.Linear)
        {
            if (!fontPath.IsAbsolute) throw ExceptionRelativeResourcePath;

            return LoadSpriteFont(
                FontRasterizationParameters.Default.ToFontFileResourcePath(
                    fontPath), textureFilter);
        }

        /// <summary>
        /// Begins converting a "normal" font into a sprite font and loads it 
        /// into graphic memory, which can be used to create a 
        /// <see cref="SpriteText"/>, that can then be drawn to a 
        /// <see cref="Canvas"/> in the <see cref="Redraw"/> event once the 
        /// loading process is finished.
        /// </summary>
        /// <param name="fontPath">
        /// The path to the generic font data resource.
        /// </param>
        /// <param name="rasterizationParameters">
        /// The style parameters, which are converted to a standardized 
        /// <see cref="ResourceQuery"/> and combined with the provided
        /// <paramref name="fontFilePath"/> to form a valid
        /// <see cref="ResourcePath"/>, which is then used with the
        /// <see cref="LoadSpriteFont(ResourcePath, TextureFilter)"/>
        /// method.
        /// </param>
        /// <param name="textureFilter">
        /// The texture filter which will be used to interpolate the texture
        /// when drawn.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{SpriteFont}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="rasterizationParameters"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="fontPath"/> is not absolute or when
        /// the specified <paramref name="textureFilter"/> is invalid.
        /// </exception>
        /// <remarks>
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<SpriteFont> LoadGenericFont(FileSystemPath fontPath,
            FontRasterizationParameters rasterizationParameters, 
            TextureFilter textureFilter = TextureFilter.Linear)
        {
            if (rasterizationParameters == null)
                throw new ArgumentNullException(
                    nameof(rasterizationParameters));

            if (!fontPath.IsAbsolute) throw ExceptionRelativeResourcePath;

            return LoadSpriteFont(
                rasterizationParameters.ToFontFileResourcePath(fontPath), 
                textureFilter);
        }

        /// <summary>
        /// Begins loading a sprite font and the associated mesh and texture 
        /// data into graphic memory, which can be used to create a
        /// <see cref="SpriteText"/>, which can then be drawn to a 
        /// <see cref="Canvas"/> in the <see cref="Redraw"/> event once the 
        /// loading process is finished.
        /// </summary>
        /// <param name="fontPath">
        /// The path to the sprite font data resource.
        /// </param>
        /// <param name="textureFilter">
        /// The texture filter which will be used to interpolate the texture
        /// when drawn.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{SpriteFont}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="fontPath"/> is not absolute or when
        /// the specified <paramref name="textureFilter"/> is invalid.
        /// </exception>
        /// <remarks>
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<SpriteFont> LoadSpriteFont(ResourcePath fontPath,
            TextureFilter textureFilter = TextureFilter.Linear)
        {
            if (!fontPath.IsAbsolute) throw ExceptionRelativeResourcePath;

            SyncTask<SpriteFont> task = new SpriteFontTask(fontPath, this,
                textureFilter);
            loadingTasks.Enqueue(task);
            return task;
        }

        /// <summary>
        /// Begins loading a sprite font and the associated mesh and texture 
        /// data into graphic memory, which can be used to create a
        /// <see cref="SpriteText"/>, which can be drawn to a 
        /// <see cref="Canvas"/> in the <see cref="Redraw"/> event once the 
        /// loading process is finished.
        /// </summary>
        /// <param name="data">
        /// The data of the sprite font. Members with a resource path but 
        /// without resource values are ignored.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{SpriteFont}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="data"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="textureFilter"/> 
        /// is invalid.
        /// </exception>
        /// <remarks>
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// </remarks>
        public SyncTask<SpriteFont> LoadSpriteFont(SpriteFontData data,
            TextureFilter textureFilter = TextureFilter.Linear)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            SyncTask<SpriteFont> task = new SpriteFontTask(data, this, 
                textureFilter);
            loadingTasks.Enqueue(task);
            return task;
        }

        /// <summary>
        /// Begins loading a scene and all referenced resources.
        /// The resources are not buffered by this method.
        /// The task isn't started instantly, but at the end 
        /// of the current <see cref="Update"/> cycle (after the 
        /// <see cref="Starting"/> event). All of the <see cref="SyncTask{T}"/>
        /// events and property changes occur in the same thread as the
        /// events of this <see cref="ShamanApplicationBase"/>.
        /// Requires the current platform to have a 
        /// <see cref="IO.ResourceManager"/>.
        /// </summary>
        /// <param name="scenePath">
        /// The path to the scene resource.
        /// </param>
        /// <returns>
        /// A new <see cref="SyncTask{Scene}"/> instance, which is 
        /// managed by the current <see cref="ShamanApplicationBase"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="scenePath"/> is not absolute.
        /// </exception>
        public SyncTask<Scene> LoadScene(ResourcePath scenePath)
        {
            if (!scenePath.IsAbsolute) throw ExceptionRelativeResourcePath;

            SyncTask<Scene> task = new SceneTask(scenePath, this);
            loadingTasks.Enqueue(task);
            return task;
        }

        /// <summary>
        /// Opens an existing file in the current <see cref="FileSystem"/>.
        /// </summary>
        /// <param name="filePath">
        /// An absolute <see cref="FileSystemPath"/> specifying the file which
        /// should be opened.
        /// </param>
        /// <param name="requestWriteAccess">
        /// <c>true</c> to open the file with read-write access,
        /// <c>false</c> (default) to open the file with read-only access.
        /// </param>
        /// <returns>The <see cref="Stream"/> to the file.</returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <see cref="FileSystemPath.IsEmpty"/> or
        /// <see cref="FileSystemPath.IsDirectoryPath"/> of 
        /// <paramref name="filePath"/> are <c>true</c> or if 
        /// <see cref="FileSystemPath.IsAbsolute"/> of 
        /// <paramref name="filePath"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Is thrown when no file at the specified 
        /// <paramref name="filePath"/> was found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <paramref name="requestWriteAccess"/> is 
        /// <c>true</c>, but <see cref="IsWritable"/> is <c>false</c>.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when the underlying operating system failed to perform
        /// the requested action.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current file system is disposed and can't be
        /// used anymore.
        /// </exception>
        public Stream OpenFile(FileSystemPath filePath, 
            bool requestWriteAccess = false)
        {
            return FileSystem.OpenFile(filePath, requestWriteAccess);
        }

        /// <summary>
        /// Continues the processing of the tasks in the current 
        /// <see cref="ResourceManager"/> instance for a specific amount 
        /// of time.
        /// </summary>
        /// <param name="timeout">
        /// The intended amount of time available to continue a single task.
        /// </param>
        /// <returns>
        /// <c>true</c> if the task was started/continued without errors and
        /// should be invoked another time to continue the task,
        /// <c>false</c> if the task finished/failed in this step or in another
        /// step before and this method of this instance shouldn't be 
        /// invoked anymore.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="timeout"/> is zero or negative.
        /// </exception>
        internal bool ContinueTasks(TimeSpan timeout)
        {
            return loadingTasks.Continue(timeout);
        }

        /// <summary>
        /// Verifies a <see cref="FileSystemPath"/> and returns the first
        /// available <see cref="IResourceFormatHandler"/> which supports
        /// the files' extension.
        /// </summary>
        /// <param name="filePath">
        /// The path of the file which should be verified and requires a 
        /// compatible <see cref="IResourceFormatHandler"/>.
        /// </param>
        /// <param name="requireExport">
        /// <c>true</c> if an <see cref="IResourceFormatHandler"/> should be
        /// returned which supports exporting, <c>false</c> to return one
        /// which supports importing of the file type defined by
        /// <paramref name="filePath"/>.
        /// </param>
        /// <returns>
        /// A reference to an <see cref="IResourceFormatHandler"/> instance.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="filePath"/> is empty, 
        /// a directory instead of a file path or the file has no extension.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when the file format of the specified file is neither
        /// supported natively nor by any of the available resource handlers.
        /// </exception>
        private IResourceFormatHandler GetResourceFormatHandler(
            FileSystemPath filePath, bool requireExport)
        {
            try { filePath.Verify(false); }
            catch (ArgumentException) { throw; }

            try
            {
                return GetResourceFormatHandler(filePath.GetFileExtension(),
                    requireExport);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("The file path doesn't contain " +
                    "a file extension which can be used to determine a " +
                    "compatible format handler.");
            }
            catch (NotSupportedException) { throw; }
        }

        /// <summary>
        /// Gets the first available <see cref="IResourceFormatHandler"/> 
        /// which supports a specific file type.
        /// </summary>
        /// <param name="fileExtension">
        /// The file extension as lowercase string without any preceding
        /// period.
        /// </param>
        /// <param name="requireExport">
        /// <c>true</c> if an <see cref="IResourceFormatHandler"/> should be
        /// returned which supports exporting, <c>false</c> to return one
        /// which supports importing of the file type defined by
        /// <paramref name="filePath"/>.
        /// </param>
        /// <returns>
        /// A reference to an <see cref="IResourceFormatHandler"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fileExtension"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="fileExtension"/> is empty or
        /// whitespaces only.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Is thrown when the file format of the specified file is neither
        /// supported natively nor by any of the available resource handlers.
        /// </exception>
        private IResourceFormatHandler GetResourceFormatHandler(
            string fileExtension, bool requireExport)
        {
            if (fileExtension == null)
                throw new ArgumentNullException(nameof(fileExtension));
            if (fileExtension.Trim().Length == 0)
                throw new ArgumentException("No file format extension was " +
                    "specified which could be used to determine a " +
                    "suitable format handler.");

            IResourceFormatHandler formatHandler = null;
            foreach (IResourceFormatHandler formatHandlerCandidate
                in formatHandlers)
            {
                if ((!requireExport &&
                    formatHandlerCandidate.SupportsImport(fileExtension)) ||
                    (requireExport &&
                    formatHandlerCandidate.SupportsExport(fileExtension)))
                {
                    formatHandler = formatHandlerCandidate;
                    break;
                }
            }
            if (formatHandler == null)
            {
                if (fileExtension == 
                    FontRasterizationParameters.SystemFontFileAliasFormat)
                    throw new NotSupportedException("No format handler for " +
                        "system font files was found.");
                else throw new NotSupportedException("No format handler for " +
                    "the resource format/extension '" + fileExtension.Clamp() +
                    "' was found.");
            }
            else return formatHandler;
        }

        /// <summary>
        /// Disposes the <see cref="FileSystem"/> of the current
        /// <see cref="ResourceManager"/> instance. Depending on the 
        /// implementation of the used <see cref="IO.FileSystem"/>, this
        /// operation may or may not have any effect on whether the resource
        /// manager can be used any longer.
        /// </summary>
        public void Dispose()
        {
            FileSystem.Dispose();
        }
    }
}
