using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace InfiniMap
{
    /// <summary>
    /// Provides support for attaching Expiration to objects 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CacheEntry<T>
    {
        private DateTime _expiration;
        private readonly object _lock = new object();

        public T Value { get; private set; }

        public DateTime Expiration
        {
            get { lock (_lock) { return _expiration; } }
            private set { lock (_lock) { _expiration = value; } }
        }

        public CacheEntry(T value, DateTime expiration)
        {
            Expiration = expiration;
            Value = value;
        }

        public void Refresh()
        {
            lock (_lock)
            {
                Expiration = DateTime.Now;
            }
        }
    }

    public class Map
    {
        public IDictionary<Tuple<int, int>, CacheEntry<Chunk>> Chunks;

        private readonly int _chunkWidth;
        private readonly int _chunkHeight;

        public Map(int chunkHeight, int chunkWidth)
        {
            _chunkHeight = chunkHeight;
            _chunkWidth = chunkWidth;
            Chunks = new Dictionary<Tuple<int, int>, CacheEntry<Chunk>>(64);
        }

        private IEnumerable<Tuple<int, int>> Distance(int startX, int startY, int range)
        {
            return Enumerable.Range(startX, range + 1).SelectMany(x => Enumerable.Range(startY, range + 1), Tuple.Create);
        }

        /// <summary>
        /// Unload all chunks within a given area.
        /// Ignores expiration, all chunks are unloaded; expired or not.
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

        /// <summary>
        /// Unload everything except the defined area.
        /// Ignores expiration, all chunks are unloaded, expired or not.
        /// </summary>
        /// <remarks>
        /// Frees chunks with a square pattern being kept in memory. Given (1,1) and a range of 1:
        /// (0,0) (0,1) (0,2)
        /// (1,0) [1,1] [1,2]
        /// (2,0) [2,1] [2,2]
        /// (3,0) (3,1) (2,2)
        /// 
        /// [x,y] are kept in memory, (x,y) are discarded from memory.
        /// </remarks>
        /// <param name="curX">Chunk X to start from</param>
        /// <param name="curY">Chunk Y to start from</param>
        /// <param name="range">Square distance to keep in memory</param>
        public void UnloadAreaInverted(int curX, int curY, int range)
        {
            var localChunks = Distance(curX, curY, range).ToList();
            // Clean out chunks outside of (x,y) -> (x+range, y+range)
            foreach (var pair in Chunks.Where(pair => !localChunks.Contains(pair.Key)))
            {
                Chunks.Remove(pair.Key);
            }
        }

        /// <summary>
        /// Unload all expired blocks within a given area.
        /// </summary>
        /// <remarks>
        /// Follows the same selection rules as <seealso cref="UnloadArea"/> except for
        /// considering expiration of chunks.
        /// </remarks>
        /// <param name="expiryTime">Expiry time to use, any chunks last accessed before this point are unloaded</param>
        /// <param name="curX">Chunk X to start from</param>
        /// <param name="curY">Chunk Y to start from</param>
        /// <param name="range">Square distance to unload</param>
        public void UnloadExpiredArea(DateTime expiryTime, int curX, int curY, int range)
        {
            CacheEntry<Chunk> chunk = null;
            foreach (var result in Distance(curX, curY, range).Select(pair => new {pair, found = Chunks.TryGetValue(pair, out chunk)}).Where(
                    result => result.found).Where(result => chunk.Expiration <= expiryTime)) {
                Chunks.Remove(result.pair);
            }
        }

        /// <summary>
        /// Unload all expired blocks outside of the defined area.
        /// </summary>
        /// <remarks>
        /// Follows the same selection rules as <seealso cref="UnloadAreaInverted"/> except for
        /// considering expiration of chunks.
        /// </remarks>
        /// <param name="expiryTime">Expiry time to use, any chunks last accessed before this point are unloaded</param>
        /// <param name="curX">Chunk X to start from</param>
        /// <param name="curY">Chunk Y to start form</param>
        /// <param name="range">Square distance to unload</param>
        public void UnloadedExpiredAreaInverted(DateTime expiryTime, int curX, int curY, int range)
        {
            CacheEntry<Chunk> chunk = null;
            var localChunks = Distance(curX, curY, range).ToList();
            // Clean out chunks outside of (x,y) -> (x+range,y+range) where Expiration <= expiryTime
            foreach (var result in Chunks.Select(pair => new {key = pair.Key, found = Chunks.TryGetValue(pair.Key, out chunk)}).Where(
                result => result.found).Where(pair => !localChunks.Contains(pair.key)).Where(result => chunk.Expiration <= expiryTime))
            {
                Chunks.Remove(result.key);
            }
        }
        
        /// <summary>
        /// Unload all expired blocks.
        /// This does not account for keeping any blocks in memory.
        /// </summary>
        /// <param name="expiryTime">Expiry time to use, any chunks last accessed before this point are unloaded</param>
        public void UnloadExpired(DateTime expiryTime)
        {
            foreach (var pair in Chunks.Where(x => x.Value.Expiration <= expiryTime))
            {
                Chunks.Remove(pair.Key);
            }
        }

        /// <summary>
        /// Save a world into <paramref name="folderPath"/>
        /// </summary>
        /// <param name="folderPath">Root world folder to save into</param>
        public void Write(string folderPath)
        {
            foreach (var pair in Chunks.Select(chunk => new {Chunk = chunk.Value.Value, chunk.Key}))
            {
                var chunkName = String.Format("{0}/chnk_{1}-{2}.cdat", folderPath, pair.Key.Item1, pair.Key.Item2);
                var chunkMetadataName = String.Format("{0}/cnk-md_{1}-{2}.cdat", folderPath, pair.Key.Item1, pair.Key.Item2);

                // Write the metadata first, because the block has a byte-offset lookup into the metadata file.

                using (var stream = new BinaryWriter(File.Open(chunkMetadataName, FileMode.Create)))
                {
                    pair.Chunk.WriteMetadata(stream);
                }

                using (var stream = new BinaryWriter(File.Open(chunkName, FileMode.Create)))
                {
                    pair.Chunk.Write(stream);
                }
            }
        }

        /// <summary>
        /// Load a worldmap from <paramref name="folderPath"/>
        /// </summary>
        /// <param name="folderPath">Root world folder to load from</param>
        public void Read(string folderPath)
        {
        }

        public Block this[int x, int y]
        {
            get
            {
                var xChunk = (int) Math.Floor(x/(float) _chunkHeight);
                var yChunk = (int) Math.Floor(y/(float) _chunkWidth);

                CacheEntry<Chunk> chunk;
                var foundChunk = Chunks.TryGetValue(Tuple.Create(xChunk, yChunk), out chunk);
                if (foundChunk)
                {
                    chunk.Refresh();
                    return chunk.Value[x, y];
                }

                var newChunk = new Chunk(_chunkHeight, _chunkWidth);
                Chunks.Add(Tuple.Create(xChunk, yChunk), new CacheEntry<Chunk>(newChunk, DateTime.Now));
                return newChunk[x, y];
            }
            set
            {
                // Block is a reference type, so we just discard a local pointer after
                // altering the object
                var block = this[x, y];
                block = value;
            }
        }

    }
}