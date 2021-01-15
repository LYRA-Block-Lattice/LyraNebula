using Lyra.Core.API;
using Lyra.Data.API;
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
		public string network { get; }

		public NodeViewResultAction(string networkId, BillBoard billBoard, ConcurrentDictionary<string, GetSyncStateAPIResult> NodeStatusResult, string ipdb)
		{
			network = networkId;
			billBoardResult = billBoard;
			nodeStatusResult = NodeStatusResult;
			ipDbFn = ipdb;
		}
	}
}
