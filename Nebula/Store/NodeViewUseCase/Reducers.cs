using Converto;
using Fluxor;
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
				NodeStatus: action.historyState?.nodeStatus);

		[ReducerMethod]
		public static NodeViewState ReduceFetchDataResultAction(NodeViewState state, NodeViewResultAction action)
        {
			var nvs = new NodeViewState(
					isLoading: false,
					billBoard: action.billBoardResult,
					NodeStatus: action.nodeStatusResult);

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
					NodeStatus: action.historyState.nodeStatus);

			nvs.Id = action.historyState.Id;
			nvs.TimeStamp = action.historyState.TimeStamp;
			return nvs;
		}

		[ReducerMethod]
		public static NodeViewState ReducePftInfoResultAction(NodeViewState state, PftResultAction action)
		{
			var state2 = state.With(new
			{
				UI = NodeViewState.UIStage.Profiting,
				pft = action.pft,
				stks = action.stks,
				pftStats = action.stats,
				stkRewards = action.rewards
			});
			return state2;
		}

		[ReducerMethod]
		public static NodeViewState ReduceReturnAction(NodeViewState state, ReturnToMainAction action)
		{
			var state2 = state.With(new
			{
				UI = NodeViewState.UIStage.Main,
			});
			return state2;
		}
	}
}
