using Lyra.Core.Accounts;
using Nebula.Data;
using Nethereum.Metamask.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Nebula.Store.WebWalletUseCase
{
    public class WebWalletCreateAction { }

    public class WebWalletRestoreAction { 
        public string privateKey { get; set; } 
        public bool selfVote { get; set; }
    }

    public class WebWalletCloseAction { }

    public class WebWalletRefreshBalanceAction { public Wallet wallet { get; set; } }

    public class WebWalletSendAction { }

    public class WebWalletSendTokenAction
    {
        public Wallet wallet { get; set; }
        public string DstAddr { get; set; }
        public string TokenName { get; set; }
        public decimal Amount { get; set; }
    }

    public class WebWalletCancelSendAction { }

    public class WebWalletSettngsAction { }

    public class WebWalletCreateTokenAction { }

    public class WebWalletSettingsAction { }

    public class WebWalletSaveSettingsAction
    {
        public string VoteFor { get; set; }
    }

    public class WebWalletCancelSaveSettingsAction { }

    public class WebWalletTransactionsAction
    {
        public Wallet wallet { get; set; }
    }
    public class WebWalletTransactionsResultAction
    {
        public Wallet wallet { get; set; }
        public List<string> transactions { get; set; }
    }

    public class WebWalletSwapTokenAction
    {

    }

    public class WebWalletBeginTokenSwapAction
    {
        // lyra specified
        public Wallet wallet { get; set; }

        // swap specified
        public string fromToken { get; set; }
        public string toToken { get; set; }
        public decimal fromAmount { get; set; }
        public decimal expectedRito { get; set; }
        public decimal slippage { get; set; }
    }

    public class WebWalletTokenSwapResultAction
    {
        public bool Success { get; set; }
        public string errMessage { get; set; }
    }

    public class WebWalletSwapTLYRAction
    {

    }

    public class WebWalletBeginSwapTLYRAction
    {
        // lyra specified
        public Wallet wallet { get; set; }

        // swap specified
        public string fromToken { get; set; }
        public string toToken { get; set; }
        public string fromAddress { get; set; }
        public string toAddress { get; set; }
        public decimal fromAmount { get; set; }
        public decimal toAmount { get; set; }

        // eth contract, metamask, etc.
        public SwapOptions options { get; set; }
        public MetamaskInterceptor metamask { get; set; }

        public int gasPrice { get; set; }
        public BigInteger gasLimit { get; set; }
    }

    public class WebWalletSwapTLYRResultAction
    {
        public bool Success { get; set; }
        public string errMessage { get; set; }
    }

    public class WebWalletFreeTokenAction
    {
        public string faucetPvk { get; set; }
    }
    public class WebWalletFreeTokenResultAction
    {
        public decimal faucetBalance { get; set; }
    }
    public class WebWalletSendMeFreeTokenAction
    {
        public Wallet wallet { get; set; }
        public string faucetPvk { get; set; }
    }

    public class WebWalletSendMeFreeTokenResultAction
    {
        public bool Success { get; set; }
        public decimal FreeAmount { get; set; }
    }

    public class WebWalletReCAPTCHAValidAction
    {
        public bool ValidReCAPTCHA { get; set; }
    }

    public class WebWalletReCAPTCHAServerAction
    {
        public bool ServerVerificatiing { get; set; }
    }
}
