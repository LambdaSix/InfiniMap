using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace InfiniMap
{
    public class Chunk<T> : IEnumerable<T>
    {
        private readonly int _chunkWidth;
        private readonly int _chunkHeight;
        private readonly int _chunkDepth;
        private readonly T[] _blocks;
        private readonly HashSet<IEntityLocationData> _items;
        private bool _persist;

        public bool IsPersisted => _persist;

        public Chunk(int chunkWidth, int chunkHeight, int chunkDepth, IEnumerable<T> items)
            : this(chunkWidth, chunkHeight, chunkDepth)
        {
            var array = items.ToArray();
            _blocks = array.Any() ? array : new T[chunkWidth * chunkHeight * chunkDepth];
            _items = new HashSet<IEntityLocationData>();
        }

        public Chunk(int chunkWidth, int chunkHeight, int chunkDepth = 1)
        {
            _chunkWidth = chunkWidth;
            _chunkHeight = chunkHeight;
            _chunkDepth = chunkDepth;
            _blocks = new T[chunkWidth * chunkHeight * chunkDepth];
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

        public T this[WorldSpace coordinate]
        {
            get
            {
                // Translate from world-space to item-space
                var chunkSpace = WorldSpaceToItem(coordinate);
                var blockX = chunkSpace.X;
                var blockY = chunkSpace.Y;
                var blockZ = chunkSpace.Z;

                // Flat array, so walk the stride length for the Y component.
                return _blocks[blockX + (blockY + blockZ * _chunkWidth) * _chunkHeight];
            }
            set
            {
                var chunkSpace = WorldSpaceToItem(coordinate);
                var blockX = chunkSpace.X;
                var blockY = chunkSpace.Y;
                var blockZ = chunkSpace.Z;

                _blocks[blockX + (blockY + blockZ * _chunkWidth) * _chunkHeight] = value;
            }
        }

        /// <summary>
        /// Return an item from the given index into the chunk, ignoring spatial positioning.
        /// </summary>
        public T this[int n]
        {
            get { return _blocks[n]; }
            set { _blocks[n] = value; }
        }

        public int Count
        {
            get { return _blocks.Length; }
        }

        /// <summary>
        /// Convert a given WorldSpace co-ordinate to an in-chunk coordinate (an ItemSpace coordinate)
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        private ItemSpace WorldSpaceToItem(WorldSpace coordinate)
        {
            var blockX = Math.Abs(coordinate.X) % _chunkHeight;
            var blockY = Math.Abs(coordinate.Y) % _chunkWidth;
            var blockZ = Math.Abs(coordinate.Z) % _chunkDepth;

            return new ItemSpace((byte) blockX, (byte) blockY, (byte) blockZ);
        }

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

            public void Dispose()
            {
            }

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