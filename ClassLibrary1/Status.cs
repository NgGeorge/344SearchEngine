using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class Status : TableEntity
    {
        public Status(string status)
        {
            this.PartitionKey = "workerStatusPK";
            this.RowKey = "workerStatusRK";

            this.status = status;
        }

        public Status() { }

        public string status { get; set; }

    }
}
