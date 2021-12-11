using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserLibrary.Data
{
    public class DexTransferArgs
    {
        public string title { get; set; }  
        public decimal min { get; set; }
        public decimal max { get; set; }
        public decimal val { get; set; }
        public string symbol { get; set; }
    }

    public class DexDepositArgs
    {
        public string title { get; set; }
        public WalletView view { get; set; }
        public string address { get; set; }
    }

    public class DexWithdrawArgs
    {
        public string title { get; set; }
        public WalletView view { get; set; }
        public string address { get; set; }
        public decimal amount { get; set; }
        public decimal min { get; set; }
        public decimal max { get; set; }
    }
}
