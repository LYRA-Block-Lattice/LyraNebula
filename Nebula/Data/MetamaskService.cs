﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Nebula.Pages;
using Nethereum.Contracts.TransactionHandlers;

namespace Nethereum.Metamask.Blazor
{
    public class MetamaskService
    {
        private readonly IMetamaskInterop _metamaskInterop;
        public static MetamaskService Current { get; private set; }
        public bool IsMetamaskAvailable { get; private set; }
        public bool IsEthereumEnabled { get; private set; }

        public string SelectedAccount { get; private set; }

        public event Func<bool, Task> MetamaskAvailable;
        public event Func<bool, Task> EthereumEnabled;
        public event Func<string, Task> SelectedAccountChanged;

        public MetamaskService(IMetamaskInterop metamaskInterop)
        {
            _metamaskInterop = metamaskInterop;
            Current = this;
        }
        public async ValueTask<bool> EnableEthereumAsync()
        {
            var result = await _metamaskInterop.EnableEthereumAsync();
            IsEthereumEnabled = result;
            if (EthereumEnabled != null)
            {
                await EthereumEnabled.Invoke(result);
            }
            return result;
        }

        public async Task<bool> RegisterEventsAsync(DotNetObjectReference<WebWallet> dotNetObjRef)
        {
            var result = await _metamaskInterop.RegisterHandler(dotNetObjRef);
            return result == "registered!";
        }

        public async ValueTask<string> GetChainName()
        {
            var result = await _metamaskInterop.GetChainId();
            
            switch(result)
            {
                case "0x1": 
                    return "Ethereum Main Network";
                case "0x3":
                    return "Ropsten Test Network";
                case "0x4":
                    return "Rinkeby Test Network";
                case "0x5":
                    return "Goerli Test Network";
                case "0x42":
                    return "Kovan Test Network";
                default:
                    return "Unknown";
            }                
        }

        public async ValueTask<string> GetSelectedAccount()
        {
            var result = await _metamaskInterop.GetSelectedAddress();
            await ChangeSelectedAccountAsync(result);
            return result;
        }

        public async Task ChangeSelectedAccountAsync(string selectedAccount)
        {
            if (SelectedAccount != selectedAccount)
            {
                SelectedAccount = selectedAccount;
                if (SelectedAccountChanged != null)
                {
                    await SelectedAccountChanged.Invoke(selectedAccount);
                }
            }
        }

        public async ValueTask<bool> CheckMetamaskAvailability()
        {
            var result = await _metamaskInterop.CheckMetamaskAvailability();
            await ChangeMetamaskAvailableAsync(result);
            return result;
        }

        public async Task ChangeMetamaskAvailableAsync(bool available)
        {
            IsMetamaskAvailable = available;
            if (MetamaskAvailable != null)
            {
                await MetamaskAvailable.Invoke(available);
            }
        }

    }
}
