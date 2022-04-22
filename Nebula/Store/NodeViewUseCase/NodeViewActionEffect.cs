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
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Lyra.Data.API;
using Lyra.Data.Blocks;
using Lyra.Core.Blocks;

namespace Nebula.Store.NodeViewUseCase
{
	public class NodeViewActionEffect : Effect<NodeViewAction>
	{
		private readonly ILyraAPI client;
		private readonly IConfiguration config;
		private readonly INodeHistory hist;

		public NodeViewActionEffect(ILyraAPI lyraClient, IConfiguration configuration, INodeHistory history)
		{
			client = lyraClient;
			config = configuration;
			hist = history;
		}

		public override async Task HandleAsync(NodeViewAction action, IDispatcher dispatcher)
		{
			int port = 4504;
			if (config["network"].Equals("mainnet", StringComparison.InvariantCultureIgnoreCase))
				port = 5504;

			var bb = await client.GetBillBoardAsync();

			var bag = new ConcurrentDictionary<string, GetSyncStateAPIResult>();
			var tasks = bb.NodeAddresses
				.Select(async node =>
			{
				var lcx = LyraRestClient.Create(config["network"], Environment.OSVersion.ToString(), "Nebula", "1.4", $"https://{node.Value}:{port}/api/Node/");
				try
                {
					var syncState = await lcx.GetSyncStateAsync();
					bag.TryAdd(node.Key, syncState);
				}
				catch(Exception ex)
                {
					bag.TryAdd(node.Key, null);
                }
			});
			await Task.WhenAll(tasks);

			dispatcher.Dispatch(new NodeViewResultAction(bb, bag, config["ipdb"]));
		}
	}

	public class PftActionEffect : Effect<GetProfitAction>
	{
		private readonly ILyraAPI client;
		private readonly IConfiguration config;
		private readonly INodeHistory hist;

		public PftActionEffect(ILyraAPI lyraClient, IConfiguration configuration, INodeHistory history)
		{
			client = lyraClient;
			config = configuration;
			hist = history;
		}

		public override async Task HandleAsync(GetProfitAction action, IDispatcher dispatcher)
		{
			var result = new PftResultAction();

			var lcx = LyraRestClient.Create(config["network"], Environment.OSVersion.ToString(), "Nebula", "1.4");
			try
			{
				var allbrksResult = await lcx.GetAllBrokerAccountsForOwnerAsync(action.owner);
				if(allbrksResult.Successful())
                {
					var allbrks = allbrksResult.GetBlocks();

					result.pft = allbrks.Where(a => a is ProfitingGenesis)
						.FirstOrDefault() as IProfiting;

					if(result.pft is TransactionBlock tb)
                    {
						var sjs = await lcx.FindAllStakingsAsync(tb.AccountID, DateTime.UtcNow);
						result.stks = sjs.Deserialize<List<Staker>>();
						result.stats = await lcx.GetAccountStatsAsync(tb.AccountID, DateTime.MinValue, DateTime.MaxValue);

						var rwds = new Dictionary<string, decimal>();
						foreach (var stk in result.stks)
						{
							var pftid = (result.pft as TransactionBlock).AccountID;
							var stats = await lcx.GetBenefitStatsAsync(pftid, stk.StkAccount, DateTime.MinValue, DateTime.MaxValue);
							rwds.Add(stk.StkAccount, stats.Total);
						}
						result.rewards = rwds;
					}	
				}
			}
			catch (Exception ex)
			{

			}

			dispatcher.Dispatch(result);
		}
	}
}
