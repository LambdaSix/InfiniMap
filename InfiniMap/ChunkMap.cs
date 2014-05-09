using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace InfiniMap
{
    public class Map2D<T> :ChunkMap<T>
    {
        public Map2D(int chunkHeight, int chunkWidth) : base(chunkHeight, chunkWidth, 1) {}

        public Map2D() : this(16, 16) { }

        public new IEnumerable<T> Within(long x0, long y0, long x1, long y1)
        {
            return base.Within(x0, y0, x1, y1);
        }

        public new T this[long x, long y]
        {
            get { return base[x, y]; }
            set { base[x, y] = value; }
        }
    }

    public class Map3D<T> : ChunkMap<T>
    {
        public Map3D(int chunkHeight, int chunkWidth, int chunkDepth) : base(chunkHeight, chunkWidth, chunkDepth) {}

        public Map3D() : this(16,16,16) { }

        public new IEnumerable<T> Within(long x0, long y0, long z0, long x1, long y1, long z1)
        {
            return base.Within(x0, y0, z0, x1, y1, z1);
        }

        public new T this[long x, long y, long z]
        {
            get { return base[x, y, z]; }
            set { base[x, y, z] = value; }
        }
    }

    /// <summary>
    /// An abstract base class for providing chunked item storage in a 3 dimension grid.
    /// The user need not be intimately aware that the map is chunked.
    /// </summary>
    /// <remarks>
    /// There are three co-ordinate systems in use, chunk, item, and world.
    /// 
    ///     Chunk-Space, A co-ordinate of a chunk among other chunks, the center of the world is chunk (0,0,0)
    ///                 the chunk sitting on top of that to it would be (0,0,1)
    /// 
    ///     World-Space, A co-ordinate of an item among other items, the center of the world is (0,0,0) and
    ///                 an item directly ontop of it would be (0,0,1). An item 63 tiles away on the Y plane would be
    ///                 (0,63,1)
    /// 
    ///     Item-Space, A co-ordinate of an item inside a block, translated from world-space. The item at (worldspace) (0,0,1)
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
        private readonly Dictionary<Tuple<long, long, long>, Chunk<T>> _map;

        private Action<IEnumerable<T>, Tuple<long, long, long>> _writerFunc;
        private Func<Tuple<long, long, long>, IEnumerable<T>> _readerFunc; 

        public ChunkMap(int chunkHeight, int chunkWidth, int chunkDepth)
        {
            _chunkHeight = chunkHeight;
            _chunkWidth = chunkWidth;
            _chunkDepth = chunkDepth;
            _map = new Dictionary<Tuple<long, long, long>, Chunk<T>>(8);
        }

        public int Count { get { return _map.Values.Sum(c => c.Count); } }

        protected virtual T this[long x, long y]
        {
            get { return Get(x, y, 0); }
            set { Put(x, y, 0, value); }
        }

        protected virtual T this[long x, long y, long z]
        {
            get { return Get(x, y, z); }
            set { Put(x, y ,z, value); }
        }

        /// <summary>
        /// Return all loaded chunks
        /// </summary>
        /// <remarks>
        /// The (x,y,z) are chunk-space, not world-space
        /// </remarks>
        /// <returns>A 4-Tuple of (x,y,z,Chunk{T})</returns>
        protected IEnumerable<Tuple<long, long, long, Chunk<T>>> All()
        {
            return _map.Select(pair => Tuple.Create(pair.Key.Item1, pair.Key.Item2, pair.Key.Item3, pair.Value));
        }

        /// <summary>
        /// Return all loaded chunks given the predicate
        /// </summary>
        /// <remarks>
        /// The returned (x,y,z) are chunk-space, not world-space
        /// </remarks>
        /// <param name="predicate">Filter function to apply</param>
        /// <returns>A 4-Tuple of (x,y,z,Chunk{T}) filtered by the predicate function</returns>
        protected IEnumerable<Tuple<long, long, long, Chunk<T>>> All(Func<Tuple<long, long, long, Chunk<T>>, bool> predicate)
        {
             return All().Where(predicate);
        }

        /// <summary>
        /// Start writing chunks to disk, calling <paramref name="writeFunc"/> for every chunk of blocks, with
        /// that chunk passed to the callback.
        /// </summary>
        /// <remarks>
        /// <paramref name="writeFunc"/> will be called once for every chunk in memory, it gets passed
        /// a chunks worth of {T} each time, along with the chunk co-ordinates as an 3-tuple of (x,y,z)
        /// </remarks>
        /// <param name="writeFunc">Serialization function to use</param>
        public void Write(Action<IEnumerable<T>, Tuple<long,long,long>> writeFunc)
        {
            foreach (var chunk in _map)
            {
                writeFunc(chunk.Value.AsEnumerable(), chunk.Key);
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
        public void RegisterWriter(Action<IEnumerable<T>, Tuple<long,long,long>> writerFunc)
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
        public void RegisterReader(Func<Tuple<long,long,long>, IEnumerable<T>> readerFunc )
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
        /// <param name="coordinates">3-Tuple of co-ordinates of chunk to read</param>
        /// <returns>Chunk filled with T</returns>
        private Chunk<T> ReadChunk(Tuple<long,long,long> coordinates)
        {
            if (_readerFunc != null)
            {
                var items = _readerFunc(coordinates).ToList();

                if (items.Count() > (_chunkHeight*_chunkWidth*_chunkDepth))
                {
                    throw new NotSupportedException("Attempted to load a item block larger than this Maps chunk dimensions");
                }

                return new Chunk<T>(_chunkHeight, _chunkWidth, _chunkDepth, items);
            }

            // Without a reader function, just return a blank chunk.
            return new Chunk<T>(_chunkHeight, _chunkDepth, _chunkWidth);
        }

        /// <summary>
        /// Write a chunk using the write function, if defined.
        /// </summary>
        /// <param name="coordinates">3-Tuple of co-ordinates of the chunk to write</param>
        /// <param name="chunk">The chunk to write</param>
        private void WriteChunk(Tuple<long, long, long> coordinates, Chunk<T> chunk)
        {
            if (_writerFunc != null)
            {
                _writerFunc(chunk, coordinates);
            }
        }

        public bool Contains(T item)
        {
            return _map.Values.Any(chunk => chunk.Contains(item));
        }

        public bool Contains(T item, EqualityComparer<T> comp )
        {
            return _map.Values.Any(chunk => chunk.Contains(item, comp));
        }

        protected IEnumerable<T> Within(long x0, long y0, long x1, long y1)
        {
            return Within(x0, y0, 0, x1, y1, 0);
        } 

        protected IEnumerable<T> Within(long x0, long y0, long z0, long x1, long y1, long z1)
        {
            for (long x = x0; x <= x1; x++)
            {
                for (long y = y0; y <= y1; y++)
                {
                    for (long z = z0; z <= z1; z++)
                    {
                        yield return this[x, y, z];
                    }
                }
            }
        }

        private Tuple<long,long,long> TranslateWorldToChunk(long x, long y, long z)
        {
            var xChunk = (long)Math.Floor(x / (float)_chunkHeight);
            var yChunk = (long)Math.Floor(y / (float)_chunkWidth);
            var zChunk = (long)Math.Floor(z / (float)_chunkDepth);
            return Tuple.Create(xChunk, yChunk, zChunk);
        }

        private void UnloadChunk(long x, long y, long z)
        {
            WriteChunk(TranslateWorldToChunk(x, y, z), GetChunk(x, y, z));
        }

        private Chunk<T> GetChunk(long x, long y, long z)
        {
            var coordinates = TranslateWorldToChunk(x, y, z);

            // Scope chunk to here.
            {
                Chunk<T> chunk;
                var foundChunk = _map.TryGetValue(coordinates, out chunk);
                if (foundChunk)
                {
                    return chunk;
                }
            }

            var newChunk = ReadChunk(coordinates);
            _map.Add(coordinates, newChunk);
            return newChunk;
        }

        protected T Get(long x, long y, long z)
        {
            return GetChunk(x, y, z)[x, y, z];
        }

        protected void Put(long x, long y, long z, T block)
        {
            var chunk = GetChunk(x, y, z);
            chunk[x, y, z] = block;
        }

        protected class Chunk<U> : IEnumerable<U>
        {
            private readonly int _chunkWidth;
            private readonly int _chunkHeight;
            private readonly int _chunkDepth;
            private readonly U[] _blocks;

            public Chunk(int chunkHeight, int chunkWidth, int chunkDepth, IEnumerable<U> items)
                : this(chunkHeight,chunkWidth,chunkDepth)
            {
                var array = items.ToArray();
                if (array.Any())
                {
                    _blocks = array;
                }
                else
                {
                    _blocks = new U[chunkHeight*chunkWidth*chunkDepth];
                }
            }

            public Chunk(int chunkHeight, int chunkWidth, int chunkDepth)
            {
                _chunkWidth = chunkWidth;
                _chunkHeight = chunkHeight;
                _chunkDepth = chunkDepth;
                _blocks = new U[chunkHeight*chunkWidth*chunkDepth];
            }

            public U this[long x, long y, long z]
            {
                get
                {
                    // Translate from world-space to chunk-space
                    var blockX = Math.Abs(x) % _chunkHeight;
                    var blockY = Math.Abs(y) % _chunkWidth;
                    var blockZ = Math.Abs(z) % _chunkDepth;

                    // Flat array, so walk the stride length for the Y component.
                    return _blocks[blockX + _chunkWidth*(blockY + _chunkDepth*blockZ)];
                }
                set
                {
                    var blockX = Math.Abs(x) % _chunkHeight;
                    var blockY = Math.Abs(y) % _chunkWidth;
                    var blockZ = Math.Abs(z) % _chunkDepth;

                    _blocks[blockX + _chunkWidth*(blockY + _chunkDepth*blockZ)] = value;
                }
            }

            public U this[int n]
            {
                get { return _blocks[n]; }
                set { _blocks[n] = value; }
            }

            public int Count { get { return _blocks.Length; } }

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

                public void Dispose() {}

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
                    else
                    {
                        _current = _collection[_index];
                    }
                    return true;
                }
            }
        }
    }

    public static class ChunkMapExtensions
    {
        /// <summary>
        /// Provides a centered square distance on a center point including negative chunk
        /// co-ordinates.
        /// An odd value for range will round upwards.
        /// </summary>
        /// <remarks>
        /// Due to rounding, odd values for range will provide the same value as the next
        /// even number. That is, 'range: 5' will return the same values as 'range: 6' and
        /// 'range: 1' will return the same values as 'range: 2'
        /// </remarks>
        /// <param name="startX">Center position</param>
        /// <param name="startY">Center position</param>
        /// <param name="range">Range of search</param>
        /// <returns>A list of co-ordinates that are within the area</returns>
        public static IEnumerable<Tuple<int, int>> Distance<T>(this ChunkMap<T> context, int startX, int startY, int range)
        {
            range = (range % 2 == 0) ? range : range + 1;

            var topLeft = Tuple.Create(startX - (range / 2), startY - (range / 2));
            var topRight = Tuple.Create(startX + (range / 2), startY - (range / 2));
            var bottomLeft = Tuple.Create(startX - (range / 2), startY + (range / 2));

            for (int x = topLeft.Item1; x <= topRight.Item1; x++)
            {
                for (int y = topLeft.Item2; y <= bottomLeft.Item2; y++)
                {
                    yield return Tuple.Create(x, y);
                }
            }
        }
    } 
}