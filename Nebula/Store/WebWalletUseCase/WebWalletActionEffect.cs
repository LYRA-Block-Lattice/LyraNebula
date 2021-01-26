using Fluxor;
using Microsoft.AspNetCore.Components;
using Nebula.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Lyra.Core.API;
using Microsoft.Extensions.Configuration;
using System.IO;
using Lyra.Core.Accounts;
using Lyra.Core.Blocks;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Lyra.Data.API;

namespace Nebula.Store.WebWalletUseCase
{
	public class Effects
	{
		private readonly ILyraAPI client;
		private readonly IConfiguration config;
		private readonly ILogger<Effects> logger;

		public Effects(ILyraAPI lyraClient, 
			IConfiguration configuration,
			ILogger<Effects> logger)
		{
			client = lyraClient;
			config = configuration;
			this.logger = logger;
		}

        [EffectMethod]
        public async Task HandleSend(WebWalletSendTokenAction action, IDispatcher dispatcher)
        {
			var result = await action.wallet.Sync(null);
			if (result == Lyra.Core.Blocks.APIResultCodes.Success)
			{
				var result2 = await action.wallet.Send(action.Amount, action.DstAddr, action.TokenName);
				if (result2.ResultCode == Lyra.Core.Blocks.APIResultCodes.Success)
				{

				}
			}
			dispatcher.Dispatch(new WebWalletResultAction(action.wallet, true, UIStage.Main));
		}

        [EffectMethod]
		public async Task HandleCreation(WebWalletCreateAction action, IDispatcher dispatcher)
		{
			var store = new AccountInMemoryStorage();
			var name = Guid.NewGuid().ToString();
			Wallet.Create(store, name, "", config["network"]);

			var wallet = Wallet.Open(store, name, "");
			await wallet.Sync(client);

			dispatcher.Dispatch(new WebWalletResultAction(wallet, true, UIStage.Main));
		}

		[EffectMethod]
		public async Task HandleRestore(WebWalletRestoreAction action, IDispatcher dispatcher)
		{
			try
            {
				var store = new AccountInMemoryStorage();
				var name = Guid.NewGuid().ToString();
				Wallet.Create(store, name, "", config["network"], action.privateKey);

				var wallet = Wallet.Open(store, name, "");
				if (action.selfVote)
					wallet.VoteFor = wallet.AccountId;

				await wallet.Sync(client);

				dispatcher.Dispatch(new WebWalletResultAction(wallet, true, UIStage.Main));
			}
			catch(Exception ex)
            {
				dispatcher.Dispatch(new WebWalletResultAction(null, false, UIStage.Entry));
			}
		}

		[EffectMethod]
		public async Task HandleRefresh(WebWalletRefreshBalanceAction action, IDispatcher dispatcher)
		{
			var result = await action.wallet.Sync(null);
			if (result == Lyra.Core.Blocks.APIResultCodes.Success)
			{

			}
			dispatcher.Dispatch(new WebWalletResultAction(action.wallet, true, UIStage.Main));
		}

		[EffectMethod]
		public async Task HandleTransactions(WebWalletTransactionsAction action, IDispatcher dispatcher)
		{
			var result = await action.wallet.Sync(null);
			List<string> txs = new List<string>();
			if (result == Lyra.Core.Blocks.APIResultCodes.Success)
			{
				var accHeight = await client.GetAccountHeight(action.wallet.AccountId);
				Dictionary<string, long> oldBalance = null;
				var start = accHeight.Height - 100;
				if (start < 1)
					start = 1;			// only show the last 100 tx
				for (long i = start; i <= accHeight.Height; i++)
                {
					var blockResult = await client.GetBlockByIndex(action.wallet.AccountId, i);
					var block = blockResult.GetBlock() as TransactionBlock;
					if (block == null)
						txs.Add("Null");
					else
                    {
						var str = $"No. {block.Height} {block.TimeStamp}, ";
						if (block is SendTransferBlock sb)
							str += $"Send to {sb.DestinationAccountId}";
						else if(block is ReceiveTransferBlock rb)
                        {
							if(rb.SourceHash == null)
                            {
								str += $"Genesis";
                            }
							else
                            {
								var srcBlockResult = await client.GetBlock(rb.SourceHash);
								var srcBlock = srcBlockResult.GetBlock() as TransactionBlock;
								str += $"Receive from {srcBlock.AccountID}";
							}
						}
						str += BalanceDifference(oldBalance, block.Balances);
						str += $" Balance: {string.Join(", ", block.Balances.Select(m => $"{m.Key}: {m.Value.ToBalanceDecimal()}"))}";
							
						txs.Add(str);

						oldBalance = block.Balances;
					}					
                }
			}
			txs.Reverse();
			dispatcher.Dispatch(new WebWalletTransactionsResultAction { wallet = action.wallet, transactions = txs });
		}

		private string BalanceDifference(Dictionary<string, long> oldBalance, Dictionary<string, long> newBalance)
        {
			if(oldBalance == null)
            {
				return " Amount: " + string.Join(", ", newBalance.Select(m => $"{m.Key} {m.Value.ToBalanceDecimal()}"));
			}
			else
            {
				return " Amount: " + string.Join(", ", newBalance.Select(m => $"{m.Key} {(decimal)(m.Value - (oldBalance.ContainsKey(m.Key) ? oldBalance[m.Key] : 0)) / LyraGlobal.TOKENSTORAGERITO}"));               
            }
        }

