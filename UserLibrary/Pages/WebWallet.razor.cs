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
using Nebula.Store.WebWalletUseCase;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UserLibrary.Components;

namespace UserLibrary.Pages
{
	public partial class WebWallet
	{
		[Inject]
		private IState<WebWalletState> walletState { get; set; }

		[Inject]
		private IDispatcher Dispatcher { get; set; }

		[CascadingParameter]
		public Error Error { get; set; }

		public string stkName { get; set; }
		public string stkVoting { get; set; }
		public string stkDays { get; set; }
		public bool stkCompound { get; set; }

		public string pftName { get; set; }
		public string pftType { get; set; }
		public string pftShare { get; set; }
		public string pftSeats { get; set; }

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
			tokenName = "LYR";
			altDisplay = "************";

			pftType = "Node";
		}

        protected override void OnInitialized()
        {
            base.OnInitialized();
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
			altDisplay = "************";

			Dispatcher.Dispatch(new WebWalletCreateAction());
		}

		private async void RestoreWallet(MouseEventArgs e)
		{
			altDisplay = "************";

			if (string.IsNullOrWhiteSpace(prvKey))
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

		private void Staking(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new WebWalletStakingAction { wallet = walletState.Value.wallet });
		}

		private void ClearError()
		{
			Dispatcher.Dispatch(new WalletErrorResetAction());
		}

		private void StakingCreate(MouseEventArgs e)
        {
			try
            {
				Dispatcher.Dispatch(new WebWalletCreateStakingAction
				{
					wallet = walletState.Value.wallet,
					name = stkName,
					voting = stkVoting,
					days = int.Parse(stkDays),
					compound = stkCompound
				});
			}
			catch (Exception ex)
			{
				Dispatcher.Dispatch(new WalletErrorResultAction { error = ex.Message });
			}
		}

		private void ProfitingCreate(MouseEventArgs e)
		{
			try
            {
				if (pftType != "Node" && pftType != "Yield")
					return;

				var type = (ProfitingType)Enum.Parse(typeof(ProfitingType), pftType);

				Dispatcher.Dispatch(new WebWalletCreateProfitingAction
				{
					wallet = walletState.Value.wallet,
					name = pftName,
					type = type,
					share = decimal.Parse(pftShare) / 100m,
					seats = int.Parse(pftSeats)
				});
			}
			catch(Exception ex)
            {
				Dispatcher.Dispatch(new WalletErrorResultAction { error = ex.Message });
            }
		}

		private async Task AddStkAsync(MouseEventArgs e, string stkid)
		{
			try
            {
				var amt = await GetAmountInput();
				if(amt > 0)
                {
					Dispatcher.Dispatch(new WebWalletAddStakingAction
					{
						wallet = walletState.Value.wallet,
						stkid = stkid,
						amount = amt
					});
				}
			}
			catch (Exception ex)
			{
				Dispatcher.Dispatch(new WalletErrorResultAction { error = ex.Message });
			}
		}

		private void RmStk(MouseEventArgs e, string stkid)
		{
			try
			{
				Dispatcher.Dispatch(new WebWalletRemoveStakingAction
				{
					wallet = walletState.Value.wallet,
					stkid = stkid
				});
			}
			catch (Exception ex)
			{
				Dispatcher.Dispatch(new WalletErrorResultAction { error = ex.Message });
			}
		}

		private async Task Send(MouseEventArgs e)
		{
			if(walletState.Value.wallet.BaseBalance > 1)
				Dispatcher.Dispatch(new WebWalletSendAction {   });
			else
				await JS.InvokeAsync<object>("alert", "Nothing to send.");
		}

		private async Task SendX(string name)
		{
			tokenName = name;
			if (walletState.Value.wallet.BaseBalance > 1)
				Dispatcher.Dispatch(new WebWalletSendAction { });
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

		private void StartDex(MouseEventArgs e)
		{		
			Dispatcher.Dispatch(new WebWalletStartDexAction());
		}
	}
}
