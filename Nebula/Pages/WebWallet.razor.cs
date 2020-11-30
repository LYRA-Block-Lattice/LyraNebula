using CoinGecko.Clients;
using CoinGecko.Interfaces;
using Fluxor;
using Lyra.Core.Accounts;
using Lyra.Core.API;
using Lyra.Core.Blocks;
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
		protected decimal ethGasFee { get; set; }
		protected decimal lyraPrice { get; set; }
		protected decimal serviceFee { get; set; }
		protected bool IsDisabled { get; set; }
		public string queryingNotify { get; set; }
		public decimal lyrReserveBalance { get; set; }
		public decimal tlyrReserveBalance { get; set; }
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
				swapFromCount = 0;
				_ = Task.Run(async () => { await UpdateSwapFromBalanceAsync(); });				
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
		private async Task Swap(MouseEventArgs e)
		{
			swapFromToken = "LYR";
			swapToToken = "TLYR";
			IsDisabled = false;
			swapResultMessage = "";
			walletState.Value.Message = "";
			await InvokeAsync(() =>
			{
				StateHasChanged();
			});
			Dispatcher.Dispatch(new WebWalletSwapAction ());
			_ = Task.Run(async () => { await UpdateSwapBalanceAsync(); });
		}

		private async Task SwapToken(MouseEventArgs e)
		{
			IsDisabled = true;
			swapResultMessage = "Checking for configurations...";
			await InvokeAsync(() =>
			{
				StateHasChanged();
			});

			// do check		
			swapResultMessage = "";
			if (!EthereumEnabled || !walletState.Value.IsOpening)
				swapResultMessage = "Wallet(s) not opening or connected.";
			else if (swapFromToken == swapToToken)
				swapResultMessage = "No need to swap.";
			else if (!((swapFromToken == "LYR" && swapToToken == "TLYR") || (swapFromToken == "TLYR" && swapToToken == "LYR")))
				swapResultMessage = "Unknown token pair";
			else if (swapFromCount < 10)
				swapResultMessage = "Swap amount too small. (< 10)";
			else if (swapFromCount > 1000000)
				swapResultMessage = "Swap amount too large. (> 1M)";
			else if (swapFromToken == "LYR" && swapFromCount > walletState.Value.wallet.BaseBalance)
				swapResultMessage = "Not enough LYR in Lyra Wallet.";
			else if(swapFromToken == "TLYR")
            {
				try
                {
					var tlyrBalance = await SwapUtils.GetEthContractBalanceAsync(swapOptions.CurrentValue.ethUrl,
						swapOptions.CurrentValue.ethContract, SelectedAccount);
					if (tlyrBalance < swapFromCount)
						swapResultMessage = "Not enough TLYR in Ethereum Wallet.";
				}
				catch(Exception)
                {
					swapResultMessage = "Unable to get TLYR balance.";
				}
			}

			if(!string.IsNullOrEmpty(swapResultMessage))
            {
				IsDisabled = false;
				await InvokeAsync(() =>
				{
					StateHasChanged();
				});
				return;
			}

			swapResultMessage = "Do swapping... please wait...";
			walletState.Value.Message = "";
			await InvokeAsync(() =>
			{
				StateHasChanged();
			});

			var arg = new WebWalletSwapTokenAction
			{
				wallet = walletState.Value.wallet,

				fromToken = swapFromToken,
				fromAddress = swapFromToken == "LYR" ? walletState.Value.wallet.AccountId : SelectedAccount,
				fromAmount = swapFromCount,

				toToken = swapToToken,
				toAddress = swapToAddress,
				toAmount = swapToCount,

				options = swapOptions.CurrentValue,
				metamask = metamaskInterceptor
			};
			logger.LogInformation($"Begin swapping {arg.fromAmount} from {arg.fromAddress} to {arg.toAddress} amount {arg.toAmount}");
			Dispatcher.Dispatch(arg);
			IsDisabled = true;
		}

		private async Task UpdateSwapBalanceAsync()
        {
			queryingNotify = "Querying balance ...";
			await InvokeAsync(() =>
			{
				StateHasChanged();
			});

			lyrReserveBalance = await SwapUtils.GetLyraBalanceAsync(Configuration["network"], swapOptions.CurrentValue.lyrPvk);
			tlyrReserveBalance = await SwapUtils.GetEthContractBalanceAsync(swapOptions.CurrentValue.ethUrl,
					swapOptions.CurrentValue.ethContract, swapOptions.CurrentValue.ethPub);

   //         var ethGas = await SwapUtils.EstimateEthTransferFeeAsync(swapOptions.CurrentValue.ethUrl,
   //                 swapOptions.CurrentValue.ethContract, swapOptions.CurrentValue.ethPub);

   //         ICoinGeckoClient _client;
   //         _client = CoinGeckoClient.Instance;
   //         const string vsCurrencies = "usd";
   //         var prices = await _client.SimpleClient.GetSimplePrice(new[] { "ethereum", "lyra" }, new[] { vsCurrencies });

			//ethGasFee = (decimal)((double)(ethGas / 10000000000) * prices["ethereum"]["usd"].GetValueOrDefault());
			//lyraPrice = (decimal) prices["lyra"]["usd"].Value;

			queryingNotify = "";
			await InvokeAsync(() =>
			{
				StateHasChanged();
			});
		}

		private async Task UpdateSwapFromBalanceAsync()
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
				fromTokenBalance = 0;
				fromTokenBalance = await SwapUtils.GetEthContractBalanceAsync(swapOptions.CurrentValue.ethUrl,
					swapOptions.CurrentValue.ethContract, SelectedAccount);
			}

			await InvokeAsync(() =>
			{
				StateHasChanged();
			});
		}

		private void UpdateSwapToBalance()
		{
			if (_swapToTokenName == "TLYR")
			{
				swapToCount = swapFromCount - (1 + swapFromCount / 1000);
				swapToAddress = SelectedAccount;
			}
			else if (_swapToTokenName == "LYR")
			{
				swapToCount = swapFromCount - (1 + swapFromCount / 1000);
				swapToAddress = walletState.Value.wallet.AccountId;
			}

            //// calculate fees
            //serviceFee = ethGasFee
            //    + lyraPrice * (swapFromCount * 0.001m + 1);
        }
	}
}
