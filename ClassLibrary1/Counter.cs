using Microsoft.WindowsAzure.Storage.Table;

namespace ClassLibrary
{
    public class Counter : TableEntity
    {
        public Counter(int count)
        {
            this.PartitionKey = "counter";
            this.RowKey = "CounterRK";

            this.count = count;
        }

        public Counter() { }

        public int count { get; set; }

    }
}
