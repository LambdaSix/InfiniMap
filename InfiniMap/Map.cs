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

        public Block this[int x, int y]
        {
            get
            {
                int xChunk = (x/_chunkHeight);
                int yChunk = (y/_chunkWidth);

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