using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace InfiniMap
{
    public class Chunk : Chunk<Block, IEntity>
    {
        public Chunk(int height, int width) : base(height, width) { }
    }

    public class Chunk<TBlockType, TEntityType>
        where TBlockType : ISerialize, new()
        where TEntityType : class, ISerialize
    {
        private readonly int _width;
        private readonly int _height;

        public TBlockType[] Blocks { get; set; }
        public IEnumerable<TEntityType> Entities { get; set; }

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

                Entities = new List<TEntityType>();
        }

        public TBlockType this[int x, int y]
        {
            get
            {
                // Translate from world-space to chunk-space
                int blockX = Math.Abs(x)/_height;
                int blockY = Math.Abs(y)/_width;
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

        public void Write(BinaryWriter stream)
        {
            // Write chunk header:
            // Magic number header
            stream.Write(0xC45A);
            // Version
            stream.Write(1);
            // Engine version
            stream.Write(1);
            // Number of blocks
            stream.Write(Blocks.Length);

            foreach (var block in Blocks)
            {
                block.Write(stream);
            }

            // Number of entities
            stream.Write(Entities.Count());

            foreach (var entity in Entities)
            {
                entity.Write(stream);
            }
        }
    }
}