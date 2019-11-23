using ConcurrentCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Azure_Serverless_SignalR_Core_Emulator.Models
{
    public class ConnectionMapping
    {
        public string connectionId { get; set; }
        public string userId { get; set; }
        public string hubName { get; set; }

        public ConcurrentHashSet<string> groups;

        public override int GetHashCode()
        {
            return connectionId.GetHashCode();
        }
    }
}
