using System.Threading.Tasks;
using Microsoft.JSInterop;
using Nebula.Pages;
using Nethereum.JsonRpc.Client.RpcMessages;

namespace Nethereum.Metamask.Blazor
{
    public interface IMetamaskInterop
    {
        ValueTask<bool> EnableEthereumAsync();
        ValueTask<bool> CheckMetamaskAvailability();
        ValueTask<string> RegisterHandler(DotNetObjectReference<WebWallet> dotNetObjRef);
        ValueTask<string> GetChainId();
        ValueTask<string> GetSelectedAddress();
        ValueTask<RpcResponseMessage> SendAsync(RpcRequestMessage rpcRequestMessage);
        ValueTask<RpcResponseMessage> SendTransactionAsync(MetamaskRpcRequestMessage rpcRequestMessage);
    }
}