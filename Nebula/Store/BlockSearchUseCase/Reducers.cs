﻿using Fluxor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Store.BlockSearchUseCase
{
	public static class Reducers
	{
		[ReducerMethod]
		public static BlockSearchState ReduceFetchDataAction(BlockSearchState state, BlockSearchAction action) =>
			new BlockSearchState(
				isLoading: true,
				blockResult: null,
				previousBlock: null,
				pageKey: null,
				maxHeight: 0,
				errmsg: null);

		[ReducerMethod]
		public static BlockSearchState ReduceFetchDataResultAction(BlockSearchState state, BlockSearchResultAction action) =>
			new BlockSearchState(
				isLoading: false,
				blockResult: action.block,
				previousBlock: action.prevBlock,
				pageKey: action.key,
				maxHeight: action.maxHeight,
				errmsg: action.error);

    }
}
