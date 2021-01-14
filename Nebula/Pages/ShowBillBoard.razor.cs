using Fluxor;
using Lyra.Data.API;
using Microsoft.AspNetCore.Components;
using Nebula.Store.NodeViewUseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nebula.Pages
{
	public partial class ShowBillBoard
	{
		[Inject]
		private IState<NodeViewState> NodeState { get; set; }

		[Inject]
		private IDispatcher Dispatcher { get; set; }

		private Dictionary<string, string> seedHosts;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			Dispatcher.Dispatch(new NodeViewAction());

			seedHosts = new Dictionary<string, string>();
			_ = Task.Run(() => {
				var seeds = LyraAggregatedClient.GetSeedNodes(Configuration["network"]);
				foreach(var seed in seeds)
                {
					try
					{
						var ip = Dns.GetHostEntry(seed);
						seedHosts.Add($"{ip.AddressList[0]}", seed);
					}
					catch { }
                }					
			});
		}
	}
}
