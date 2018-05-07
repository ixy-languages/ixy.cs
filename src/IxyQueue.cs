namespace IxyCs
{
    public abstract class IxyQueue
    {
        public readonly int EntriesCount;
        public int Index {get; set;}

        public IxyQueue(int count)
        {
            this.EntriesCount = count;
            Index = 0;
        }
    }
}