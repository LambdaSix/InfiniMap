using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace InfiniMap
{
    /// <summary>
    /// An abstract base class for providing chunked item storage in a 3 dimension grid.
    /// The user need not be intimately aware that the map is chunked.
    /// </summary>
    /// <remarks>
    /// There are three coordinate systems in use, chunk, item, and world.
    /// 
    ///     Chunk-Space, A coordinate of a chunk among other chunks, the center of the world is chunk (0,0,0)
    ///                 the chunk sitting on top of that to it would be (0,0,1)
    /// 
    ///     World-Space, A coordinate of an item among other items, the center of the world is (0,0,0) and
    ///                 an item directly ontop of it would be (0,0,1). An item 63 tiles away on the Y plane would be
    ///                 (0,63,1)
    /// 
    ///     Item-Space, A coordinate of an item inside a block, translated from world-space. The item at (worldspace) (0,0,1)
    ///                 exists in the chunk space of (0,0,0) and the block space of (0,0,1).
    ///                 An item at (63,0,0) in the world exists in chunkspace at (3,0,0) and itemspace of (15,0,0)
    /// 
    /// </remarks>
    /// <typeparam name="T">Type of item to store in this collection</typeparam>
    public abstract class ChunkMap<T>
    {
        private readonly int _chunkHeight;
        private readonly int _chunkWidth;
        private readonly int _chunkDepth;
        private readonly Dictionary<ChunkSpace, Chunk<T>> _map;

        private Action<ChunkSpace, IEnumerable<T>> _writerFunc;
        private Func<ChunkSpace, IEnumerable<T>> _readerFunc; 

        public ChunkMap(int chunkHeight, int chunkWidth, int chunkDepth)
        {
            if (chunkHeight > 255 || chunkWidth > 255 || chunkDepth > 255)
                throw new ArgumentException("Dimensions of a chunk cannot be larger than 255x255x255");

            _chunkHeight = chunkHeight;
            _chunkWidth = chunkWidth;
            _chunkDepth = chunkDepth;
            _map = new Dictionary<ChunkSpace, Chunk<T>>(8);
        }

        public int Count { get { return _map.Values.Sum(c => c.Count); } }


        protected virtual T this[WorldSpace coordinates]
        {
            get { return Get(coordinates); }
            set { Put(coordinates, value); }
        }

        protected virtual T this[long x, long y, long z]
        {
            get { return Get(new WorldSpace(x, y, z)); }
            set { Put(new WorldSpace(x, y, z), value); }
        }

        /// <summary>
        /// Return all loaded chunks
        /// </summary>
        /// <remarks>
        /// The (x,y,z) are chunk-space, not world-space
        /// </remarks>
        /// <returns>A dictionary of Chunk Coordinates => Chunk </returns>
        protected Dictionary<ChunkSpace, Chunk<T>> All() { return _map; }

        /// <summary>
        /// Return all loaded chunks given the predicate
        /// </summary>
        /// <remarks>
        /// The returned (x,y,z) are chunk-space, not world-space
        /// </remarks>
        /// <param name="predicate">Filter function to apply</param>
        /// <returns>A 4-Tuple of (x,y,z,Chunk{T}) filtered by the predicate function</returns>
        protected IEnumerable<KeyValuePair<ChunkSpace, Chunk<T>>> All(Func<(ChunkSpace Coordinate, Chunk<T> ChunkRef), bool> predicate)
        {
            return All().Where(item => predicate((item.Key, item.Value)));
        }

        /// <summary>
        /// Start writing chunks to disk, calling <paramref name="writeFunc"/> for every chunk of blocks, with
        /// that chunk passed to the callback.
        /// </summary>
        /// <remarks>
        /// <paramref name="writeFunc"/> will be called once for every chunk in memory, it gets passed
        /// a chunks worth of {T} each time, along with the chunk coordinates as an 3-tuple of (x,y,z)
        /// </remarks>
        /// <param name="writeFunc">Serialization function to use</param>
        public void Write(Action<ChunkSpace, IEnumerable<T>> writeFunc)
        {
            foreach (var chunk in All())
            {   // chunk.Value is a Chunk<T> which is IEnumerable<T>
                writeFunc(chunk.Key, chunk.Value);
            }
        }

        /// <summary>
        /// Register a callback for writing of {T}. Replace any existing callback.
        /// </summary>
        /// <remarks>
        /// Like the Write function, the callback will be called when a chunk is to be saved to disk.
        /// This is normally when the chunk is about to be unloaded from memory, giving the application
        /// a chance to persist the chunk to disk.
        /// </remarks>
        /// <param name="writerFunc">Serialization function use - (chunkData, (x,y,z))</param>
        public void RegisterWriter(Action<ChunkSpace, IEnumerable<T>> writerFunc)
        {
            _writerFunc = writerFunc;
        }

        /// <summary>
        /// Unregister the writer callback.
        /// </summary>
        public void UnregisterWriter()
        {
            _writerFunc = null;
        }

        /// <summary>
        /// Register a call back for reading chunks in when they aren't found in memory.
        /// Return an empty list to create a new empty chunk.
        /// </summary>
        /// <remarks>
        /// The 
        /// </remarks>
        /// <param name="readerFunc"></param>
        public void RegisterReader(Func<ChunkSpace, IEnumerable<T>> readerFunc )
        {
            _readerFunc = readerFunc;
        }

        /// <summary>
        /// Unregister the reader callback.
        /// </summary>
        public void UnregisterReader()
        {
            _readerFunc = null;
        }

        /// <summary>
        /// Read a chunk using the reader function, or a blank block.
        /// </summary>
        /// <param name="coordinates">3-Tuple of coordinates of chunk to read</param>
        /// <returns>Chunk filled with T</returns>
        private Chunk<T> ReadChunk(ChunkSpace coordinates)
        {
            if (_readerFunc != null)
            {
                var items = _readerFunc(coordinates).ToList();

                if (items.Count > (_chunkHeight*_chunkWidth*_chunkDepth))
                {
                    throw new NotSupportedException("Attempted to load a item block larger than this Maps chunk dimensions");
                }

                return new Chunk<T>(_chunkHeight, _chunkWidth, _chunkDepth, items);
            }

            // Without a reader function, just return a blank chunk.
            return new Chunk<T>(_chunkHeight, _chunkWidth, _chunkDepth);
        }

        /// <summary>
        /// Write a chunk using the write function, if defined.
        /// </summary>
        /// <param name="coordinates">3-Tuple of coordinates of the chunk to write</param>
        /// <param name="chunk">The chunk to write</param>
        private void WriteChunk(ChunkSpace coordinates, Chunk<T> chunk)
        {
            _writerFunc?.Invoke(coordinates, chunk);
        }

        public bool Contains(T item)
        {
            return _map.Values.Any(chunk => chunk.Contains(item));
        }

        public bool Contains(T item, EqualityComparer<T> comp )
        {
            return _map.Values.Any(chunk => chunk.Contains(item, comp));
        }

        public IEnumerable<T> Within(WorldSpace begin, WorldSpace end)
        {
            for (long x = begin.X; x <= end.X; x++)
            {
                for (long y = begin.Y; y <= end.Y; y++)
                {
                    for (long z = begin.Z; z <= end.Z; z++)
                    {
                        yield return this[x, y, z];
                    }
                }
            }
        }

        /// <summary>
        /// Return all chunks within a given 2D world-space region.
        /// If <paramref name="createIfNull"/> is true, then all returned regions will be initialized to some value or
        /// possibly generated by user-callbacks.
        /// If <paramref name="createIfNull"/> is false, then any chunks not currently in memory will be returned as null, no
        /// attempt to generate or load the chunks with user-callbacks is made.
        /// </summary>
        /// <param name="x0">Starting X position</param>
        /// <param name="y0">Starting Y position</param>
        /// <param name="x1">Ending X position</param>
        /// <param name="y1">Ending Y position</param>
        /// <param name="createIfNull">If false, do not create new chunks when a chunk is not currently loaded into memory</param>
        /// <returns>
        /// A list of 3-Tuples, containing the starting coordinates of the chunk, plus the chunk itself
        /// as: (x,y,Chunk{T})
        /// </returns>
        protected virtual IEnumerable<Tuple<WorldSpace, Chunk<T>>> ChunksWithin(long x0, long y0, long x1, long y1, bool createIfNull)
        {
            var begin = new WorldSpace(x0, y0, 0);
            var end = new WorldSpace(x1, y1, 0);
            return ChunksWithin(begin, end, createIfNull);
        }

        /// <summary>
        /// Returns all chunks within a given 3D world-space region.
        /// If <paramref name="createIfNull"/> is true, then all returned regions will be initialized to some value or
        /// possibly generated by user-callbacks.
        /// If <paramref name="createIfNull"/> is false, then any chunks not currently in memory will be returned as null, no
        /// attempt to generate or load the chunks with user-callbacks is made.
        /// </summary>
        /// <param name="createIfNull">If false, do not create new chunks when a chunk is not currently loaded into memory</param>
        /// <returns>
        /// A list of pairs, containing the starting coordinates of the chunk, plus the chunk itself
        /// as: (WorldSpace,Chunk{T})
        /// </returns>
        protected virtual IEnumerable<Tuple<WorldSpace, Chunk<T>>> ChunksWithin(WorldSpace begin, WorldSpace end,
            bool createIfNull)
        {
            var c0 = TranslateWorldToChunk(begin);
            var c1 = TranslateWorldToChunk(end);

            long x0 = c0.X * _chunkWidth;
            long x1 = c1.X * _chunkWidth;

            long y0 = c0.Y * _chunkHeight;
            long y1 = c1.Y * _chunkHeight;

            long z0 = c0.Z * _chunkDepth;
            long z1 = c1.Z * _chunkDepth;

            //begin with origin block and step over worldspace in chunk sized steps yielding the chunk tuple.
            var xyzPoints = Range(x0, x1, _chunkWidth)
                .SelectMany(x => Range(y0, y1, _chunkHeight)
                    .SelectMany(y => Range(z0, z1, _chunkDepth)
                        .Select(z => new WorldSpace(x, y, z))))
                .Select(point => Tuple.Create(point, GetChunk(point, createIfNull)))
                .Where(c => c.Item2 != null);

            // Ensure chunk creation
            return createIfNull ? xyzPoints.ToList() : xyzPoints;
        }

        private IEnumerable<long> Range(long from, long to, long step)
        {
            if (step <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(step), "Step size must be greater than zero.");
            }

            if (from <= to)
            {
                for (long el = from; el <= to; el += step)
                {
                    yield return el;
                }
            }
            else
            {
                for (long el = from; el >= to; el -= step)
                {
                    yield return el;
                }
            }
        }

        protected ChunkSpace TranslateWorldToChunk(WorldSpace worldSpace)
        {
            var xChunk = (long)Math.Floor(worldSpace.X / (float)_chunkHeight);
            var yChunk = (long)Math.Floor(worldSpace.Y / (float)_chunkWidth);
            var zChunk = (long)Math.Floor(worldSpace.Z / (float)_chunkDepth);
            return new ChunkSpace(xChunk, yChunk, zChunk);
        }

        /// <summary>
        /// Unload a chunk from the world by the given chunk-space coordinates
        /// </summary>
        /// <returns>True if the chunk was unloaded, false if the chunk was a persistant chunk</returns>
        protected bool UnloadChunk(ChunkSpace coordinates)
        {
            var chunk = GetChunk(coordinates, createIfNull: false);

            if (chunk?.IsPersisted == true)
                return false;           

            WriteChunk(coordinates, chunk);
            _map.Remove(coordinates);
            return true;
        }

        private Chunk<T> GetChunk(ChunkSpace chunkCoordinate, bool createIfNull)
        {
            Chunk<T> chunk;
            var foundChunk = _map.TryGetValue(chunkCoordinate, out chunk);
            if (foundChunk)
                return chunk;

            if (!createIfNull)
                return null;

            var newChunk = ReadChunk(chunkCoordinate);
            _map.Add(chunkCoordinate, newChunk);
            return newChunk;
        }

        private Chunk<T> GetChunk(WorldSpace worldPosition, bool createIfNull)
        {
            return GetChunk(TranslateWorldToChunk(worldPosition), createIfNull);
        }

        /// <summary>
        /// Add the entity to the location specified.
        /// If the entity exists in the world already, it moves it.
        /// Mutates the entities location data appropriately.
        /// </summary>
        /// <param name="worldPosition">Position in world space coordinates</param>
        /// <param name="entity">Entity to relocate</param>
        public virtual void PutEntity(WorldSpace worldPosition, IEntityLocationData entity)
        {            
            // This item exists somewhere else, move it instead.
            if (entity.X != null && entity.Y != null && entity.Z != null)
            {
                MoveEntity(worldPosition, entity);
                return;
            }

            var chunk = GetChunk(worldPosition, createIfNull: true);

            entity.X = worldPosition.X;
            entity.Y = worldPosition.Y;
            entity.Z = worldPosition.Z;
            chunk.PutEntity(entity);
        }

        /// <summary>
        /// Move an entity from one location to another.
        /// This observes re-ordering in chunks.
        /// </summary>
        /// <param name="worldPosition">Position in world space coordinates</param>
        /// <param name="entity"></param>
        private void MoveEntity(WorldSpace worldPosition, IEntityLocationData entity)
        {
            if (entity.X != null && (entity.Y != null && entity.Z != null))
            {
                var oldPosition = entity.ToWorldSpace();
                var oldChunk = GetChunk(oldPosition, createIfNull: true);
                oldChunk.RemoveEntity(entity);
            }

            var newChunk = GetChunk(worldPosition, createIfNull: true);
            entity.X = worldPosition.X;
            entity.Y = worldPosition.Y;
            entity.Z = worldPosition.Z;

            newChunk.PutEntity(entity);
        }

        /// <summary>
        /// Returns all entities in the given chunk
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<IEntityLocationData> GetEntitiesInChunk(ChunkSpace chunkCoordinate)
        {
            var chunk = GetChunk(chunkCoordinate, createIfNull: true);
            return chunk.GetEntities();
        }

        public virtual IEnumerable<IEntityLocationData> GetEntitiesInChunk(WorldSpace worldCoordinates)
        {
            var chunk = GetChunk(worldCoordinates, createIfNull: true);
            return chunk.GetEntities();
        }

        public virtual IEnumerable<IEntityLocationData> GetEntitiesAt(long x, long y, long z)
        {
            var worldSpace = new WorldSpace(x, y, z);
            var chunk = GetChunk(worldSpace, createIfNull: true);
            return chunk.GetEntitiesAt(worldSpace);
        } 

        /// <summary>
        /// Remove an entity from the chunk it resides in currently
        /// </summary>
        /// <param name="entity">Entity to remove</param>
        public virtual void RemoveEntity(IEntityLocationData entity)
        {
            if (!entity.X.HasValue && !entity.Y.HasValue && !entity.Y.HasValue)
                throw new EntityNotInWorldException(entity, "Entity not tracked by map system");

            var worldSpace = entity.ToWorldSpace();
            var chunk = GetChunk(worldSpace, createIfNull: true);

            chunk.RemoveEntity(entity);
        }

        public class EntityNotInWorldException : Exception
        {
            public IEntityLocationData EntityInstance { get; set; }

            public EntityNotInWorldException(IEntityLocationData entity, string message) : base(message)
            {
                EntityInstance = entity;
            }
        }

        protected T Get(WorldSpace coordinates)
        {
            return GetChunk(coordinates, createIfNull: true)[coordinates];
        }

        protected void Put(WorldSpace coordinates, T block)
        {
            var chunk = GetChunk(coordinates, createIfNull: true);
            chunk[coordinates] = block;
        }

        public virtual void MakePersistant(WorldSpace coordinates) => GetChunk(coordinates, createIfNull: false).Persist();

        protected class Chunk<U> : IEnumerable<U>
        {
            private readonly int _chunkWidth;
            private readonly int _chunkHeight;
            private readonly int _chunkDepth;
            private readonly U[] _blocks;
            private readonly HashSet<IEntityLocationData> _items;
            private bool _persist;

            public bool IsPersisted => _persist;

            public Chunk(int chunkHeight, int chunkWidth, int chunkDepth, IEnumerable<U> items)
                : this(chunkHeight, chunkWidth, chunkDepth)
            {
                var array = items.ToArray();
                _blocks = array.Any() ? array : new U[chunkHeight * chunkWidth * chunkDepth];
                _items = new HashSet<IEntityLocationData>();
            }

            public Chunk(int chunkHeight, int chunkWidth, int chunkDepth)
            {
                _chunkWidth = chunkWidth;
                _chunkHeight = chunkHeight;
                _chunkDepth = chunkDepth;
                _blocks = new U[chunkHeight * chunkWidth * chunkDepth];
                _items = new HashSet<IEntityLocationData>();
            }

            public void Persist() => _persist = true;
            public void Unpersist() => _persist = false;
            public void TogglePersist() => _persist = !_persist;

            public IEnumerable<IEntityLocationData> GetEntities()
            {
                return _items;
            }

            public void PutEntity(IEntityLocationData entity)
            {
                _items.Add(entity);
            }

            public void RemoveEntity(IEntityLocationData entity)
            {
                _items.Remove(entity);
                entity.X = null;
                entity.Y = null;
                entity.Z = null;
            }

            public IEnumerable<IEntityLocationData> GetEntitiesAt(WorldSpace coordinates)
            {
                return _items.Where(item => item.ToWorldSpace() == coordinates);
            }

            public U this[WorldSpace coordinate]
            {
                get
                {
                    // Translate from world-space to chunk-space
                    var chunkSpace = WorldToChunk(coordinate);
                    var blockX = chunkSpace.X;
                    var blockY = chunkSpace.Y;
                    var blockZ = chunkSpace.Z;

                    // Flat array, so walk the stride length for the Y component.
                    return _blocks[blockX + _chunkWidth * (blockY + _chunkDepth * blockZ)];
                }
                set
                {
                    var chunkSpace = WorldToChunk(coordinate);
                    var blockX = chunkSpace.X;
                    var blockY = chunkSpace.Y;
                    var blockZ = chunkSpace.Z;

                    _blocks[blockX + _chunkWidth * (blockY + _chunkDepth * blockZ)] = value;
                }
            }

            public U this[int n]
            {
                get { return _blocks[n]; }
                set { _blocks[n] = value; }
            }

            public int Count { get { return _blocks.Length; } }

            private ItemSpace WorldToChunk(WorldSpace coordinate)
            {
                var blockX = Math.Abs(coordinate.X) % _chunkHeight;
                var blockY = Math.Abs(coordinate.Y) % _chunkWidth;
                var blockZ = Math.Abs(coordinate.Z) % _chunkDepth;

                return new ItemSpace((byte)blockX, (byte)blockY, (byte)blockZ);
            }

            private ChunkEnumerator Enumerate()
            {
                return new ChunkEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Enumerate();
            }

            public IEnumerator<U> GetEnumerator()
            {
                return Enumerate();
            }

            private class ChunkEnumerator : IEnumerator<U>
            {
                private readonly Chunk<U> _collection;
                private int _index;
                private U _current;

                public U Current
                {
                    get { return _current; }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                internal ChunkEnumerator(Chunk<U> collection)
                {
                    _collection = collection;
                    _index = -1;
                    _current = default(U);
                }

                public void Dispose() { }

                public void Reset()
                {
                    _index = -1;
                }

                public bool MoveNext()
                {
                    if (++_index >= _collection.Count)
                    {
                        return false;
                    }

                    _current = _collection[_index];
                    return true;
                }
            }
        }
    }
}
