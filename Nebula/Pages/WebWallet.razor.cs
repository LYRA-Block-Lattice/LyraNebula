﻿using Fluxor;
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
		private void Swap(MouseEventArgs e)
		{
			swapFromToken = "LYR";
			swapToToken = "TLYR";
			Dispatcher.Dispatch(new WebWalletSwapAction ());
			_ = Task.Run(async () => { await UpdateSwapFromBalanceAsync(); });
		}

		private void SwapToken(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new WebWalletSwapTokenAction { 
				wallet = walletState.Value.wallet,

				fromToken = swapFromToken,
				fromAddress = swapFromToken == "LYR" ? walletState.Value.wallet.AccountId : SelectedAccount,
				fromAmount = swapFromCount,

				toToken = swapToToken,
				toAddress = swapToAddress,
				toAmount = swapToCount,

				options = swapOptions.CurrentValue,
				metamask = metamaskInterceptor
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
				fromTokenBalance = await SwapUtils.GetEthContractBalanceAsync(swapOptions.CurrentValue.ethUrl,
                    swapOptions.CurrentValue.ethContract, SelectedAccount);

                await InvokeAsync(() =>
                {
                    StateHasChanged();
                });
			}
		}

		private void UpdateSwapToBalance()
		{
			if (_swapToTokenName == "TLYR")
			{
				swapToCount = swapFromCount - 1; // remember -GAS
				swapToAddress = SelectedAccount;
			}
			else if (_swapToTokenName == "LYR")
			{
				swapToCount = swapFromCount - 1; // remember -GAS
				swapToAddress = walletState.Value.wallet.AccountId;
			}
		}
	}
}
