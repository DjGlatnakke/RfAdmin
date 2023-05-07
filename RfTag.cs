using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace RfAdmin
{
    public class RfTag : ITableEntity
    {
        public string PartitionKey { get; set; }
        //rfId
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp{ get; set; }
        public ETag ETag { get; set; }
    }
}
