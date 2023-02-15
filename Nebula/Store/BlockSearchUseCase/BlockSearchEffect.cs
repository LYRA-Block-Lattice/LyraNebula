using Fluxor;
using Microsoft.AspNetCore.Components;
using Nebula.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Lyra.Core.API;
using Lyra.Core.Blocks;
using Lyra.Data.API;

namespace Nebula.Store.BlockSearchUseCase
{
	public class BlockSearchEffect : Effect<BlockSearchAction>
	{
		private readonly ILyraAPI client;

		public BlockSearchEffect(ILyraAPI lyraClient)
		{
			client = lyraClient;
		}

		public override async Task HandleAsync(BlockSearchAction action, IDispatcher dispatcher)
		{
			var hashToSearch = action.hash;
			Block blockResult = null;
			Block prevBlock = null;
			long maxHeight = 0;
			string key = null;
			string errmsg = null;
			if(string.IsNullOrWhiteSpace(hashToSearch))
            {
				var genSvcRet = await client.GetLastConsolidationBlockAsync();
				if(genSvcRet.ResultCode == APIResultCodes.Success)
                {
					blockResult = genSvcRet.GetBlock();
				}
            }
			else
            {
				BlockAPIResult ret = null;
				if(hashToSearch.Length < 40)
                {
					ret = await client.GetServiceBlockByIndexAsync(action.hash, action.height);
                }
				else if(hashToSearch.Length == 44 || hashToSearch.Length == 43)	// hash
                {
					ret = await client.GetBlockAsync(action.hash);
				}
				else
                {
					var exists = await client.GetAccountHeightAsync(action.hash);
					if(exists.ResultCode == APIResultCodes.Success)
                    {
						maxHeight = exists.Height;
						key = action.hash;
						ret = await client.GetBlockByIndexAsync(action.hash, action.height == 0 ? exists.Height : action.height);
                    }
					else
					{
						errmsg = exists.ResultMessage;
					}
                }
				
				if (ret != null && ret.ResultCode == APIResultCodes.Success)
				{
					blockResult = ret.GetBlock();
				}
			}

			(key, maxHeight) = await GetMaxHeightAsync(blockResult);

			try
			{
				if(blockResult != null)
					prevBlock = blockResult.PreviousHash == null ? null : (await client.GetBlockAsync(blockResult.PreviousHash)).GetBlock();
			}
			catch (Exception) { }

			dispatcher.Dispatch(new BlockSearchResultAction(blockResult, prevBlock, key, maxHeight, errmsg));
		}

		private async Task<(string, long)> GetMaxHeightAsync(Block block)
        {
			BlockAPIResult lastBlockResult = null;
			switch(block)
            {
				case ServiceBlock sb:
					lastBlockResult = await client.GetLastServiceBlockAsync();
					break;
				case ConsolidationBlock cb:
					lastBlockResult = await client.GetLastConsolidationBlockAsync();
					break;
				case TransactionBlock tb:
					var tbLastResult = await client.GetAccountHeightAsync(tb.AccountID);
					if (tbLastResult.ResultCode == APIResultCodes.Success)
						return (tb.AccountID, tbLastResult.Height);
					break;
				default:
					break;
            }

			if (lastBlockResult != null && lastBlockResult.ResultCode == APIResultCodes.Success)
            {
				var lb = lastBlockResult.GetBlock();
				return (lb.BlockType.ToString(), lb.Height);
			}				
			else
				return (null, 0);
        }
	}
}
