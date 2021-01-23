using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Data
{
    public class DbObjects
    {
    }
}

namespace Lyra
{
    public class SnapInfo
    {
        public DateTime Updated { get; set; }
        public string Network { get; set; }
    }

    public class DualBalance
    {
        public decimal Normal { get; set; }
        public decimal UnRecv { get; set; }

        public decimal Total => Normal + UnRecv;
    }
    public class TotalBalance
    {
        public Dictionary<string, DualBalance> AllAccounts { get; set; }
    }

    public class Assert
    {
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public decimal Supply { get; set; }
        public string OwnerAccountId { get; set; }
        public int Holders { get; set; }
        public int Payments { get; set; }
        public decimal Price { get; set; }
        public string AssociatedPoolId { get; set; }
    }
}
