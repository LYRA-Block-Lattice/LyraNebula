using Fluxor;
using Lyra.Data.Crypto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Nebula.Store.WebWalletUseCase;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [Inject] ISnackbar Snackbar { get; set; }

        [Parameter]
        public string action { get; set; }
        [Parameter]
        public string target { get; set; }

        bool busy, busysend;
        bool showsend;

        // for send
        public string dstAddr { get; set; }
        public string tokenName { get; set; }
        public decimal amount { get; set; }

        // for settings
        public string voteAddr { get; set; }

        public string altDisplay { get; set; }

        string selectedTab = "home";

        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (walletState.Value.wallet == null)
            {
                Navigation.NavigateTo("login");
            }

            if (action != null && action != "send")
            {
                selectedTab = action;
            }

            if (action == "send" && target != null)
            {
                dstAddr = target;
                showsend = true;
            }

            walletState.StateChanged += this.WalletChanged;
        }

        public void Dispose()
        {
            walletState.StateChanged -= this.WalletChanged;
        }

        private void WalletChanged(object sender, WebWalletState wallet)
        {
            busy = false;
            busysend = false;

            if(walletState.Value.wallet != null)
                Snackbar.Add("Wallet Updated.", Severity.Info);
        }

        private Task OnSelectedTabChanged(string name)
        {
            selectedTab = name;

            if (name == "free")
            {
                Dispatcher.Dispatch(new WebWalletSendMeFreeTokenAction
                {
                    wallet = walletState.Value.wallet,
                    faucetPvk = Configuration["faucetPvk"]
                });
            }

            if (name == "send")
            {
                dstAddr = target;
            }

            return Task.CompletedTask;
        }

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
            Navigation.NavigateTo("/login");
        }

        private void ToggleSend()
        {
            showsend = !showsend;
        }

        private void Refresh()
        {
            busy = true;
            Dispatcher.Dispatch(new WebWalletRefreshBalanceAction { wallet = walletState.Value.wallet });
        }

        private async Task OnClickPost()
        {
            Dispatcher.Dispatch(new WebWalletSendMeFreeTokenAction
            {
                wallet = walletState.Value.wallet,
                faucetPvk = Configuration["faucetPvk"]
            });
            return;
        }

        private async Task SendX(string name)
        {
            tokenName = name;
            showsend = true;
        }

        private void SendTokenAsync()
        {
            busysend = true;
            Snackbar.Add("Sending token...", Severity.Info);

            Dispatcher.Dispatch(new WebWalletSendTokenAction { DstAddr = dstAddr, TokenName = tokenName, Amount = amount, wallet = walletState.Value.wallet });
        }

        private void SaveSettings(MouseEventArgs e)
        {
            Dispatcher.Dispatch(new WebWalletSaveSettingsAction { VoteFor = voteAddr });
        }

        private void Transactions(MouseEventArgs e)
        {
            Dispatcher.Dispatch(new WebWalletTransactionsAction { wallet = walletState.Value.wallet });
        }

        private void Return(MouseEventArgs e)
        {
            Dispatcher.Dispatch(new WebWalletCancelSaveSettingsAction { });
        }

    }
}
