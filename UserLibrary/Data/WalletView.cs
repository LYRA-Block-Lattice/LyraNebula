using DexServer.Ext;

namespace UserLibrary.Data
{
    public class WalletView : ExtAssert
    {
        public decimal MyBalance { get; set; }
        public decimal DexBalance { get; set; }

        public string Address { get; set; }
        public string DexWalletID { get; set; }
    }
}
