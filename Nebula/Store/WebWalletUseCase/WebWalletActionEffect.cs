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

namespace Nebula.Store.WebWalletUseCase
{
	public class Effects
	{
		private readonly LyraRestClient client;
		private readonly IConfiguration config;

		public Effects(LyraRestClient lyraClient, IConfiguration configuration)
		{
			client = lyraClient;
			config = configuration;
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
				for(long i = 1; i <= accHeight.Height; i++)
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
		public async Task HandleSwap(WebWalletSwapTokenAction action, IDispatcher dispatcher)
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
							var result = await SwapUtils.SendEthContractTokenAsync(
								action.options.ethUrl, action.options.ethContract, action.options.ethPub,
								action.options.ethPvk, 
								action.toAddress, new BigInteger(action.toAmount * 100000000), // 10^8 
								null);

							IsSuccess = result;
						}
						else
							throw new Exception("Unable to send from your wallet.");
					}
					else
						throw new Exception("Unable to sync Lyra Wallet.");
				}

				if (action.fromToken == "TLYR" && action.toToken == "LYR")
				{
					var result = await SwapUtils.SendEthContractTokenAsync(
						action.options.ethUrl, action.options.ethContract, action.fromAddress,
						null,
						action.options.ethPub, new BigInteger(action.fromAmount * 100000000), // 10^8 
						action.metamask);

					if (result) // test if success transfer
					{
						var store = new AccountInMemoryStorage();
						var wallet = Wallet.Create(store, "default", "", config["network"],
							action.options.lyrPvk);
						var lyraClient = LyraRestClient.Create(config["network"], Environment.OSVersion.ToString(),
							"Nebula Swap", "1.0");

						var syncResult = await wallet.Sync(lyraClient);
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
				}

				dispatcher.Dispatch(new WebWalletSwapResultAction { Success = IsSuccess });
			}
			catch(Exception ex)
            {
				dispatcher.Dispatch(new WebWalletSwapResultAction { Success = false, errMessage = ex.ToString() });
			}
		}
	}
}
