using Lyra.Core.Accounts;
using Lyra.Core.API;
using Lyra.Core.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Threading.Tasks;

namespace Nebula.Data
{
    public class Result
    {
        public long LastBlock { get; set; }
        public int SafeGasPrice { get; set; }
        public int ProposeGasPrice { get; set; }
        public int FastGasPrice { get; set; }
    }

    public class GasOracle
    {
        public int status { get; set; }
        public string message { get; set; }
        public Result result { get; set; }
    }

    public class TransTimeResult
    {
        public int status { get; set; }
        public string message { get; set; }
        public int result { get; set; }
    }

    public class SwapUtils
    {
        public static async Task<int> GetGasOracle(string apiKey)
        {
            var url = "https://api.etherscan.io/api?module=gastracker&action=gasoracle&apikey=" + apiKey;
            var htclient = new HttpClient();
            var gasOracle = await htclient.GetFromJsonAsync<GasOracle>(url);
            if (gasOracle.message == "OK")
                return gasOracle.result.ProposeGasPrice + 1;
            else
                throw new Exception("Failed to get Gas Oracle.");
        }

        public static async Task<int> EstimateTransferTime(string apiKey, int gasPrice)
        {
            var url = $"https://api.etherscan.io/api?module=gastracker&action=gasestimate&gasprice={gasPrice}000000000&apikey={apiKey}";
            var htclient = new HttpClient();
            var transTime = await htclient.GetFromJsonAsync<TransTimeResult>(url);
            if (transTime.message == "OK")
                return transTime.result;
            else
                throw new Exception("Failed to estimate transfer time.");
        }
        /*
        public static async Task<BigInteger> EstimateEthTransferFeeAsync(string ethApiUrl, string ethContract,
            string fromEthAddress, string toEthAddress, BigInteger tokenAmount, int gasPriceOracle)
        {
            //var web3 = new Web3(ethApiUrl);
            //var latestBlockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            //var latestBlock = await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(latestBlockNumber);

            //return latestBlock.GasLimit.Value / latestBlock.TransactionHashes.Length;

            var web3 = new Web3(ethApiUrl);
            var transactionMessage = new TransferFunction
            {
                FromAddress = fromEthAddress,
                To = toEthAddress,
                TokenAmount = tokenAmount,
                GasPrice = Web3.Convert.ToWei(gasPriceOracle, UnitConversion.EthUnit.Gwei)
            };

            var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();
            var estimate = await transferHandler.EstimateGasAsync(ethContract, transactionMessage);
            return estimate.Value;
        }

        public static async Task<(string hash, bool IsSuccess)> SendEthContractTokenAsync(string ethApiUrl, string ethContract, 
            string ethAddress, string ethPrivateKey, string targetEthAddress, BigInteger tokenCount,
            int gasPrice, BigInteger gasLimit,
            MetamaskInterceptor metamask)
        {
            Web3 web3;
            if (metamask == null)
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
                TokenAmount = tokenCount,
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei),
                Gas = gasLimit * 2
            };

            var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();
            var transferReceipt = await transferHandler.SendRequestAndWaitForReceiptAsync(ethContract, transactionMessage);

            if (transferReceipt.Status.Value == 0)
                return (transferReceipt.TransactionHash, false);

            var txDetails = await GetTxDetailsAsync(ethApiUrl, transferReceipt.TransactionHash);
            if (txDetails.FromAddress.Equals(ethAddress, StringComparison.InvariantCultureIgnoreCase)
                && txDetails.To.Equals(targetEthAddress, StringComparison.InvariantCultureIgnoreCase)
                && txDetails.TokenAmount == tokenCount
                )
            {
                return (transferReceipt.TransactionHash, true);
            }
            else
            {
                return (transferReceipt.TransactionHash, false);
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
        }*/

        public static async Task<decimal> GetLyraBalanceAsync(string lyraNetwork, string accountPvk)
        {
            var store = new AccountInMemoryStorage();
            var wallet = Wallet.Create(store, "default", "", lyraNetwork,
                accountPvk);
            var lyraClient = LyraRestClient.Create(lyraNetwork, Environment.OSVersion.ToString(),
                "Nebula Swap", "1.0");

            var syncResult = await wallet.SyncAsync(lyraClient);
            if (syncResult == APIResultCodes.Success)
            {
                return wallet.BaseBalance;
            }

            return 0m;
        }
    }
}
