window.NethereumMetamaskInterop = {
    EnableEthereum: async () => {
        try {
            await ethereum.enable();
            ethereum.autoRefreshOnNetworkChange = false;
            //ethereum.on("accountsChanged",
            //    function (accounts) {
            //        ethAccountChanged(accounts);
            //        //DotNet.invokeMethodAsync('Nebula', 'ethAccountsChanged', accounts);
            //            //.then(data => {
            //            //    data.push(4);
            //            //    console.log(data);
            //            //});
            //    });
            ethereum.on("networkChanged",
                function (networkId) {
                    window.location.reload();
                });
            return true;
        } catch (error) {
            return false;
        }
    },
    RegisterHandler: (dotNetObjRef) => {
        ethereum.on("accountsChanged",
            function (accounts) {
                dotNetObjRef.invokeMethodAsync("ethAccountsChanged", accounts);
            });

        return "registered!";
    },
    IsMetamaskAvailable: () => {
        if (window.ethereum) return true;
        return false;
    },
    GetSelectedAddress: () => {
        return ethereum.selectedAddress;
    },

    GetChainId: () => {
        return ethereum.chainId;
    },

    Send: async (message) => {
        return new Promise(function (resolve, reject) {
            ethereum.send(JSON.parse(message), function (error, result) {
                console.log(result);
                resolve(JSON.stringify(result));


            });
        });
    }





}