using Lyra.Core.API;
using Lyra.Core.Blocks;
using Lyra.Data.API;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nebula.Data;
using Nebula.Store.NodeViewUseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Humanizer;
using System.Collections.Concurrent;
using Lyra.Data.Blocks;
using Converto;
using IP2Country.MarkusGo;
using IP2Country;
using Polly.RateLimit;
using Polly;
using System.Xml.Linq;
using MongoDB.Driver;

namespace Nebula.Pages
{
	public partial class ShowBillBoard
	{
        protected bool IsDisabled { get; set; } = true;

		//[Inject]
		//private INodeHistory History { get; set; }

		[Inject]
		private ILogger logger { get; set; }

		[Inject]
		ILyraAPI client { get; set; }

		private int selId;		

        //public IProfiting pft { get; set; }
        //public List<Staker> stks { get; set; }
        //public ProfitingStats pftStats { get; set; }
        //public Dictionary<string, decimal> stkRewards { get; set; }

        NodeViewState latestState;

        class UIMsg
        {
            public string errmsg { get; set; }
            public bool busy { get; set; }
        }
        ConcurrentDictionary<string, UIMsg> errmsgs = new ConcurrentDictionary<string, UIMsg>();

        //public decimal TotalStaking => latestState.bb == null ? 0 : latestState.bb.ActiveNodes.Sum(a => a.Votes);

        private int SelectedHistId
        {
            get
            {
                return selId;
            }
            set
            {
                selId = value;
				//var recd = History.FindOne(selId);
				//Dispatcher.Dispatch(new LoadHistoryAction { historyState = recd });
				StateHasChanged();
			}
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if(firstRender)
			{
                try
                {
                    IsDisabled = true;

                    await RefreshNodeStatusAsync();

                    //BriefHist = Enumerable.Empty<HistInfo>();

                    //// load history first
                    //latestState = History.FindLatest();
                    //if (latestState == null)
                    //{
                    //    await RefreshNodeStatusAsync();
                    //}

                    //BriefHist = History.FindHistory(3);
                    //if (BriefHist.Any())
                    //{                        
                    //    selId = BriefHist.First().id;
                    //}                        

                    //var lcx = LyraRestClient.Create(Configuration["network"], Environment.OSVersion.ToString(), "Nebula", "1.4");
                    //pfts = await lcx.FindAllProfitingAccountsAsync(DateTime.MinValue, DateTime.MaxValue);

                    IsDisabled = false;
                    await InvokeAsync(() => StateHasChanged());
                }
                catch (Exception ex)
                {

                }
            }

			await base.OnAfterRenderAsync(firstRender);
		}

        private async Task RefreshNodeStatusAsync()
        {
            // TODO: optimize. use a AP seed (after late seeds migration) or even a LAN one to get billboard and pft accounts.

            int port = 4504;
            if (Configuration["network"].Equals("mainnet", StringComparison.InvariantCultureIgnoreCase))
                port = 5504;

            var lcx = LyraRestClient.Create(Configuration["network"], Environment.OSVersion.ToString(), "Nebula", "1.4");//
                        //$"https://192.168.222.50:{port}/api/Node/");

            var bb = await lcx.GetBillBoardAsync();

            //RateLimitPolicy<GetSyncStateAPIResult> rateLimitOfT = Policy
            //        .RateLimit<GetSyncStateAPIResult>(20, TimeSpan.FromSeconds(1));

            var bag = new ConcurrentDictionary<string, GetSyncStateAPIResult>();
            var rand = new Random();
            var allstart = DateTime.Now;
            var tasks = bb.NodeAddresses
                .Select(async node =>
                {
                    var start = DateTime.Now;
                    var addr = node.Value.Contains(':') ? node.Value : $"{node.Value}:{port}";
                    var lcx = LyraRestClient.Create(Configuration["network"], Environment.OSVersion.ToString(), "Nebula", "1.4", $"https://{addr}/api/Node/");
                    lcx.SetTimeout(TimeSpan.FromSeconds(25));

                    try
                    {
                        await Task.Delay(rand.Next(0, 500));
                        var syncState = await lcx.GetSyncStateAsync();
                        bag.TryAdd(node.Key, syncState);
                        var umsg = new UIMsg { errmsg = "Success", busy = false };
                        errmsgs.AddOrUpdate(node.Key, umsg, (key, oldvalue) => umsg);
                    }
                    catch (Exception ex)
                    {
                        bag.TryAdd(node.Key, null);
                        var umsg = new UIMsg { errmsg = ex.Message, busy = false };
                        errmsgs.AddOrUpdate(node.Key, umsg, (key, oldvalue) => umsg);
                    }
                    var ts = DateTime.Now - start;
                    Console.WriteLine($"Node {node.Value} uses {ts.Milliseconds} ms.");
                });

            var pftstsk = lcx.FindAllProfitingAccountsAsync(DateTime.MinValue, DateTime.MaxValue);

            await Task.WhenAll(tasks.Concat(new[] { pftstsk }));

            var allts = DateTime.Now - allstart;
            Console.WriteLine($"All uses {allts.Milliseconds} ms.");

            var nvs = new NodeViewState(
                isLoading: false,
                billBoard: bb,
                NodeStatus: bag);

            nvs.Id = 0;     // create new for liteDB
            nvs.TimeStamp = DateTime.UtcNow;

            nvs.pfts = pftstsk.Result;

            latestState = nvs;
            //History.Insert(nvs);
        }

