﻿using CoinGecko.Clients;
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

		[JSInvokable]
		public void ethAccountsChanged(string[] accounts)
		{
			SelectedAccount = accounts[0];

			_ = Task.Run(async () => {
				UpdateSwapToBalance();
				await UpdateSwapFromBalanceAsync();				
			});
		}

		// swap
		protected string swapFeeDesc { get; set; }
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
			altDisplay = "************";
        }

		private void ToggleKey(MouseEventArgs e)
		{
			if (altDisplay == "************")
				altDisplay = walletState?.Value?.wallet?.PrivateKey;
			else
				altDisplay = "************";
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
				await JS.InvokeAsync<object>("alert", "Private Key can't be empty.");
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
					await JS.InvokeAsync<object>("alert", "Private Key specified is not valid.");
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
				await JS.InvokeAsync<object>("alert", "Nothing to send.");
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
			voteAddr = walletState.Value.wallet.VoteFor;
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

		private void SwapToken(MouseEventArgs e)
        {
			Dispatcher.Dispatch(new WebWalletSwapTokenAction());
		}

		// swap tlyr
		private async Task SwapTLYR(MouseEventArgs e)
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
			Dispatcher.Dispatch(new WebWalletSwapTLYRAction ());
			_ = Task.Run(async () => { await UpdateSwapBalanceAsync(); });
		}

		private async Task BeginSwapTLYR(MouseEventArgs e)
		{
			IsDisabled = true;
			swapResultMessage = "Checking for configurations...";
			await InvokeAsync(() =>
			{
				StateHasChanged();
			});

			// do check		
			swapResultMessage = "";

			// check network id
			CurrentChainName = await metamaskService.GetChainName();
			if (Configuration["network"] == "testnet" && CurrentChainName != "Rinkeby Test Network")
				swapResultMessage = "Only Rinkeby Test Network is supported for testnet.";
			else if (Configuration["network"] == "mainnet" && CurrentChainName != "Ethereum Main Network")
				swapResultMessage = "Only Ethereum Main Network is supported for mainnet.";
			else if (!EthereumEnabled || !walletState.Value.IsOpening)
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
			else if (string.IsNullOrWhiteSpace(swapToAddress))
				swapResultMessage = "Not valid swap to address.";
			else if (swapToToken == "TLYR" && swapToCount > 0.8m * tlyrReserveBalance)
				swapResultMessage = "Reserve account of TLYR is running low. Please contact support.";
			else if (swapToToken == "LYR" && swapToCount > 0.8m * lyrReserveBalance)
				swapResultMessage = "Reserve account of LYR is running low. Please contact support.";
			else if (swapToCount < 1)
				swapResultMessage = "Swap to amount too small.";
			else if (swapFromToken == "TLYR")
			{
				try
				{
					var tlyrBalance = await SwapUtils.GetEthContractBalanceAsync(swapOptions.CurrentValue.ethUrl,
						swapOptions.CurrentValue.ethContract, SelectedAccount);
					if (tlyrBalance < swapFromCount)
						swapResultMessage = "Not enough TLYR in Ethereum Wallet.";
				}
				catch (Exception)
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

			var mmmsg = swapFromToken == "TLYR" ? "Please open Matamask and confirm transaction." : "";
				swapResultMessage = $"Do swapping... {mmmsg} Please wait for a moment... ";
			walletState.Value.Message = "";
			await InvokeAsync(() =>
			{
				StateHasChanged();
			});

			var arg = new WebWalletBeginSwapTLYRAction
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
			logger.LogInformation($"TokenSwap: Begin  {arg.fromAmount} from {arg.fromAddress} to {arg.toAddress} amount {arg.toAmount}");
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

		Task _svcFeeCalculationTask;
		private void UpdateSwapToBalance()
		{
			if (_swapToTokenName == "TLYR")
			{
				swapToAddress = SelectedAccount;
			}
			else if (_swapToTokenName == "LYR")
			{
				swapToAddress = walletState.Value.wallet.AccountId;
			}

			if(swapFromCount == 0)
            {
				swapToCount = 0;
				return;
            }

			if (swapToAddress == null || _svcFeeCalculationTask != null)
				return;

			walletState.Value.IsLoading = true;
			swapToCount = 0;
			IsDisabled = true;
			swapFeeDesc = "Calculating swap service fee...";

			_svcFeeCalculationTask = Task.Run(async () => {
				try
				{
					if (_swapToTokenName == "TLYR")
                    {
						ICoinGeckoClient _client;
						_client = CoinGeckoClient.Instance;
						const string vsCurrencies = "usd";
						var priceTask = _client.SimpleClient.GetSimplePrice(new[] { "ethereum", "lyra" }, new[] { vsCurrencies });
						var gasOracleTask = SwapUtils.GetGasOracle(swapOptions.CurrentValue.ethScanApiKey);
						var estimateGasTask = SwapUtils.EstimateEthTransferFeeAsync(swapOptions.CurrentValue.ethUrl,
							swapOptions.CurrentValue.ethContract,
							swapOptions.CurrentValue.ethPub,
							swapToAddress);
						await Task.WhenAll(priceTask, estimateGasTask, gasOracleTask);

						if (priceTask.IsCompletedSuccessfully && estimateGasTask.IsCompletedSuccessfully && gasOracleTask.IsCompletedSuccessfully)
						{
							var prices = priceTask.Result;
							var gasOracle = gasOracleTask.Result;
							var gasLimit = estimateGasTask.Result;

							ethGasFee = (decimal)(gasOracle * (int)gasLimit * prices["ethereum"]["usd"] / 1000000000);
							lyraPrice = (decimal)prices["lyra"]["usd"];

							var totalFee = swapFromCount * 0.0001m + 1 + ethGasFee / lyraPrice;
							var totalfeeInLyra = Math.Round(totalFee, 2);

							//swapFeeDesc = $"Ethereum Gas price: {gasOracle} Gas limit: {gasLimit} ETH price: ${prices["ethereum"]["usd"]} Lyra price: ${prices["lyra"]["usd"].Value} Final fee ${ethGasFee} in LYR: {feeInLyra}";
							swapFeeDesc = $"{totalfeeInLyra} LYR ($ {Math.Round(totalfeeInLyra * lyraPrice, 2)})";
							swapToCount = swapFromCount - totalfeeInLyra;
						}
						else
							throw new Exception("Can't query fee. Network error.");
					}
					else
					{
						var totalFee = swapFromCount * 0.0001m + 1;
						var totalfeeInLyra = Math.Round(totalFee, 2);

						swapFeeDesc = $"{totalfeeInLyra} LYR ($ {Math.Round(totalfeeInLyra * lyraPrice, 2)})";
						swapToCount = swapFromCount - totalfeeInLyra;
					}

					IsDisabled = false;
				}
				catch(Exception ex)
                {
					swapFeeDesc = $"Error: {ex.Message}";
                }

				await InvokeAsync(() =>
				{
					walletState.Value.IsLoading = false;
					StateHasChanged();
					_svcFeeCalculationTask = null;
				});
			});
		}
	}
}
