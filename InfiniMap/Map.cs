using System;
using System.Collections.Generic;

namespace InfiniMap
{
    public class Map
    {
        public IDictionary<Tuple<int, int>, Chunk> Chunks;

        private int _chunkWidth;
        private int _chunkHeight;

        public Map(int chunkHeight, int chunkWidth)
        {
            _chunkHeight = chunkHeight;
            _chunkWidth = chunkWidth;
            Chunks = new Dictionary<Tuple<int, int>, Chunk>(64);
        }

        private IEnumerable<Tuple<int, int>> Distance(int startX, int startY, int range)
        {
            return Enumerable.Range(startX, range + 1).SelectMany(x => Enumerable.Range(startY, range + 1), Tuple.Create);
        }

        /// <summary>
        /// Very simple garbage collection, frees all chunks within the given range.
        /// </summary>
        /// <remarks>
        /// Frees up chunks in a square pattern. Given (0,0) and a range of 1, free:
        /// 0,0,1,0
        /// 0,1,1,1
        /// </remarks>
        /// <param name="curX">Chunk X to start from</param>
        /// <param name="curY">Chunk Y to start from</param>
        /// <param name="range">Square distance to free</param>
        public void UnloadArea(int curX, int curY, int range) 
        {
            // Clean out chunks further than (x,y) -> (x+range, y+range)
            foreach (var pair in Distance(curX, curY, range))
            {
                Chunks.Remove(pair);
            }
        }

        public Block this[int x, int y]
        {
            get
            {
                var xChunk = (int) Math.Floor(x/(float) _chunkHeight);
                var yChunk = (int) Math.Floor(y/(float) _chunkWidth);

                Chunk chunk;
                var foundChunk = Chunks.TryGetValue(Tuple.Create(xChunk, yChunk), out chunk);
                if (foundChunk)
                {
                    return chunk[x, y];
                }

                var newChunk = new Chunk(_chunkHeight, _chunkWidth);
                Chunks.Add(Tuple.Create(xChunk, yChunk), newChunk);
                return newChunk[x, y];
            }
            set
            {
                // Block is a reference type, so we just discard a local pointer after
                // alterting the object
                var block = this[x, y];
                block = value;
            }
        }

    }
}