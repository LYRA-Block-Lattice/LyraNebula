using Lyra.Core.Accounts;
using Lyra.Core.API;
using Lyra.Core.Blocks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Metamask.Blazor;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Nebula.Data
{
    public class SwapUtils
    {
        public static async Task<bool> SendEthContractTokenAsync(string ethApiUrl, string ethContract, 
            string ethAddress, string ethPrivateKey, string targetEthAddress, BigInteger tokenCount,
            MetamaskInterceptor metamask)
        {
            try
            {
                Web3 web3;
                if(metamask == null)
                {
                    if (string.IsNullOrWhiteSpace(ethPrivateKey))
                    {
                        throw new ArgumentNullException("Need key for ethereum account.");
                    }
                    var account = new Account(ethPrivateKey);
                    web3 = new Web3(account, ethApiUrl);
                }
                else
                {
                    web3 = new Nethereum.Web3.Web3();
                    web3.Client.OverridingRequestInterceptor = metamask;
                }

                var transactionMessage = new TransferFunction
                {
                    FromAddress = ethAddress,
                    To = targetEthAddress,
                    TokenAmount = tokenCount
                };

                var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();
                var transferReceipt = await transferHandler.SendRequestAndWaitForReceiptAsync(ethContract, transactionMessage);

                //var transaction = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transferReceipt.TransactionHash);
                //var transferDecoded = transaction.DecodeTransactionToFunctionMessage<TransferFunction>();

                var txDetails = await GetTxDetailsAsync(ethApiUrl, transferReceipt.TransactionHash);
                if(txDetails.FromAddress.Equals(ethAddress, StringComparison.InvariantCultureIgnoreCase) 
                    && txDetails.To.Equals(targetEthAddress, StringComparison.InvariantCultureIgnoreCase)
                    && txDetails.TokenAmount == tokenCount)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch(Exception ex)
            {
                return false; // verify either
            }
        }

        public static async Task<TransferFunction> GetTxDetailsAsync(string ethApiUrl, string ethTxHash)
        {
            var web3 = new Web3(ethApiUrl);
            var transaction = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(ethTxHash);
            var transferDecoded = transaction.DecodeTransactionToFunctionMessage<TransferFunction>();
            return transferDecoded;
        }

        public static async Task<decimal> GetEthContractBalanceAsync(string ethApiUrl, string ethContract, string ethAddress)
        {
            var web3 = new Web3(ethApiUrl);

            var balanceMessage = new BalanceOfFunction
            {
                Owner = ethAddress
            };

            var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
            var balance = await balanceHandler.QueryAsync<BigInteger>(ethContract, balanceMessage);

            var fromTokenBalance = (decimal)(balance / 100000000);

            return fromTokenBalance;
        }

        [Function("balanceOf", "uint256")]
        public class BalanceOfFunction : FunctionMessage
        {
            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("address", "_owner", 1)]
            public string Owner { get; set; }
        }

        [Function("transfer", "bool")]
        public class TransferFunction : FunctionMessage
        {
            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("address", "_to", 1)]
            public string To { get; set; }

            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "_value", 2)]
            public BigInteger TokenAmount { get; set; }
        }

        public static async Task<decimal> GetLyraBalanceAsync(string lyraNetwork, string accountPvk)
        {
            var store = new AccountInMemoryStorage();
            var wallet = Wallet.Create(store, "default", "", lyraNetwork,
                accountPvk);
            var lyraClient = LyraRestClient.Create(lyraNetwork, Environment.OSVersion.ToString(),
                "Nebula Swap", "1.0");

            var syncResult = await wallet.Sync(lyraClient);
            if (syncResult == APIResultCodes.Success)
            {
                var block = wallet.GetLatestBlock();
                return block.Balances["LYR"] / LyraGlobal.TOKENSTORAGERITO;
            }

            return 0m;
        }
    }
}