        bool allbusy = false;
        private async Task RefreshAll()
        {
            allbusy = true;
            await InvokeAsync(() => StateHasChanged());

            if (errmsgs.Values.Any(a => a.busy))
                return;

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 6
            };

            var rand = new Random();
            await Parallel.ForEachAsync(errmsgs.Keys, parallelOptions, async (act, token) =>
            {
                await Task.Delay(rand.Next(0, 50));
                await RefreshNode(act);
            });

            allbusy = false;
            await InvokeAsync(() => StateHasChanged());
        }

        private async Task RefreshNode(string accountId)
        {
            // get the row
            var ui = errmsgs[accountId];
            ui.errmsg = "Updating...";
            ui.busy = true;
            await InvokeAsync(() => StateHasChanged());

            var ep = latestState.bb.NodeAddresses.FirstOrDefault(a => a.Key == accountId).Value;

            int port = 4504;
            if (Configuration["network"].Equals("mainnet", StringComparison.InvariantCultureIgnoreCase))
                port = 5504;
            var addr = ep.Contains(':') ? ep : $"{ep}:{port}";

            var lcx = LyraRestClient.Create(Configuration["network"], Environment.OSVersion.ToString(), "Nebula", "1.4", $"https://{addr}/api/Node/");
            lcx.SetTimeout(TimeSpan.FromSeconds(25));

            try
            {
                var syncState = await lcx.GetSyncStateAsync();
                latestState.nodeStatus.AddOrUpdate(accountId, syncState, (key, oldvalue) => syncState);
                var umsg = new UIMsg { errmsg = "Success", busy = false };
                errmsgs.AddOrUpdate(accountId, umsg, (key, oldvalue) => umsg);
            }
            catch (Exception ex)
            {
                latestState.nodeStatus.AddOrUpdate(accountId, (GetSyncStateAPIResult)null, (key, oldvalue) => null);

                var umsg = new UIMsg { errmsg = ex.Message, busy = false };
                errmsgs.AddOrUpdate(accountId, umsg, (key, oldvalue) => umsg);
            }

            await InvokeAsync(() => StateHasChanged());
        }

        private async Task Refresh(MouseEventArgs e)
		{
			try
			{
                var start = DateTime.Now;
				//logger.LogInformation("Refreshing all nodes...");
				IsDisabled = true;
				StateHasChanged();

				await RefreshNodeStatusAsync();
                //latestState = History.FindLatest();

                IsDisabled = false;
                StateHasChanged();

                var end = DateTime.Now;
                logger.LogInformation($"Refresh all nodes uses {(end - start).Humanize()}");
            }
			catch(Exception ex)
			{
                logger.LogError($"Error refresh all nodes: {ex}");
            }
		}

