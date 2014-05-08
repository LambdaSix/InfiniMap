using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace InfiniMap
{
    public class ChunkMap<T>
    {
        private readonly int _chunkHeight;
        private readonly int _chunkWidth;
        private readonly Dictionary<Tuple<int, int>, Chunk<T>> _map;

        public ChunkMap() : this(16, 16) {}

        public ChunkMap(int chunkHeight, int chunkWidth)
        {
            _chunkHeight = chunkHeight;
            _chunkWidth = chunkWidth;
            _map = new Dictionary<Tuple<int, int>, Chunk<T>>(8);
        }

        public int Count { get { return _map.Values.Sum(c => c.Count); } }

        public bool Contains(T item)
        {
            return _map.Values.Any(chunk => chunk.Contains(item));
        }

        public bool Contains(T item, EqualityComparer<T> comp )
        {
            return _map.Values.Any(chunk => chunk.Contains(item, comp));
        }

        public IEnumerable<T> Within(int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x0 < x1; x++)
            {
                for (int y = y0; y0 < y1; y++)
                {
                    yield return this[x, y];
                }
            }
        }

        private Chunk<T> GetChunk(int x, int y)
        {
            var xChunk = (int)Math.Floor(x / (float)_chunkHeight);
            var yChunk = (int)Math.Floor(y / (float)_chunkWidth);

            Chunk<T> chunk;
            var foundChunk = _map.TryGetValue(Tuple.Create(xChunk, yChunk), out chunk);
            if (foundChunk)
            {
                return chunk;
            }

            var newChunk = new Chunk<T>(_chunkHeight, _chunkWidth);
            _map.Add(Tuple.Create(xChunk, yChunk), new Chunk<T>());
            return newChunk;
        }

        private T Get(int x, int y)
        {
            return GetChunk(x, y)[x, y];
        }

        private void Put(int x, int y, T block)
        {
            var chunk = GetChunk(x, y);
            chunk[x, y] = block;
        }

        public T this[int x, int y]
        {
            get { return Get(x, y); }
            set { Put(x, y, value); }
        }

        private class Chunk<T> : IEnumerable<T>
        {
            private readonly int _chunkWidth;
            private readonly int _chunkHeight;
            private readonly T[] _blocks;

            public Chunk() : this(16,16) { }

            public Chunk(int chunkHeight, int chunkWidth)
            {
                _chunkWidth = chunkWidth;
                _chunkHeight = chunkHeight;
                _blocks = new T[chunkHeight*chunkWidth];
            }

            public T this[int x, int y]
            {
                get
                {
                    // Translate from world-space to chunk-space
                    int blockX = Math.Abs(x) % _chunkHeight;
                    int blockY = Math.Abs(y) % _chunkWidth;
                    // Flat array, so walk the stride length for the Y component.
                    return _blocks[blockX + (blockY * _chunkWidth)];
                }
                set
                {
                    int blockX = Math.Abs(x) / _chunkHeight;
                    int blockY = Math.Abs(y) / _chunkWidth;

                    _blocks[blockX + (blockY * _chunkWidth)] = value;
                }
            }

            public T this[int n]
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

            public IEnumerator<T> GetEnumerator()
            {
                return Enumerate();
            } 

            private class ChunkEnumerator : IEnumerator<T>
            {
                private readonly Chunk<T> _collection;
                private int _index;
                private T _current;

                public T Current
                {
                    get { return _current; }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                internal ChunkEnumerator(Chunk<T> collection)
                {
                    _collection = collection;
                    _index = -1;
                    _current = default(T);
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