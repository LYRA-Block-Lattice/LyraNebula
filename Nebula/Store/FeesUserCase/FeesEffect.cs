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

namespace Nebula.Store.FeesUserCase
{
	public class FeesEffect : Effect<FeesAction>
	{
		private readonly ILyraAPI client;

		public FeesEffect(ILyraAPI lyraClient)
		{
			client = lyraClient;
		}

		public override async Task HandleAsync(FeesAction action, IDispatcher dispatcher)
		{
			var stats = await client.GetFeeStatsAsync();// .GetFeeStatsAsync();
			var sbResult = await client.GetLastServiceBlockAsync();
			var sb = sbResult.GetBlock() as ServiceBlock;
			var voters = client.GetVoters(new VoteQueryModel
			{
				posAccountIds = sb.Authorizers.Keys.ToList(),
				endTime = sb.TimeStamp
			});
			
			dispatcher.Dispatch(new FeesResultAction(stats, sb, voters));
		}
	}
}
