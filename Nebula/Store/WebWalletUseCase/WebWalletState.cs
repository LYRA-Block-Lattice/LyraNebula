using Lyra.Core.Accounts;
using Lyra.Core.Blocks;
using Nebula.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Store.WebWalletUseCase
{
	public enum UIStage { Entry, Main, Send, Settings, Transactions, FreeToken, SwapToken, SwapTLYR, Staking };

	public class WebWalletState
	{
		private const string faucetKey = "freeLyraToken";
		// for faucet
		public int? freeTokenTimes { get; set; }

		// staking
		public List<Block> brokerAccounts { get; set; }
		public Dictionary<string, decimal> stkBalances { get; set; }
		public Dictionary<string, decimal> stkRewards { get; set; }

		public bool IsLoading { get; set; }
		public UIStage stage { get; set; } = UIStage.Entry;
		public bool IsOpening { get; set; } = false;
		public Wallet wallet { get; set; } = null;
		public string balanceString { get; set; } = "<empty>";
		public List<string> txs { get; set; } = null;
		public decimal faucetBalance { get; set; } = 0m;
		public bool freeTokenSent { get; set; } = false;

		public bool ValidReCAPTCHA { get; set; } = false;

		public bool ServerVerificatiing { get; set; } = false;

		public bool DisablePostButton => !ValidReCAPTCHA || ServerVerificatiing;

		public bool LastOperationIsSuccess { get; set; }
		public string Message { get; set; }
	}
}
