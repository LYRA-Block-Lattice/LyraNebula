using Fluxor;
using Lyra.Core.Accounts;
using Lyra.Core.API;
using Lyra.Data.Crypto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Nebula.Data;
using Nebula.Store.BlockSearchUseCase;
using Nebula.Store.WebWalletUseCase;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.StandardTokenEIP20.ContractDefinition;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Pages
{
	public partial class WebWallet
	{
		[Inject]
		private IState<WebWalletState> walletState { get; set; }

		[Inject]
		private IDispatcher Dispatcher { get; set; }

		[Inject]
		private IJSRuntime iJS { get; set; }

		// swap
		public string[] SwapableTokens { get; set; }
		private string _swapFromTokenName;
		public string swapFromToken { 
			get
			{
				return _swapFromTokenName;
			}
			set 
			{
				_swapFromTokenName = value;

				UpdateSwapFromBalance();
			} 
		}
		private string _swapToTokenName;
		public string swapToToken
		{
			get
			{
				return _swapToTokenName;
			}
			set
			{
				_swapToTokenName = value;

				UpdateSwapToBalance();
			}
		}

		public decimal fromTokenBalance { get; set; }

		private decimal _swapFromCount;
		public decimal swapFromCount
		{
			get
			{
				return _swapFromCount;
			}
			set
			{
				_swapFromCount = value;
				UpdateSwapToBalance();
			}
		}
		public decimal swapToCount { get; set; }

		[Required]
		[StringLength(120, ErrorMessage = "Name is too long.")]
		public string swapToAddress { get; set; }
		public string swapResultMessage { get; set; }
		//end swap
		public string prvKey { get; set; }
		public bool selfVote { get; set; }

		// for send
		public string dstAddr { get; set; }
		public string tokenName { get; set; }
		public decimal amount { get; set; }

		// for settings
		public string voteAddr { get; set; }

		public string altDisplay { get; set; }

		public WebWallet()
        {
			SwapableTokens = new[] { "LYR", "TLYR" };

			tokenName = "LYR";
			altDisplay = "**************************";
        }

		private void ToggleKey(MouseEventArgs e)
		{
			if (altDisplay == "**************************")
				altDisplay = walletState?.Value?.wallet?.PrivateKey;
			else
				altDisplay = "**************************";
		}

		private void CloseWallet(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new WebWalletCloseAction());
		}

		private void CreateWallet(MouseEventArgs e)
        {
			Dispatcher.Dispatch(new WebWalletCreateAction());
		}

		private async void RestoreWallet(MouseEventArgs e)
		{
			if(string.IsNullOrWhiteSpace(prvKey))
            {
				await iJS.InvokeAsync<object>("alert", "Private Key can't be empty.");
				return;
			}
            else
            {
				try
                {
					Base58Encoding.DecodePrivateKey(prvKey);
					Dispatcher.Dispatch(new WebWalletRestoreAction { privateKey = prvKey, selfVote = this.selfVote });
				}
				catch (Exception)
                {
					await iJS.InvokeAsync<object>("alert", "Private Key specified is not valid.");
					return;
				}
            }
		}

		private void Refresh(MouseEventArgs e)
        {
			Dispatcher.Dispatch(new WebWalletRefreshBalanceAction { wallet = walletState.Value.wallet });
        }

		private async void Send(MouseEventArgs e)
		{
			if(walletState.Value.wallet.BaseBalance > 1)
				Dispatcher.Dispatch(new WebWalletSendAction {   });
			else
				await iJS.InvokeAsync<object>("alert", "Nothing to send.");
		}

		private void SendToken(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new WebWalletSendTokenAction { DstAddr = dstAddr, TokenName = tokenName, Amount = amount, wallet = walletState.Value.wallet });
		}

		private void CancelSend(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new WebWalletCancelSendAction ());
		}

		private void Settings(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new WebWalletSettingsAction { });
		}

		private void SaveSettings(MouseEventArgs e)
        {
			Dispatcher.Dispatch(new WebWalletSaveSettingsAction { VoteFor = voteAddr });
        }

		private void CancelSave(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new WebWalletCancelSaveSettingsAction { });
		}

		private void Transactions(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new WebWalletTransactionsAction { wallet = walletState.Value.wallet });
		}

		private void Return(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new WebWalletCancelSaveSettingsAction { });
		}

		private void FreeToken(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new WebWalletFreeTokenAction { faucetPvk = Configuration["faucetPvk"] });
		}

		// swap
		private void Swap(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new WebWalletSwapAction { wallet = walletState.Value.wallet });
			UpdateSwapFromBalance();
		}

		[Function("transfer", "bool")]
		public class TransferFunction : FunctionMessage
		{
			[Nethereum.ABI.FunctionEncoding.Attributes.Parameter("address", "_to", 1)]
			public string To { get; set; }

			[Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "_value", 2)]
			public BigInteger TokenAmount { get; set; }
		}

		private async void SwapToken(MouseEventArgs e)
		{
			//Dispatcher.Dispatch(new WebWalletSwapAction { wallet = walletState.Value.wallet });
			var sbLog = new StringBuilder();
			swapResultMessage = "";
			try
            {
				if (_swapFromCount < 2 || swapFromToken == swapToToken)
				{
					sbLog.AppendLine("Unable to swap.");
					return;
				}
				if (swapFromToken == "LYR" && swapToToken == "TLYR")
				{
					var syncResult = await walletState.Value.wallet.Sync(null);
					if (syncResult == Lyra.Core.Blocks.APIResultCodes.Success)
					{
						var sendResult = await walletState.Value.wallet.Send(swapToCount,
							swapOptions.CurrentValue.lyrPub, "LYR");

						if (sendResult.ResultCode == Lyra.Core.Blocks.APIResultCodes.Success)
						{
							var url = swapOptions.CurrentValue.ethUrl;
							var privateKey = swapOptions.CurrentValue.ethPvk;
							var account = new Account(privateKey);
							var web3 = new Web3(account, url);

                            //var web3 = new Nethereum.Web3.Web3();
                            //web3.Client.OverridingRequestInterceptor = metamaskInterceptor;

                            var transactionMessage = new TransferFunction
                            {
                                FromAddress = swapOptions.CurrentValue.ethPub,
                                To = swapToAddress,
                                TokenAmount = new BigInteger(swapToCount * 100000000)   // 10^8 
                            };

                            var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();
                            var transferReceipt = await transferHandler.SendRequestAndWaitForReceiptAsync(swapOptions.CurrentValue.ethContract, transactionMessage);

                            var transaction = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transferReceipt.TransactionHash);
                            var transferDecoded = transaction.DecodeTransactionToFunctionMessage<TransferFunction>();

							sbLog.AppendLine("Swap succeed!");
						}
						else
							throw new Exception("Unable to send from your wallet.");
					}
					else
						throw new Exception("Unable to sync Lyra Wallet.");
				}

				if (swapFromToken == "TLYR" && swapToToken == "LYR")
				{
                    var web3 = new Web3();
                    web3.Client.OverridingRequestInterceptor = metamaskInterceptor;

                    var transactionMessage = new TransferFunction
					{
						FromAddress = SelectedAccount,
						To = swapOptions.CurrentValue.ethPub,
						TokenAmount = new BigInteger(swapFromCount * 100000000)   // 10^8 
					};

					var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();
					var transferReceipt = await transferHandler.SendRequestAndWaitForReceiptAsync(swapOptions.CurrentValue.ethContract, transactionMessage);

					var transaction = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transferReceipt.TransactionHash);
					var transferDecoded = transaction.DecodeTransactionToFunctionMessage<TransferFunction>();

					if(true) // test if success transfer
                    {
						var store = new AccountInMemoryStorage();
						var wallet = Wallet.Create(store, "default", "", Configuration["network"],
							swapOptions.CurrentValue.lyrPvk);
						var lyraClient = LyraRestClient.Create(Configuration["network"], Environment.OSVersion.ToString(),
							"Nebula Swap", "1.0");

						var syncResult = await wallet.Sync(lyraClient);
						if (syncResult == Lyra.Core.Blocks.APIResultCodes.Success)
						{
							var sendResult = await wallet.Send(swapToCount,
								swapToAddress, "LYR");

							if (sendResult.ResultCode == Lyra.Core.Blocks.APIResultCodes.Success)
							{
								sbLog.AppendLine("Swap succeed!");
							}
							else
								throw new Exception("Unable to send from your wallet.");
						}
						else
							throw new Exception("Unable to sync Lyra Wallet.");
					}

				}
			}
			catch(Exception ex)
            {
				sbLog.AppendLine("Failed in Swap: " + ex.ToString());
            }
			finally
            {
				swapResultMessage = sbLog.ToString();
				logger.LogInformation($"Swap Result: {swapResultMessage}\n\n");
			}
        }

		private void UpdateSwapFromBalance()
        {
			if (string.IsNullOrEmpty(_swapFromTokenName))
				_swapFromTokenName = "LYR";

			if (_swapFromTokenName == "LYR")
			{
				// get lyr balance
				fromTokenBalance = walletState.Value.wallet.BaseBalance;
			}
			else if (_swapFromTokenName == "TLYR")
			{
				fromTokenBalance = 1.111111111m;
			}
		}

		private void UpdateSwapToBalance()
		{
			if (_swapFromTokenName == "LYR")
			{
				swapToCount = swapFromCount - 1; // remember -GAS
			}
			else if (_swapFromTokenName == "TLYR")
			{
				swapToCount = 2.222222m;
			}
		}
	}
}
