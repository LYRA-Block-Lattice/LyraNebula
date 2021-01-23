using Fluxor;
using LiteDB;
using Lyra;
using Microsoft.AspNetCore.Components;
using Nebula.Store.BlockSearchUseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Pages
{
	public partial class Index
	{
		[Inject]
		public NavigationManager navigationManager { get; set; }

		private SnapInfo Snap { get; set; }
		public TotalBalance Total { get; private set; }
		public List<RichItem> RichList { get; private set; }

		private decimal TeamTotal { get; set; }
		private decimal TeamRito { get; set; }
		private decimal Circulate { get; set; }
		private decimal CirculateRito { get; set; }

		public void oninput(ChangeEventArgs args)
		{
			navigationManager.NavigateTo($"/showblock/{args.Value}");
		}

        protected override void OnInitialized()
        {
			base.OnInitialized();

			LoadRichData();
		}

		public class RichItem
        {
			public string AccountId { get; set; }
			public DualBalance Balance { get; set; }
			public int Order { get; set; }
			public string Tag { get; set; }
			public decimal Sum { get; set; }
			public decimal SumRito { get; set; }
		}

        private void LoadRichData()
        {
			var dbfn = "LiteDb/RichList.db";
			var teamAddresses = new[]{
				"L5ViiZbSmLJJpXppwBCNPuCzRds2VMkydvfcENp3SxqAfLNuqk5JuuDrshmJNCjTo6oKgXRagCnTrVXseyxn2q74vXmYcG",
				"LUVD9A8vnS3Ni1WGKAe2gBcY3w2fc2EEmaqNggXm5Fm6t9yqggnUrk7rE9v3xtMj51KM2aM1APUGbXrjuhT9hXsgpkYe2a",
				"LVzZptfCgvsredZ4NFe1GXFQz6BsWZS82izypgMCdTU3EAai41g11NFGih8SjH6PwRNPsovmLmoZikcEZHLPTvaLtYb73f",
				"L4Qmz1KaG7U5vnqeMdBjEQCgL57PsU9UNuh5ZbrXKMNDCoMUBnR3Eui4M2owPnyNknCh3cRTR2jiUh95ecKdJN16ryPGbb",
				"LWPwRHXQ4PsghJdtWykmhtJG1LrP56Qvfmt4FkzghDjEHaN7wk7UCtwy6iWbRUUoHopMPov8XgEJR1ie52LGWD1dZ7srWm",
				"LPYeZRnYj3Jc5mdZwioo4m6ut7CupR6c1mfewBZADsUasgo4gFQWLjhCAZk7pDZuzaRMfXbeuwMds9YNurx4q2gBaz4LBh",
				"LKDkDrKYfNXj7FhbjKU4QuyQHhrW8vv3EYuwEwzDZUot8tVKxCTTDuMr9tsiYuS1qXu93N9tQw4WFDsjbkFzJsmVW1GbPo",
				"L6Wvo9RRZFsmfy7r5dr14BZJZtmx3oHdZEwsViudGrXyaBiGesoFnHqzLxhrMnx35YRP24iBbZyZNtEAZrnKciGNdKuPF7",
				"LLRmYsbr8ZL6YvPSCVCHxKcPhM4Ex1fYu2dzNz4y7ZoPQ6vxb7Khyi6gXp4aDVYm1hiLsaLYXHNFp82bSsPLEAhfDnnemt",
				"LrCupHHBaHFV51MSQYub17M5w7TnoYmvuQdwBKtPGy7r1KQdXiRP59NEJfwYnWpsiMj3YUcVDqKBNprpZ89TW6EwHhj4Y",
				"L5NouvmuTPx6hq1661UBP2tPrJB3Tnxm827k3yf8PjN6JpEMeCPTvf3iYjVjWJEiy6i72GYHKqucsdXZ9MFJ8FSJDgbLXR",
				"LL3QE9aiyGNS34PEELxiVf7KoxWNAWCsih9ZDh3svwYqR27NbqnBcaQuHzQFrHgPjiHfvji9ADpjdPicpWKLB436w6LZ7G",
				"LNLunuxUt9o68RWC9CtJh2z1o1BwBxYnn4JTBF4TVfDVa6bi2dXZAVUYEVsxav6LihzznV5Rz8Kq67bo3BcwKke5q3ETMv",
			};
			using (var db = new LiteDatabase(dbfn))
            {
				if (db.CollectionExists("Meta"))
				{
					var coll = db.GetCollection<SnapInfo>("Meta");
					Snap = coll.FindAll().FirstOrDefault();
				}

				if (db.CollectionExists("TotalBalance"))
				{
					var coll = db.GetCollection<TotalBalance>("TotalBalance");
					Total = coll.FindAll().FirstOrDefault();

					RichList = new List<RichItem>();
					int order = 0;
					decimal sum = 0;
					decimal subRito = 0;
					foreach (var e in Total.AllAccounts.Take(100))
                    {
						sum += e.Value.Total;
						subRito = Math.Round(sum / 100000000, 4);
						order++;
						RichList.Add(new RichItem
						{
							AccountId = e.Key,
							Balance = e.Value,
							Order = order,
							Sum = sum,
							SumRito = subRito,
							Tag = teamAddresses.Contains(e.Key) ? "Lyra Team" : ""
						});
                    }
				}
			}

			TeamTotal = Total.AllAccounts
				.Where(x => teamAddresses.Contains(x.Key))
				.Sum(x => x.Value.Total);

			TeamRito = Math.Round(TeamTotal / 10000000000 * 100, 4);

			Circulate = 10000000000 - TeamTotal;
			CirculateRito = 100 - TeamRito;
		}
	}
}

