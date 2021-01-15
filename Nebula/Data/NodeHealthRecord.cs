using Lyra.Data.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Data
{
    public class NodeHealthRecord
    {
        public int Id { get; set; }

        public string AccountId { get; set; }
        public string NetworkId { get; set; }
        public string IP { get; set; }    

        // need to be UTC
        public DateTime TimeStamp { get; set; }

        public bool IsAPIUp { get; set; }
        public bool IsPrimary { get; set; }
        //public bool IsUpgraded { get; set; }

        public BlockChainState? State { get; set;}

        // staking amount
        public decimal Votes { get; set; }
    }
}
