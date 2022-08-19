using Fluxor;
using Lyra.Core.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Nebula.Data.Lyra;
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

        private RichList rich;
        public IncentiveProgram(IConfiguration configuration, INodeHistory history, RichList richList)
        {
            config = configuration;
            History = history;
            rich = richList;

            client = LyraRestClient.Create(config["network"], Environment.OSVersion.ToString(), "Nebula", "1.4");
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(5 * 60 * 1000);

            if(config["network"] == "mainnet" || config["network"] == "testnet")      // only server do this.
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshNodeStatusAsync();
                }
                catch { }

                try
                {
                    await rich.Run();
                }
                catch { }

                await Task.Delay(3 * 60 * 60 * 1000);
            }
        }

        private async Task RefreshNodeStatusAsync()
        {
            int port = 4504;
            if (config["network"].Equals("mainnet", StringComparison.InvariantCultureIgnoreCase))
                port = 5504;

            var bb = await client.GetBillBoardAsync();

            var bag = new ConcurrentDictionary<string, GetSyncStateAPIResult>();
            var tasks = bb.NodeAddresses
                .Select(async node =>
                {
                    var addr = node.Value.Contains(':') ? node.Value : $"{node.Value}:{port}";
                    var lcx = LyraRestClient.Create(config["network"], Environment.OSVersion.ToString(), "Nebula", "1.4", $"https://{addr}/api/Node/");
                    try
                    {
                        var syncState = await lcx.GetSyncStateAsync();
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
                NodeStatus: bag);

            nvs.Id = 0;     // create new for liteDB
            nvs.TimeStamp = DateTime.UtcNow;

            History.Insert(nvs);
        }
    }
}
