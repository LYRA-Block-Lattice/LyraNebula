using Lyra.Core.Accounts;
using Lyra.Core.Blocks;
using Lyra.Data.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nebula.Store.StatsUseCase
{
	public class StatsState
	{
		public bool IsLoading { get; }
		public List<TransStats> transStats { get; }

		public StatsState(bool isLoading, List<TransStats> transStats)
		{
			IsLoading = isLoading;
			this.transStats = transStats;
		}

		private IEnumerable<TransStats> AllSend => transStats.Where(a => a.trans == BlockTypes.SendTransfer);
		private IEnumerable<TransStats> AllRecv => transStats.Where(a => a.trans == BlockTypes.ReceiveTransfer);

		public TransStats Fastest => transStats.OrderBy(a => a.ms).FirstOrDefault();
		public TransStats Slowest => transStats.OrderBy(a => a.ms).LastOrDefault();
		public TransStats FastestSend => AllSend.OrderBy(b => b.ms).FirstOrDefault();
		public TransStats SlowestSend => AllSend.OrderBy(b => b.ms).LastOrDefault();
		public TransStats FastestReceive => AllRecv.OrderBy(b => b.ms).FirstOrDefault();
		public TransStats SlowestReceive => AllRecv.OrderBy(b => b.ms).LastOrDefault();
		public double AvgTime => Math.Round(transStats.Average(a => a.ms), 2);
		public double AvgSendTime => Math.Round(AllSend.Any() ? AllSend.Average(b => b.ms) : 0, 2);
		public double AvgRecvTime => Math.Round(AllRecv.Any() ? AllRecv.Average(b => b.ms) : 0, 2);
	}
}
