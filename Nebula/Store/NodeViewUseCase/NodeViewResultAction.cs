using Lyra.Core.API;
using Lyra.Core.Blocks;
using Lyra.Data.API;
using Lyra.Data.Blocks;
using Nebula.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Store.NodeViewUseCase
{
	public class NodeViewResultAction
	{
		public BillBoard billBoardResult { get; }
		public ConcurrentDictionary<string, GetSyncStateAPIResult> nodeStatusResult { get; }
		public string ipDbFn { get; }

		public NodeViewResultAction(BillBoard billBoard, ConcurrentDictionary<string, GetSyncStateAPIResult> NodeStatusResult, string ipdb)
		{
			billBoardResult = billBoard;
			nodeStatusResult = NodeStatusResult;
			ipDbFn = ipdb;
		}
	}

	public class PftResultAction
    {
		public IProfiting pft { get; set; }
		public List<Staker> stks { get; set; }
		public ProfitingStats stats { get; set; }
		public Dictionary<string, decimal> rewards { get; set; }
	}
}
