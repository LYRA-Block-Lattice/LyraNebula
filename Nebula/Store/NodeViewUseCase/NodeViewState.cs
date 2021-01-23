using Converto;
using IP2Country;
using IP2Country.MarkusGo;
using Lyra.Core.API;
using Lyra.Data.API;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Nebula.Store.NodeViewUseCase
{
	public class NodeViewState
	{
        public bool IsLoading { get; set; }
		public BillBoard bb { get; set; }
		public ConcurrentDictionary<string, GetSyncStateAPIResult> nodeStatus { get; set; }
		public string ipDbFn { get; set; }

        public int Id { get; set; }         // used by litedb
        public DateTime TimeStamp { get; set; }     // history

		public NodeViewState()
        {
			IsLoading = false;
        }
		public NodeViewState(bool isLoading, BillBoard billBoard, ConcurrentDictionary<string, GetSyncStateAPIResult> NodeStatus, string ipdb)
		{
			IsLoading = isLoading;
			bb = billBoard;
			nodeStatus = NodeStatus;
			ipDbFn = ipdb;
		}

		public List<NodeInfoSet> GetRankedList()
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
                var resolver = new IP2CountryBatchResolver(new IP2CountryResolver(
                    new MarkusGoCSVFileSource(ipDbFn)
                ));

                var iplist = result.Select(a => bb.NodeAddresses[a.ID]);
                var geoList = resolver.Resolve(iplist);
                for (int i = 0; i < result.Count; i++)
                {
                    result[i].Country = geoList[i] == null ? "" : geoList[i].Country;
                }
            }
            catch (Exception)
            { }

            return result;
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
