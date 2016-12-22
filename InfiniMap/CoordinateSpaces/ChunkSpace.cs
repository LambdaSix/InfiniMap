using System;

namespace InfiniMap
{
    public struct ChunkSpace : IEquatable<ChunkSpace>
    {
        public readonly long X;
        public readonly long Y;
        public readonly long Z;

        public ChunkSpace(long x, long y, long z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc />
        public bool Equals(ChunkSpace other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}";
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ChunkSpace && Equals((ChunkSpace) obj);
        }

        public static bool operator ==(ChunkSpace self, ChunkSpace other) => self.GetHashCode() == other.GetHashCode();
        public static bool operator !=(ChunkSpace self, ChunkSpace other) => self.GetHashCode() != other.GetHashCode();

#if CSHARP7
        public static implicit operator (long, long, long) (ChunkSpace self) => (self.X, self.Y, self.Z);
        public static implicit operator ChunkSpace((long x, long y, long z) self) => new ChunkSpace(self.x, self.y, self.z);
        public static implicit operator ChunkSpace((int x, int y, int z) self) => new ChunkSpace(self.x, self.y, self.z);

        public static bool operator ==(ChunkSpace self, (long x, long y, long z) other)
            => (other.x == self.X && other.y == self.Y && other.z == self.Z);
        public static bool operator !=(ChunkSpace self, (long x, long y, long z) other)
            => (other.x != self.X || other.y != self.Y || other.z != self.Z);

        public static bool operator ==(ChunkSpace self, (int x, int y, int z) other)
            => (other.x == self.X && other.y == self.Y && other.z == self.Z);
        public static bool operator !=(ChunkSpace self, (int x, int y, int z) other)
            => (other.x != self.X || other.y != self.Y || other.z != self.Z);
#endif
    }
}