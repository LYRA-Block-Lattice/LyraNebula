using Lyra.Core.Accounts;
using Lyra.Core.Blocks;
using System.Collections.Generic;

namespace Nebula.Store.WebWalletUseCase
{
	public class WalletCreateResultAction
    {

    }
	public class WebWalletResultAction
	{
		public bool IsOpening { get; }
		public Wallet wallet { get; }
		public UIStage stage { get; }

		public WebWalletResultAction(Wallet wallet, bool isOpening, UIStage Stage)
		{
			this.IsOpening = isOpening;
			this.wallet = wallet;
			this.stage = Stage;
		}
	}

	public class StakingResultAction
    {
		public List<Block> brokers { get; set; }
		public Dictionary<string, decimal> balances { get; set; }
		public Dictionary<string, decimal> rewards { get; set; }
	}

	public class WalletErrorResultAction
    {
		public string error { get; set; }
    }

	public class WalletErrorResetAction { }
}
