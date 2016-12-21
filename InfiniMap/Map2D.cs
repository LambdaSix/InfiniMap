using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace InfiniMap
{
    public struct WorldSpace2D
    {
        public readonly long X;
        public readonly long Y;
        internal readonly long Z;

        public WorldSpace2D(long x, long y) : this()
        {
            X = x;
            Y = y;
            Z = 0;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return X.GetHashCode() | Y.GetHashCode() | Z.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj?.GetHashCode() == GetHashCode();
        }

        public static bool operator ==(WorldSpace2D self, WorldSpace2D other) => self.GetHashCode() == other.GetHashCode();
        public static bool operator ==(WorldSpace2D self, WorldSpace other) => self.GetHashCode() == other.GetHashCode();

        public static bool operator !=(WorldSpace2D self, WorldSpace2D other) => self.GetHashCode() != other.GetHashCode();
        public static bool operator !=(WorldSpace2D self, WorldSpace other) => self.GetHashCode() != other.GetHashCode();

        public static explicit operator WorldSpace(WorldSpace2D self) => new WorldSpace(self.X, self.Y, self.Z);
    }

    public class Map2D<T> : ChunkMap<T>
    {
        public Map2D(int chunkHeight, int chunkWidth) : base(chunkHeight, chunkWidth, 1) {}

        public Map2D() : this(16, 16) { }

        public IEnumerable<T> Within(WorldSpace2D begin, WorldSpace2D end)
        {
            return Within((WorldSpace) begin, (WorldSpace) end);
        }

        public T this[WorldSpace2D coordinate]
        {
            get { return base[(WorldSpace)coordinate]; }
            set { base[(WorldSpace)coordinate] = value; }
        }

        public T this[long x, long y]
        {
            get { return this[new WorldSpace2D(x, y)]; }
            set { this[new WorldSpace2D(x, y)] = value; }
        }

        public void UnloadArea(WorldSpace2D begin, WorldSpace2D end)
        {
            var sequence = base.ChunksWithin((WorldSpace) begin, (WorldSpace) end, createIfNull: false)
                .Select(chunk => TranslateWorldToChunk(chunk.Item1));

            foreach (var position in sequence)
            {
                UnloadChunk(position);
            }
        }

        /// <summary>
        /// Add the entity to the location specified.
        /// Mutates the entities location data appropriately.
        /// </summary>
        /// <param name="x">World-space X coordinate</param>
        /// <param name="y">world-space Y coordinate</param>
        /// <param name="entity">Entity to relocate</param>
        public void PutEntity(long x, long y, IEntityLocationData entity)
        {
            base.PutEntity(new WorldSpace(x, y, 0), entity);
        }

        /// <summary>
        /// Returns all entities at the location.
        /// </summary>
        /// <param name="x">World-space X coordinate</param>
        /// <param name="y">World-space Y coordinate</param>
        /// <returns>A set of entities at that location</returns>
        public IEnumerable<IEntityLocationData> GetEntitiesAt(long x, long y)
        {
            return base.GetEntitiesAt(x, y, 0);
        }

        /// <summary>
        /// Returns all entities in the chunk containing the location
        /// </summary>
        /// <param name="x">World-space X coordinate</param>
        /// <param name="y">World-space Y coordinate</param>
        /// <returns>A set of entities in the same chunk as the coordinates</returns>
        public IEnumerable<IEntityLocationData> GetEntitiesInChunk(long x, long y)
        {
            return base.GetEntitiesInChunk(new WorldSpace(x, y, 0));
        }

        public void UnloadAreaOutside(int x0, int y0, int x1, int y1)
        {
            var localChunks = base.ChunksWithin(x0, y0, x1, y1, createIfNull: false)
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
        /// <param name="x0">Starting X coordinate</param>
        /// <param name="y0">Starting Y coordinates</param>
        /// <param name="x1">Ending X coordinates</param>
        /// <param name="y1">Ending Y coordinates</param>
        /// <param name="createIfNull">If true, give the user a chance to create chunks</param>
        /// <returns>A list of chunk sized enumerations from a specified area as (ChunkSpace,IEnumerable{T})</returns>
        public new IEnumerable<Tuple<ChunkSpace, IEnumerable<T>>> ChunksWithin(long x0, long y0, long x1, long y1, bool createIfNull)
        {
            return base.ChunksWithin(x0, y0, x1, y1, createIfNull)
                .Select(s => Tuple.Create(TranslateWorldToChunk(s.Item1), s.Item2.AsEnumerable()));
        }
    }
}