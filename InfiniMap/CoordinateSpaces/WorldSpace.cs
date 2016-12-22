using System;

namespace InfiniMap
{
    public struct WorldSpace : IEquatable<WorldSpace>
    {
        public readonly long X;
        public readonly long Y;
        public readonly long Z;

        public WorldSpace(long x, long y, long z) : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <inheritdoc />
        public bool Equals(WorldSpace other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
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
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is WorldSpace && Equals((WorldSpace) obj);
        }

        /// <inheritdoc />
        public override string ToString() => $"{X},{Y},{Z}";

        public static bool operator ==(WorldSpace self, WorldSpace other) => self.GetHashCode() == other.GetHashCode();
        public static bool operator !=(WorldSpace self, WorldSpace other) => self.GetHashCode() != other.GetHashCode();

        public static implicit operator WorldSpace(WorldSpace2D self) => new WorldSpace(self.X, self.Y, 0);

#if CSHARP7
        public static implicit operator (long, long, long)(WorldSpace self) => (self.X, self.Y, self.Z);
        public static implicit operator WorldSpace((long x, long y, long z) self) => new WorldSpace(self.x, self.y, self.z);
        public static implicit operator WorldSpace((int x, int y, int z) self) => new WorldSpace(self.x, self.y, self.z);

        public static bool operator ==(WorldSpace self, (long x, long y, long z) other)
            => (other.x == self.X && other.y == self.Y && other.z == self.Z);
        public static bool operator !=(WorldSpace self, (long x, long y, long z) other)
            => (other.x != self.X || other.y != self.Y || other.z != self.Z);

        public static bool operator ==(WorldSpace self, (int x, int y, int z) other)
            => (other.x == self.X && other.y == self.Y && other.z == self.Z);
        public static bool operator !=(WorldSpace self, (int x, int y, int z) other)
            => (other.x != self.X || other.y != self.Y || other.z != self.Z);
#endif
    }
}