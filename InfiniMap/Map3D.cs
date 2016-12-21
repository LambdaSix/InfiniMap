using System;
using System.Collections.Generic;
using System.Linq;

namespace InfiniMap
{
    public class Map3D<T> : ChunkMap<T>
    {
        public Map3D(int chunkHeight, int chunkWidth, int chunkDepth) : base(chunkHeight, chunkWidth, chunkDepth) {}

        public Map3D() : this(16,16,16) { }

        public void UnloadArea(long x0, long y0, long z0, long x1, long y1, long z1 )
        {
            var begin = new WorldSpace(x0, y0, z0);
            var end = new WorldSpace(x1, y1, z1);

            var sequence = base.ChunksWithin(begin, end, createIfNull: false)
                .Select(chunk => TranslateWorldToChunk(chunk.Item1));

            foreach (var chunk in sequence)
            {
                UnloadChunk(chunk);
            }
        }

        public void UnloadAreaOutside(WorldSpace begin, WorldSpace end)
        {
            var localChunks = base.ChunksWithin(begin, end, createIfNull: false)
                .Select(tuple => TranslateWorldToChunk(tuple.Item1))
                .ToList();

            var worldChunks = All(chunk => !localChunks.Contains(chunk.Item1)).ToList();

            foreach (var chunk in worldChunks)
            {
                UnloadChunk(chunk.Key);
            }
        }

        /// <summary>
        /// Return a list of chunk sized enumerations from the specified area.
        /// </summary>
        /// <param name="createIfNull">If true, give the user a chance to create chunks</param>
        /// <param name="begin">Starting position</param>
        /// <param name="end">Ending position</param>
        /// <returns>A list of chunk sized enumerations from a specified area as (x,y,z,IEnumerable{T}) in chunk-space coordinates</returns>
        public new IEnumerable<Tuple<ChunkSpace, IEnumerable<T>>> ChunksWithin(WorldSpace begin, WorldSpace end, bool createIfNull)
        {
            return base.ChunksWithin(begin, end, createIfNull)
                .Select(s => Tuple.Create(TranslateWorldToChunk(s.Item1), s.Item2.AsEnumerable()));
        }

        public new T this[WorldSpace coordinate]
        {
            get { return base[coordinate]; }
            set { base[coordinate] = value; }
        }

        public new T this[long x, long y, long z]
        {
            get { return this[new WorldSpace(x, y, z)]; }
            set { base[new WorldSpace(x, y, z)] = value; }
        }
    }
}