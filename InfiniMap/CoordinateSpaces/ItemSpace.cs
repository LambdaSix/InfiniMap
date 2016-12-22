namespace InfiniMap
{
    public struct ItemSpace
    {
        public readonly byte X;
        public readonly byte Y;
        public readonly byte Z;

        public ItemSpace(byte x, byte y, byte z) : this()
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}