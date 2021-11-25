using Fluxor;
using Microsoft.AspNetCore.Components;
using Nebula.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Lyra.Core.API;
using Lyra.Core.Blocks;
using Lyra.Data.API;

namespace Nebula.Store.StatsUseCase
{
	public class StatsEffect : Effect<StatsAction>
	{
		private readonly ILyraAPI client;

		public StatsEffect(ILyraAPI lyraClient)
		{
			client = lyraClient;
		}

		public override async Task HandleAsync(StatsAction action, IDispatcher dispatcher)
		{
			var stats = await client.GetTransStatsAsync();
			
			dispatcher.Dispatch(new StatsResultAction(stats));
		}
	}
}
