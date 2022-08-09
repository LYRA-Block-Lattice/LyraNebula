using Fluxor;
using Lyra.Core.API;
using Lyra.Core.Blocks;
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
		protected bool IsDisabled { get; set; }

		[Inject]
		private IState<NodeViewState> NodeState { get; set; }

		[Inject]
		private IDispatcher Dispatcher { get; set; }

		[Inject]
		private INodeHistory History { get; set; }

		private Dictionary<string, string> seedHosts;

		private int selId;
		List<Profiting> pfts;

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
			try
            {
				IsDisabled = true;

				BriefHist = Enumerable.Empty<HistInfo>();
				base.OnInitialized();

				// load history first
				var latest = History.FindLatest();
				if (latest == null)
					Dispatcher.Dispatch(new NodeViewAction());
				else
				{
					BriefHist = History.FindHistory(100);
					if (BriefHist.Any())
						selId = BriefHist.First().id;

					Dispatcher.Dispatch(new LoadHistoryAction { historyState = latest });
					StateHasChanged();
				}

                NodeState.StateChanged += NodeState_StateChanged; ;

				seedHosts = new Dictionary<string, string>();
				_ = Task.Run(async () => {
					int port = 4504;
					if (Configuration["network"].Equals("mainnet", StringComparison.InvariantCultureIgnoreCase))
						port = 5504;

					var lcx = LyraRestClient.Create(Configuration["network"], Environment.OSVersion.ToString(), "Nebula", "1.4");
					pfts = await lcx.FindAllProfitingAccountsAsync(DateTime.MinValue, DateTime.MaxValue);

					var aggClient = new LyraAggregatedClient(Configuration["network"], true, null);
					var seeds = aggClient.GetSeedNodes();
					foreach (var seed in seeds)
					{
						try
						{
							var ip = Dns.GetHostEntry(seed);
							seedHosts.Add($"{ip.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)}:{port}", seed);
						}
						catch { }
					}
				});
			}
            catch { }
		}

        private void Refresh(MouseEventArgs e)
		{
			IsDisabled = true;
			StateHasChanged();

			var latest = History.FindLatest();
			if (latest == null)
				Dispatcher.Dispatch(new NodeViewAction());
			else
				Dispatcher.Dispatch(new NodeViewAction { historyState = latest });
		}

		private void NodeState_StateChanged(object sender, EventArgs ex)
        {
			var e = NodeState.Value;
			if(e.Id == 0 && e.TimeStamp != default(DateTime))
            {
				try
                {
					History.Insert(e);

					BriefHist = History.FindHistory(100);
					if (BriefHist.Any())
						selId = BriefHist.First().id;

					IsDisabled = false;
					StateHasChanged();
				}
                catch { }
			}				
        }

        protected override void OnAfterRender(bool firstRender)
        {
			if(firstRender)
            {
				IsDisabled = false;
				StateHasChanged();
			}
		}

		private string GetProfitingAccount(string posAccount)
        {
			var target = NodeState.Value.bb?.ActiveNodes.FirstOrDefault(a => a.AccountID == posAccount);
			if (target == null)
				return "";
			else
				return target.ProfitingAccountId;
		}
		private string GetProfitingAccountShort(string posAccount)
		{
			var acct = GetProfitingAccount(posAccount);
			if (acct?.Length > 10)
				return acct.Substring(0, 10);
			else
				return acct;
		}

		private string GetProfitingAccountName(string posAccount)
		{
			if (pfts == null)
				return "";

			var acct = pfts.FirstOrDefault(a => a.gens.OwnerAccountId == posAccount);
			if (acct == null)
				return "";

			return acct.gens.Name;
		}

		private decimal GetStakingAmount(string posAccount)
		{
			var target = NodeState.Value.bb.ActiveNodes.FirstOrDefault(a => a.AccountID == posAccount);
			if (target == null)
				return 0;
			else
				return target.Votes;
		}

		private void ShowPft(MouseEventArgs e, string owner)
		{
			Dispatcher.Dispatch(new GetProfitAction { owner = owner });
		}

		private string GetPftID()
        {
			if (NodeState.Value.pft == null)
				return "";

			return ((TransactionBlock)NodeState.Value.pft).AccountID;
        }

		private void Return(MouseEventArgs e)
		{
			Dispatcher.Dispatch(new ReturnToMainAction { });
		}

		private decimal GetToOwner()
        {
			return Math.Round(NodeState.Value.pftStats.Total * (1 - NodeState.Value.pft.ShareRito), 8);
        }
		private decimal GetToStakers()
		{
			return Math.Round(NodeState.Value.pftStats.Total * NodeState.Value.pft.ShareRito, 8);
		}

		private string FormatShare()
        {
			return $"{NodeState.Value.pft.ShareRito * 100} %";
        }
	}

}
