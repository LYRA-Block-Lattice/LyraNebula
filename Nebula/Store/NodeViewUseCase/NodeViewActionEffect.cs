﻿using Fluxor;
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
using Microsoft.Extensions.Logging;

namespace Nebula.Store.NodeViewUseCase
{
	public class NodeViewActionEffect : Effect<NodeViewAction>
	{
        private readonly ILogger _logger;
        private readonly ILyraAPI client;
		private readonly IConfiguration config;

		public NodeViewActionEffect(ILyraAPI lyraClient, IConfiguration configuration, ILogger logger)
		{
			_logger = logger;
			client = lyraClient;
			config = configuration;
		}

		public override async Task HandleAsync(NodeViewAction action, IDispatcher dispatcher)
		{
			try
			{
                int port = 4504;
                if (config["network"].Equals("mainnet", StringComparison.InvariantCultureIgnoreCase))
                    port = 5504;

                _logger.LogInformation($"Getting billboard...");
                var bb = await client.GetBillBoardAsync();
                _logger.LogInformation($"Got billboard.");

                var bag = new ConcurrentDictionary<string, GetSyncStateAPIResult>();
                var tasks = bb.NodeAddresses
                    .Select(async node =>
                    {
                        var addr = node.Value.Contains(':') ? node.Value : $"{node.Value}:{port}";
                        var lcx = LyraRestClient.Create(config["network"], Environment.OSVersion.ToString(), "Nebula", "1.4", $"https://{addr}/api/Node/");
                        try
                        {
                            lcx.SetTimeout(TimeSpan.FromSeconds(5));
                            var syncState = await lcx.GetSyncStateAsync();
                            bag.TryAdd(node.Key, syncState);
                        }
                        catch (Exception ex)
                        {
                            bag.TryAdd(node.Key, null);
                        }
                    });
                _logger.LogInformation($"Waiting parallel tasks...");
                await Task.WhenAll(tasks);
                _logger.LogInformation($"Parallel tasks finished.");

                dispatcher.Dispatch(new NodeViewResultAction(bb, bag));
            }
			catch(Exception ex)
			{
                _logger.LogError($"Error Parallel Getting all nodes: {ex}");
            }
		}
	}

	public class PftActionEffect : Effect<GetProfitAction>
	{
		private readonly ILyraAPI client;
		private readonly IConfiguration config;

		public PftActionEffect(ILyraAPI lyraClient, IConfiguration configuration)
		{
			client = lyraClient;
			config = configuration;
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
