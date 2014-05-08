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

    public abstract class ChunkMap<T>
    {
        private readonly int _chunkHeight;
        private readonly int _chunkWidth;
        private readonly int _chunkDepth;
        private readonly Dictionary<Tuple<long, long, long>, Chunk<T>> _map;

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

        private Chunk<T> GetChunk(long x, long y, long z)
        {
            var xChunk = (long) Math.Floor(x/(float) _chunkHeight);
            var yChunk = (long) Math.Floor(y/(float) _chunkWidth);
            var zChunk = (long) Math.Floor(z/(float) _chunkDepth);

            // Scope chunk to here.
            {
                Chunk<T> chunk;
                var foundChunk = _map.TryGetValue(Tuple.Create(xChunk, yChunk, zChunk), out chunk);
                if (foundChunk)
                {
                    return chunk;
                }
            }

            var newChunk = new Chunk<T>(_chunkHeight, _chunkWidth, _chunkDepth);
            _map.Add(Tuple.Create(xChunk, yChunk, zChunk), newChunk);
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

        private class Chunk<U> : IEnumerable<U>
        {
            private readonly int _chunkWidth;
            private readonly int _chunkHeight;
            private readonly int _chunkDepth;
            private readonly U[] _blocks;

            public Chunk() : this(16,16,1) { }

            public Chunk(int chunkHeight, int chunkWidth) : this(chunkHeight, chunkWidth, 1) {}

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