using System.Diagnostics;

namespace InfiniMap
{
    /// <summary>
    /// The bare minimum information a chunk needs to know to store an entities location.
    /// Extend this interface in your own code to add any additional things you need to know
    /// about an entity stored in a chunk and implement on your concrete class.
    /// </summary>
    /// <remarks>
    /// Positional values are nullable; as a null position is used to indicate an entity that is not
    /// present in the world, either because it is awaiting deletion or exists in another container.
    /// When you invoke Chunk{T}.RemoveEntity() the item is not nulled out, only it's positional data,
    /// it merely ceases to be an item that chunk knows about, it will still exist in memory.
    /// </remarks>
    public partial interface IEntityLocationData
    {
        long? X { get; set; }
        long? Y { get; set; }
        long? Z { get; set; }
    }

    internal static class EntityLocationDataExtensions
    {
        public static WorldSpace ToWorldSpace(this IEntityLocationData self)
        {
            return new WorldSpace(self.X.Value, self.Y.Value, self.Z.Value);
        }

        public static WorldSpace? ToWorldSpaceOrDefault(this IEntityLocationData self)
        {
            return self == null
                ? (WorldSpace?) null
                : new WorldSpace(self.X.GetValueOrDefault(), self.Y.GetValueOrDefault(), self.Z.GetValueOrDefault());
        }
    }
}