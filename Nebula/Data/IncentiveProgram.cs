using Fluxor;
using Lyra.Core.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Nebula.Store.NodeViewUseCase;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nebula.Data
{
    public class IncentiveProgram : BackgroundService
    {
        private IConfiguration config;
        private INodeHistory History;
        private LyraRestClient client;
        public IncentiveProgram(IConfiguration configuration, INodeHistory history)
        {
            config = configuration;
            History = history;

            client = LyraRestClient.Create(config["network"], Environment.OSVersion.ToString(), "Nebula", "1.4");
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(3 * 60 * 60 * 1000);

                try
                {
                    await RefreshAsync();
                }
                catch { }          
            }
        }

        private async Task RefreshAsync()
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
                    catch (Exception ex)
                    {
                        bag.TryAdd(node.Key, null);
                    }
                });
            await Task.WhenAll(tasks);

            var nvs = new NodeViewState(
        isLoading: false,
        billBoard: bb,
        NodeStatus: bag,
        ipdb: config["ipdb"]);

            nvs.Id = 0;     // create new for liteDB
            nvs.TimeStamp = DateTime.UtcNow;

            History.Insert(nvs);
        }
    }
}
