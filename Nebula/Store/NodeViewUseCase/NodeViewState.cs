using Converto;
using IP2Country;
using IP2Country.MarkusGo;
using Lyra.Core.API;
using Lyra.Core.Blocks;
using Lyra.Data.API;
using Lyra.Data.Blocks;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nebula.Store.NodeViewUseCase
{
    public class NodeViewState
    {
        public enum UIStage { Main, Profiting };

        public UIStage UI { get; set; }

        public IProfiting pft { get; set; }
        public List<Staker> stks { get; set; }
        public ProfitingStats pftStats { get; set; }
        public Dictionary<string, decimal> stkRewards { get; set; }

        public bool IsLoading { get; set; }
		public BillBoard bb { get; set; }
		public ConcurrentDictionary<string, GetSyncStateAPIResult> nodeStatus { get; set; }

        public int Id { get; set; }         // used by litedb
        public DateTime TimeStamp { get; set; }     // history

        public decimal TotalStaking => bb == null ? 0 : bb.ActiveNodes.Sum(a => a.Votes);

		public NodeViewState()
        {
			IsLoading = false;
        }

		public NodeViewState(bool isLoading, BillBoard billBoard, ConcurrentDictionary<string, GetSyncStateAPIResult> NodeStatus)
		{
			IsLoading = isLoading;
			bb = billBoard;
			nodeStatus = NodeStatus;
		}

		public List<NodeInfoSet> GetRankedList(IConfiguration Config)
        {
            var list = new List<NodeInfoSet>();
            foreach (var id in bb.PrimaryAuthorizers)
            {
                if (bb.ActiveNodes.Any(a => a.AccountID == id) && nodeStatus.ContainsKey(id))       // bug in billboard. or error-proof
                {
                    var x = bb.ActiveNodes.FirstOrDefault(a => a.AccountID == id);
                    decimal vts = x == null ? 0 : x.Votes;
                    list.Add(new NodeInfoSet
                    {
                        ID = id,
                        IsPrimary = true,
                        Votes = (long)vts,
                        Status = nodeStatus[id]
                    });
                }
            }

            var list2 = new List<NodeInfoSet>();
            var nonPrimaryNodes = nodeStatus.Where(a => !bb.PrimaryAuthorizers.Contains(a.Key));
            foreach (var node in nonPrimaryNodes)
            {
                var x = bb.ActiveNodes.FirstOrDefault(a => a.AccountID == node.Key);
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
                .Where(a => bb.ActiveNodes.Any(b => b.AccountID == a.ID))
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

                var hostlist = result.Select(a => bb.NodeAddresses[a.ID]).ToList();
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
                
                if(uri.HostNameType == UriHostNameType.Dns)
                    return Dns.GetHostEntry(uri.Host).AddressList[0].ToString();

                return hostName;
            }
            catch
            {
                return hostName;
            }
        }
	}

	public class NodeInfoSet
    {
		public int Index { get; set; }
		public string ID { get; set; }
		public bool IsPrimary { get; set; }
		public long Votes { get; set; }
		public GetSyncStateAPIResult Status { get; set; }
		public string Country { get; set; }
	}
}
