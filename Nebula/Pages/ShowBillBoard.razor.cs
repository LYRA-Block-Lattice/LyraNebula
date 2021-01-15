using Fluxor;
using Lyra.Data.API;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Nebula.Data;
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

		[Inject]
		private INodeHistory History { get; set; }

		private Dictionary<string, string> seedHosts;

		protected override void OnInitialized()
		{
			base.OnInitialized();

			// load history first
			var latest = History.FindLatest(Configuration["network"]);
			if(latest == null)
				Dispatcher.Dispatch(new NodeViewAction());
			else
            {
				Dispatcher.Dispatch(new LoadHistoryAction { historyState = latest });
				StateHasChanged();
			}				

			NodeState.StateChanged += NodeState_StateChanged;			

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

		private void Refresh(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new NodeViewAction());
		}

		private void NodeState_StateChanged(object sender, NodeViewState e)
        {
			if(e.Id == 0)
				History.Insert(e);
        }
    }
}
