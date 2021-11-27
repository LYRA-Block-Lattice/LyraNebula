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

namespace UserLibrary.Pages
{
	public partial class WebWallet
	{
		[Inject]
		private IState<WebWalletState> walletState { get; set; }

		[Inject]
		private IDispatcher Dispatcher { get; set; }

		[Inject]
		private IJSRuntime JS { get; set; }


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







		private void OnSuccess()
		{
			Dispatcher.Dispatch(new WebWalletReCAPTCHAValidAction { ValidReCAPTCHA = true });
		}

		private void OnExpired()
		{
			Dispatcher.Dispatch(new WebWalletReCAPTCHAValidAction { ValidReCAPTCHA = false });
		}

		private async Task OnClickPost()
		{
			Dispatcher.Dispatch(new WebWalletSendMeFreeTokenAction
			{
				wallet = walletState.Value.wallet,
				faucetPvk = Configuration["faucetPvk"]
			});
			return;

			/*        if (walletState.Value.ValidReCAPTCHA)
					{
						var response = await reCAPTCHAComponent.GetResponseAsync();
						try
						{
							Dispatcher.Dispatch(new WebWalletReCAPTCHAServerAction { ServerVerificatiing = true });

							var result = await SampleAPI.Post(response);
							if (result.Success)
							{
								Dispatcher.Dispatch(new WebWalletSendMeFreeTokenAction
									{
										wallet = walletState.Value.wallet,
										faucetPvk = Configuration["faucetPvk"]
									});
								//Navigation.NavigateTo("/valid");
							}
							else
							{
								await JS.InvokeAsync<object>("alert", string.Join(", ", result.ErrorCodes));

								Dispatcher.Dispatch(new WebWalletReCAPTCHAServerAction { ServerVerificatiing = false });
							}
						}
						catch (HttpRequestException e)
						{
							await JS.InvokeAsync<object>("alert", e.Message);

							Dispatcher.Dispatch(new WebWalletReCAPTCHAServerAction { ServerVerificatiing = false });
						}
					}*/
		}

		//protected override async Task OnAfterRenderAsync(bool firstRender)
		//{
		//	var key = Configuration["network"] + "freelyr";
		//	if (walletState.Value.freeTokenTimes.HasValue)
		//	{
		//		// if it need save
		//		var oldValue = await localStore.GetItemAsync<string>(key);
		//		int oldCount;
		//		if (oldValue == null || (int.TryParse(oldValue, out oldCount) && oldCount < walletState.Value.freeTokenTimes))
		//		{
		//			await localStore.SetItemAsync(key, walletState.Value.freeTokenTimes.ToString());
		//		}
		//	}
		//	else
		//	{
		//		var oldValue = await localStore.GetItemAsync<string>(key);
		//		int oldCount;
		//		if (oldValue != null && int.TryParse(oldValue, out oldCount))
		//		{
		//			walletState.Value.freeTokenTimes = oldCount;
		//		}
		//		else
		//		{
		//			walletState.Value.freeTokenTimes = 0;
		//		}
		//	}
		//}

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
