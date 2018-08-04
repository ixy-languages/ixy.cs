namespace IxyCs
{
    public abstract class IxyQueue
    {
        public readonly int EntriesCount;
        //This could possibly be a ushort to avoid casting
        public int Index {get; set;}

        public IxyQueue(int count)
        {
            this.EntriesCount = count;
            Index = 0;
        }
    }
}