using Fluxor;
using Lyra.Core.API;
using Lyra.Core.Blocks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Nebula.Store.StatsUseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Pages
{
    public partial class ProcessQuery
    {
		[Inject]
		IConfiguration Configuration { get; set; }
		string hash;
		IEnumerable<TransactionBlock> transactions;
		protected override void OnInitialized()
		{
			base.OnInitialized();
		}

		async void OnChange(string value)
		{
			if (value!.Length == 44)
            {
				var lc = LyraRestClient.Create(Configuration["network"], Environment.OSVersion.ToString(), "Nebula", "1.4");
				var ret = await lc.GetBlocksByRelatedTxAsync(value);
				if(ret.Successful())
                {
					transactions = ret.GetBlocks().Cast<TransactionBlock>();
					StateHasChanged();
					return;
                }
			}
			transactions = null;

		}
	}
}
