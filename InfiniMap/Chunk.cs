using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace InfiniMap
{
    public class Chunk : Chunk<Block>
    {
        public Chunk(int height, int width) : base(height, width) { }
    }

    public class Chunk<TBlockType> : ISerialize, IDeserialize<Chunk<TBlockType>>
        where TBlockType : ISerialize, IDeserialize<TBlockType>, ISerializeMetadata, new()
    {
        private readonly int _width;
        private readonly int _height;

        public TBlockType[] Blocks { get; set; }

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        public Chunk(int height, int width)
        {
            _height = height;
            _width = width;
            Blocks = new TBlockType[height*width];

            for (int i = 0; i < Blocks.Length; i++)
            {
                Blocks[i] = new TBlockType();
            }
        }

        public TBlockType this[int x, int y]
        {
            get
            {
                // Translate from world-space to chunk-space
                int blockX = x%_height;
                int blockY = y%_width;
                // Flat array, so walk the stride length for the Y component.
                return Blocks[blockX + (blockY*_width)];
            }
            set
            {
                int blockX = Math.Abs(x)/_height;
                int blockY = Math.Abs(y)/_width;

                Blocks[blockX + (blockY*_width)] = value;
            }
        }

        public void WriteMetadata(BinaryWriter stream)
        {
            // (start, length)
            ICollection<Tuple<long, long>> offsets = new List<Tuple<long, long>>();

            foreach (var buffer in Blocks.Select(block => block.GetMetadata())) {
                stream.Write(buffer.BsonBuffer);
            }
        }

        public void Write(BinaryWriter stream)
        {
            // Write chunk header:
            // Magic number header
            stream.Write(0xC45A);
            // Version
            stream.Write((byte)1);

            stream.Write(_height);
            stream.Write(_width);
            // Number of blocks
            stream.Write(Blocks.Length);

            foreach (var block in Blocks)
            {
                block.Write(stream);
            }
        }

        public Chunk<TBlockType> Read(BinaryReader stream)
        {
            var magicNumber = stream.ReadInt32();

            if (magicNumber != 0xC45A)
                throw new InvalidOperationException(
                    "Attempted to load a file with the wrong magic number, corrupted file or not a valid chunk file?");

            var version = stream.ReadByte();

            var height = stream.ReadInt32();
            var width = stream.ReadInt32();

            var chunk = new Chunk<TBlockType>(height, width);

            var blockCount = stream.ReadInt32();

            for (int i = 0; i < blockCount; i++)
            {
                chunk.Blocks[i] = new TBlockType().Read(stream);
            }

            return chunk;
        }
    }
}