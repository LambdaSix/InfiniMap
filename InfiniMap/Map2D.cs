using System;
using System.Collections.Generic;
using System.Linq;

namespace InfiniMap
{
    public class Map2D<T> : ChunkMap<T>
    {
        public Map2D(int chunkWidth, int chunkHeight) : base(chunkWidth, chunkHeight, 1) {}

        public Map2D() : this(16, 16) { }

        public IEnumerable<T> Within(WorldSpace2D begin, WorldSpace2D end)
        {
            return base.Within(begin, end);
        }

        public T this[WorldSpace2D coordinate]
        {
            get { return base[coordinate]; }
            set { base[coordinate] = value; }
        }

        public T this[long x, long y]
        {
            get { return this[new WorldSpace2D(x, y)]; }
            set { this[new WorldSpace2D(x, y)] = value; }
        }

        public void UnloadArea(WorldSpace2D begin, WorldSpace2D end)
        {
            var sequence = base.ChunksWithin(begin, end, createIfNull: false)
                .Select(chunk => TranslateWorldToChunk(chunk.Item1));

            foreach (var position in sequence)
            {
                UnloadChunk(position);
            }
        }

        /// <inheritdoc />
        public void MakePersistant(WorldSpace2D coordinates)
        {
            base.MakePersistant(coordinates);
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

    public struct WorldSpace2D : IEquatable<WorldSpace2D>
    {
        public readonly long X;
        public readonly long Y;

        public WorldSpace2D(long x, long y) : this()
        {
            X = x;
            Y = y;
        }

        /// <inheritdoc />
        public bool Equals(WorldSpace2D other)
        {
            return X == other.X && Y == other.Y;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is WorldSpace2D && Equals((WorldSpace2D)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ 0.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{X},{Y}";

        public static bool operator ==(WorldSpace2D self, WorldSpace2D other) => self.GetHashCode() == other.GetHashCode();
        public static bool operator ==(WorldSpace2D self, WorldSpace other) => self.GetHashCode() == other.GetHashCode();
        public static bool operator ==(WorldSpace self, WorldSpace2D other) => self.GetHashCode() == other.GetHashCode();

        public static bool operator !=(WorldSpace2D self, WorldSpace2D other) => self.GetHashCode() != other.GetHashCode();
        public static bool operator !=(WorldSpace2D self, WorldSpace other) => self.GetHashCode() != other.GetHashCode();
        public static bool operator !=(WorldSpace self, WorldSpace2D other) => self.GetHashCode() != other.GetHashCode();

        public static implicit operator WorldSpace2D(WorldSpace self) => new WorldSpace2D(self.X, self.Y);

#if CSHARP7
        public static implicit operator (long, long) (WorldSpace2D self) => (self.X, self.Y);
        public static implicit operator WorldSpace2D((long x, long y) self) => new WorldSpace2D(self.x, self.y);
        public static implicit operator WorldSpace2D((int x, int y) self) => new WorldSpace2D(self.x, self.y);


        public static bool operator ==(WorldSpace2D self, (long x, long y) other)
            => (other.x == self.X && other.y == self.Y);

        public static bool operator !=(WorldSpace2D self, (long x, long y) other)
            => (other.x != self.X || other.y == self.Y);

        public static bool operator ==(WorldSpace2D self, (int x, int y) other)
            => (other.x == self.X && other.y == self.Y);

        public static bool operator !=(WorldSpace2D self, (int x, int y) other)
            => (other.x != self.X || other.y == self.Y);
#endif
    }
}