        public List<NodeInfoSet> GetRankedList(IConfiguration Config)
        {
            var list = new List<NodeInfoSet>();
            foreach (var id in latestState.bb.PrimaryAuthorizers)
            {
                if (latestState.bb.ActiveNodes.Any(a => a.AccountID == id) && latestState.nodeStatus.ContainsKey(id))       // bug in billboard. or error-proof
                {
                    var x = latestState.bb.ActiveNodes.FirstOrDefault(a => a.AccountID == id);
                    decimal vts = x == null ? 0 : x.Votes;
                    list.Add(new NodeInfoSet
                    {
                        ID = id,
                        IsPrimary = true,
                        Votes = (long)vts,
                        Status = latestState.nodeStatus[id]
                    });
                }
            }

            var list2 = new List<NodeInfoSet>();
            var nonPrimaryNodes = latestState.nodeStatus.Where(a => !latestState.bb.PrimaryAuthorizers.Contains(a.Key));
            foreach (var node in nonPrimaryNodes)
            {
                var x = latestState.bb.ActiveNodes.FirstOrDefault(a => a.AccountID == node.Key);
                decimal vts = x == null ? 0 : x.Votes;
                list2.Add(new NodeInfoSet
                {
                    ID = node.Key,
                    IsPrimary = false,
                    Votes = (long)vts,
                    Status = node.Value
                });
            }

            list.AddRange(list2);

            var result = list
                .Where(a => latestState.bb.ActiveNodes.Any(b => b.AccountID == a.ID))
                .OrderByDescending(a => a.Votes)
                .ThenBy(b => b.ID)
                .Zip(Enumerable.Range(1, int.MaxValue - 1),
                                  (o, i) => o.With(new { Index = i }))
                .ToList();

            // lookup IP geo location
            try
            {
                var ipDbFn = Environment.ExpandEnvironmentVariables(Config["ipdb"]);
                var resolver = new IP2CountryBatchResolver(new IP2CountryResolver(
                    new MarkusGoCSVFileSource(ipDbFn)
                ));

                var hostlist = result.Select(a => latestState.bb.NodeAddresses[a.ID]).ToList();
                Parallel.For(0, hostlist.Count, i => hostlist[i] = Name2IP(hostlist[i]));
                var geoList = resolver.Resolve(hostlist);
                for (int i = 0; i < result.Count; i++)
                {
                    result[i].Country = geoList[i] == null ? "" : geoList[i].Country;
                }
            }
            catch (Exception ex)
            { }

            return result;
        }

        private string Name2IP(string hostName)
        {
            try
            {
                var uri = new Uri("http://" + hostName);
                if (uri.HostNameType == UriHostNameType.IPv4)
                    return uri.Host;

                if (uri.HostNameType == UriHostNameType.Dns)
                    return Dns.GetHostEntry(uri.Host).AddressList[0].ToString();

                return hostName;
            }
            catch
            {
                return hostName;
            }
        }

        //private void NodeState_StateChanged(object sender, EventArgs ex)
        //      {
        //	var e = NodeState.Value;
        //	if(e.Id == 0 && e.TimeStamp != default(DateTime))
        //          {
        //		try
        //              {
        //			History.Insert(e);

        //			BriefHist = History.FindHistory(3);
        //			if (BriefHist.Any())
        //				selId = BriefHist.First().id;

        //                  IsDisabled = false;
        //              }
        //              catch { }
        //	}

        //          StateHasChanged();
        //      }

        //      protected override void OnAfterRender(bool firstRender)
        //      {
        //	if(firstRender)
        //          {
        //		IsDisabled = false;
        //		StateHasChanged();
        //	}
        //}

        private string GetProfitingAccount(string posAccount)
		{
			var target = latestState.bb?.ActiveNodes.FirstOrDefault(a => a.AccountID == posAccount);
			if (target == null)
				return "";
			else
				return target.ProfitingAccountId;
		}
		private string GetProfitingAccountShort(string posAccount)
		{
			var acct = GetProfitingAccount(posAccount);
			if (acct?.Length > 10)
				return acct.Substring(0, 10);
			else
				return acct;
		}

		private string GetProfitingAccountName(string posAccount)
		{
			if (latestState.pfts == null)
				return "";

			var acct = latestState.pfts.FirstOrDefault(a => a.gens.OwnerAccountId == posAccount);
			if (acct == null)
				return "";

			return acct.gens.Name;
		}

		private string GetStakingAmount(string posAccount)
		{
			var target = latestState.bb.ActiveNodes.FirstOrDefault(a => a.AccountID == posAccount);
			if (target == null)
				return "0";
			else
				return target.Votes.ToString("N0");
		}

		//private void ShowPft(MouseEventArgs e, string owner)
		//{
		//	Dispatcher.Dispatch(new GetProfitAction { owner = owner });
		//}

		//private string GetPftID()
		//      {
		//	if (NodeState.Value.pft == null)
		//		return "";

		//	return ((TransactionBlock)NodeState.Value.pft).AccountID;
		//      }

		//private void Return(MouseEventArgs e)
		//{
		//	Dispatcher.Dispatch(new ReturnToMainAction { });
		//}

		//private decimal GetToOwner()
		//      {
		//	return Math.Round(NodeState.Value.pftStats.Total * (1 - NodeState.Value.pft.ShareRito), 8);
		//      }
		//private decimal GetToStakers()
		//{
		//	return Math.Round(NodeState.Value.pftStats.Total * NodeState.Value.pft.ShareRito, 8);
		//}

		//private string FormatShare()
		//      {
		//	return $"{NodeState.Value.pft.ShareRito * 100} %";
		//      }
	}

}
