﻿using Fluxor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Store.NodeViewUseCase
{
	public static class Reducers
	{
		[ReducerMethod]
		public static NodeViewState ReduceFetchDataAction(NodeViewState state, NodeViewAction action) =>
			new NodeViewState(
				isLoading: true,
				billBoard: action.historyState?.bb,
				NodeStatus: action.historyState?.nodeStatus,
				ipdb: action.historyState?.ipDbFn);

		[ReducerMethod]
		public static NodeViewState ReduceFetchDataResultAction(NodeViewState state, NodeViewResultAction action)
        {
			var nvs = new NodeViewState(
					isLoading: false,
					billBoard: action.billBoardResult,
					NodeStatus: action.nodeStatusResult,
					ipdb: action.ipDbFn);

			nvs.Id = 0;     // create new for liteDB
			nvs.TimeStamp = DateTime.UtcNow;
			return nvs;
		}

		[ReducerMethod]
		public static NodeViewState ReduceLoadHistoryResultAction(NodeViewState state, LoadHistoryAction action)
		{
			var nvs = new NodeViewState(
					isLoading: false,
					billBoard: action.historyState.bb,
					NodeStatus: action.historyState.nodeStatus,
					ipdb: action.historyState.ipDbFn);

			nvs.Id = action.historyState.Id;
			nvs.TimeStamp = action.historyState.TimeStamp;
			return nvs;
		}
	}
}