		[EffectMethod]
		public async Task HandleFreeToken(WebWalletFreeTokenAction action, IDispatcher dispatcher)
		{
			var store = new AccountInMemoryStorage();
			var name = Guid.NewGuid().ToString();
			Wallet.Create(store, name, "", config["network"], action.faucetPvk);
			var wallet = Wallet.Open(store, name, "");
			await wallet.Sync(client);

			dispatcher.Dispatch(new WebWalletFreeTokenResultAction { faucetBalance = (decimal)wallet.GetLatestBlock().Balances[LyraGlobal.OFFICIALTICKERCODE] / LyraGlobal.TOKENSTORAGERITO });
		}

		[EffectMethod]
		public async Task HandleFreeTokenSend(WebWalletSendMeFreeTokenAction action, IDispatcher dispatcher)
		{
			var store = new AccountInMemoryStorage();
			var name = Guid.NewGuid().ToString();
			Wallet.Create(store, name, "", config["network"], action.faucetPvk);
			var faucetWallet = Wallet.Open(store, name, "");
			await faucetWallet.Sync(client);

			// random amount
			var random = new Random();
			var randAmount = random.Next(300, 3000);

			var result = await faucetWallet.Send(randAmount, action.wallet.AccountId);
			if (result.ResultCode == APIResultCodes.Success)
			{
				await action.wallet.Sync(client);
				dispatcher.Dispatch(new WebWalletSendMeFreeTokenResultAction { Success = true, FreeAmount = randAmount });
			}
			else
            {
				dispatcher.Dispatch(new WebWalletSendMeFreeTokenResultAction { Success = false });
			}
		}

		[EffectMethod]
		public async Task HandleLyraSwap(WebWalletBeginTokenSwapAction action, IDispatcher dispatcher)
        {
			bool IsSuccess = false;
			string swapResultMessage = "";

			try
            {
				var pool = await client.GetPool(action.fromToken, action.toToken);
				if (pool.Successful() && pool.PoolAccountId != null)
				{
					var result = await action.wallet.SwapToken(pool.Token0, pool.Token1,
						action.fromToken, action.fromAmount, action.expectedRito, action.slippage);

					if (result.ResultCode == APIResultCodes.Success)
					{
						IsSuccess = true;
						swapResultMessage = "Success!";
					}
					else
					{
						swapResultMessage = $"Failed to swap token: {result.ResultCode}";
					}
				}
				else
				{
					swapResultMessage = $"Unable to get the liquidate pool: {pool.ResultCode}";
				}
			}
			catch(Exception ex)
            {
				swapResultMessage = "In Token Swap: " + ex.Message;
            }

			dispatcher.Dispatch(new WebWalletTokenSwapResultAction { Success = IsSuccess, errMessage = swapResultMessage });
		}

		[EffectMethod]
		public async Task HandleSwap(WebWalletBeginSwapTLYRAction action, IDispatcher dispatcher)
		{
			bool IsSuccess = false;
			try
            {
				if (action.fromToken == "LYR" && action.toToken == "TLYR")
				{
					var syncResult = await action.wallet.Sync(null);
					if (syncResult == APIResultCodes.Success)
					{
						var sendResult = await action.wallet.Send(action.fromAmount,
							action.options.lyrPub, "LYR");

						if (sendResult.ResultCode == APIResultCodes.Success)
						{
							logger.LogInformation($"TokenSwap: first stage is ok for {action.fromAddress}");
							var (txHash, result) = await SwapUtils.SendEthContractTokenAsync(
								action.options.ethUrl, action.options.ethContract, action.options.ethPub,
								action.options.ethPvk, 
								action.toAddress, new BigInteger(action.toAmount * 100000000), // 10^8 
								action.gasPrice, action.gasLimit,
								null);

							logger.LogInformation($"TokenSwap: second stage for {action.fromAddress} eth tx hash is {txHash} IsSuccess: {result}");

							if (!result)
								throw new Exception("Eth sending failed.");

							IsSuccess = true;
						}
						else
							throw new Exception("Unable to send from your Lyra wallet.");
					}
					else
						throw new Exception("Unable to sync Lyra Wallet.");
				}

				if (action.fromToken == "TLYR" && action.toToken == "LYR")
				{
					var (txHash, result) = await SwapUtils.SendEthContractTokenAsync(
						action.options.ethUrl, action.options.ethContract, action.fromAddress,
						null,
						action.options.ethPub, new BigInteger(action.fromAmount * 100000000), // 10^8 
						action.gasPrice, action.gasLimit,
						action.metamask);

					logger.LogInformation($"TokenSwap: first stage for {action.fromAddress} eth tx hash {result} IsSuccess: {result}");

					if (result) // test if success transfer
					{
						var store = new AccountInMemoryStorage();
						var wallet = Wallet.Create(store, "default", "", config["network"],
							action.options.lyrPvk);

						var syncResult = await wallet.Sync(client);
						if (syncResult == APIResultCodes.Success)
						{
							var sendResult = await wallet.Send(action.toAmount,
								action.toAddress, "LYR");

							if (sendResult.ResultCode == Lyra.Core.Blocks.APIResultCodes.Success)
							{
								IsSuccess = true;
							}
							else
								throw new Exception("Unable to send from your wallet.");
						}
						else
							throw new Exception("Unable to sync Lyra Wallet.");
					}
					else
                    {
						throw new Exception("Eth sending failed.");
                    }
				}
				logger.LogInformation($"TokenSwap: Swapping {action.fromAmount} from {action.fromAddress} to {action.toAddress} is succeed.");
				dispatcher.Dispatch(new WebWalletSwapTLYRResultAction { Success = IsSuccess });
			}
			catch(Exception ex)
            {
				logger.LogInformation($"TokenSwap: Swapping {action.fromAmount} from {action.fromAddress} to {action.toAddress} is failed. Error: {ex}");
				dispatcher.Dispatch(new WebWalletSwapTLYRResultAction { Success = false, errMessage = ex.ToString() });
			}
		}
	}
}
