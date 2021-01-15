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

		protected override async Task HandleAsync(NodeViewAction action, IDispatcher dispatcher)
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
					var syncState = await lcx.GetSyncState();
					bag.TryAdd(node.Key, syncState);
				}
				catch(Exception ex)
                {
					bag.TryAdd(node.Key, null);
                }
			});
			await Task.WhenAll(tasks);

			dispatcher.Dispatch(new NodeViewResultAction(config["network"], bb, bag, config["ipdb"]));
		}
	}
}
