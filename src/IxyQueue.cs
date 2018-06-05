namespace IxyCs
{
    public abstract class IxyQueue
    {
        public readonly int EntriesCount;
        //TODO : Should (at least in some cases) be a ushort
        public int Index {get; set;}

        public IxyQueue(int count)
        {
            this.EntriesCount = count;
            Index = 0;
        }
    }
}