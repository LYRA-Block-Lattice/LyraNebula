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

		private int selId;
        private int SelectedHistId
        {
            get
            {
                return selId;
            }
            set
            {
                selId = value;
				var recd = History.FindOne(selId);
				Dispatcher.Dispatch(new LoadHistoryAction { historyState = recd });
				StateHasChanged();
			}
        }
        private IEnumerable<HistInfo> BriefHist { get; set; }

		protected override void OnInitialized()
		{
			BriefHist = Enumerable.Empty<HistInfo>();
			base.OnInitialized();

			// load history first
			var latest = History.FindLatest();
			if(latest == null)
				Dispatcher.Dispatch(new NodeViewAction());
			else
            {
				BriefHist = History.FindHistory(100);
				if (BriefHist.Any())
					selId = BriefHist.First().id;

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
			if(e.Id == 0 && e.TimeStamp != default(DateTime))
            {
				History.Insert(e);

				BriefHist = History.FindHistory(100);
				if (BriefHist.Any())
					selId = BriefHist.First().id;

				StateHasChanged();
			}				
        }
    }

}
