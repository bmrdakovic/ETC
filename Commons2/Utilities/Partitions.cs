namespace Commons.Utilities
{
    public class Partitions
    {
        public static int GetPartitionKey(long ID)
        {
            int key = (int)(ID % 2);
            return key;
        }
    }
}