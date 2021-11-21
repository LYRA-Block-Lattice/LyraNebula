using DexServer.Ext;

namespace Nebula.Data
{
    public class WalletView
    {
        public string assertName { get; set; }
        public string assertSymbol { get; set; }    
        public string assertNetworkProvider { get; set; }
        public string assertContract { get; set; }
        public decimal balance { get; set; }
    }
}
