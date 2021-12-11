using Converto;
using Fluxor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Store.WebWalletUseCase
{
	public static class Reducers
	{
		[ReducerMethod]
		public static WebWalletState CreateWalletActionHandler(WebWalletState state, WebWalletCreateAction action) => state.With(new { IsLoading = true });

		[ReducerMethod]
		public static WebWalletState RestoreWalletActionHandler(WebWalletState state, WebWalletRestoreAction action) => state.With(new { IsLoading = true });

		[ReducerMethod]
		public static WebWalletState RefreshBalanceActionHandler(WebWalletState state, WebWalletRefreshBalanceAction action) => state.With(new { IsLoading = true });

        [ReducerMethod]
		public static WebWalletState CloseAction(WebWalletState state, WebWalletCloseAction action) => new WebWalletState();

		[ReducerMethod]
		public static WebWalletState SendAction(WebWalletState state, WebWalletSendAction action) => state.With(new { stage = UIStage.Send });

        [ReducerMethod]
        public static WebWalletState StakingAction(WebWalletState state, StakingResultAction action)
        {
			return state.With(new
			{
				IsLoading = false,
				stage = UIStage.Staking,
				brokerAccounts = action.brokers,
				stkBalances = action.balances,
				stkRewards = action.rewards
			});
		}

		[ReducerMethod]
		public static WebWalletState ErrorAction(WebWalletState state, WalletErrorResultAction action)
		{
			return state.With(new
			{
				IsLoading = false,
				error = action.error
			});
		}

		[ReducerMethod]
		public static WebWalletState ErrorResetAction(WebWalletState state, WalletErrorResetAction action)
		{
			return state.With(new
			{
				IsLoading = false,
				error = ""
			});
		}

		[ReducerMethod]
		public static WebWalletState SendTokenActionHandler(WebWalletState state, WebWalletSendTokenAction action) => state.With(new { IsLoading = true });

		[ReducerMethod]
		public static WebWalletState CancelSendAction(WebWalletState state, WebWalletCancelSendAction action) => state.With(new { stage = UIStage.Main });

		[ReducerMethod]
		public static WebWalletState RefreshBalanceAction(WebWalletState state, WebWalletResultAction action)
        {
			var bs = "<empty>";
			if (action.wallet == null)
            {

			}
			else
            {
				var bst = action.wallet.GetDisplayBalancesAsync().ContinueWith(a => bs = a.Result);
				bst.Wait();
			}

			return state.With(new
			{
				IsLoading = false,
				stage = action.stage,
				IsOpening = action.IsOpening,
				wallet = action.wallet,
				balanceString = bs,
				VoteFor = action.wallet?.VoteFor,
				error = ""
			});
		}			

		[ReducerMethod]
		public static WebWalletState ReduceOpenSettingsAction(WebWalletState state, WebWalletSettingsAction action) => state.With(new { stage = UIStage.Settings });

		[ReducerMethod]
		public static WebWalletState ReduceSaveSettingsAction(WebWalletState state, WebWalletSaveSettingsAction action)
		{
            var state2 = state.With(new
            {
                stage = UIStage.Main,
            });
			state2.wallet.SetVoteFor(action.VoteFor);
			return state2;
        }

		[ReducerMethod]
		public static WebWalletState ReduceCancelSaveSettingsAction(WebWalletState state, WebWalletCancelSaveSettingsAction action) => state.With(new { stage = UIStage.Main });

		[ReducerMethod]
		public static WebWalletState GetTransactionActionHandler(WebWalletState state, WebWalletTransactionsAction action) => state.With(new { IsLoading = true });

		[ReducerMethod]
		public static WebWalletState ReduceTransactionsAction(WebWalletState state, WebWalletTransactionsResultAction action) =>
			state.With(new {
				IsLoading = false,
				stage = UIStage.Transactions,
				txs = action.transactions
			});

		[ReducerMethod]
		public static WebWalletState ReduceFreeTokenAction(WebWalletState state, WebWalletFreeTokenResultAction action) =>
			state.With(new {
                stage = UIStage.FreeToken,
                faucetBalance = action.faucetBalance,
                ValidReCAPTCHA = true,
                ServerVerificatiing = false,				
            });

		[ReducerMethod]
		public static WebWalletState ReduceSendMeFreeTokenAction(WebWalletState state, WebWalletSendMeFreeTokenResultAction action)
        {
			var stt = state.With(new { 
				stage = UIStage.Main,
				LastOperationIsSuccess = action.Success
			});

			if (action.Success)
			{
				stt.freeTokenSent = true;
				stt.freeTokenTimes++;
			}
			return stt;
		}

		[ReducerMethod]
		public static WebWalletState ReduceLyraSwapAction(WebWalletState state, WebWalletSwapTokenAction action) => state.With(new 
		{ 
			stage = UIStage.SwapToken,
			Message = ""
		});

		[ReducerMethod]
		public static WebWalletState ReductLyraSwapHandler(WebWalletState state, WebWalletBeginTokenSwapAction action) => state.With(new { IsLoading = true });

		[ReducerMethod]
		public static WebWalletState ReduceLyraSwapResultAction(WebWalletState state, WebWalletTokenSwapResultAction action)
		{
			var stt = state.With(new
			{
				IsLoading = false,
				LastOperationIsSuccess = action.Success,
				Message = action.Success ? "Swapping is succeed!" : "Swapping is failed."
			});

			return stt;
		}

		[ReducerMethod]
		public static WebWalletState ReduceSwapTLYRAction(WebWalletState state, WebWalletSwapTLYRAction action) => state.With(new { stage = UIStage.SwapTLYR, Message = "" });

		[ReducerMethod]
		public static WebWalletState ReductSwapTLYRHandler(WebWalletState state, WebWalletBeginSwapTLYRAction action) => state.With(new { IsLoading = true });

		[ReducerMethod]
		public static WebWalletState ReduceSwapTLYRResultAction(WebWalletState state, WebWalletSwapTLYRResultAction action)
		{
			var stt = state.With(new
			{
				IsLoading = false,
				LastOperationIsSuccess = action.Success,
				Message = action.errMessage
			});

			return stt;
		}

		[ReducerMethod]
		public static WebWalletState ReduceRCValidAction(WebWalletState state, WebWalletReCAPTCHAValidAction action) => state.With(new { ValidReCAPTCHA = action.ValidReCAPTCHA });

		[ReducerMethod]
		public static WebWalletState ReduceRCServerAction(WebWalletState state, WebWalletReCAPTCHAServerAction action) => state.With(new { ServerVerificatiing = action.ServerVerificatiing });
	
		// DEX
		[ReducerMethod]
		public static WebWalletState ReduceStartDEXAction(WebWalletState state, WebWalletStartDexAction action) => state.With(new { stage = UIStage.DEX, Message = "" });
	}
}
