using Fluxor;
using LiteDB;
using Lyra.Core.API;
using Microsoft.AspNetCore.Components;
using Nebula.Data;
using Nebula.Data.Lyra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Pages
{
    public partial class Asserts
    {
		private SnapInfo Snap { get; set; }
		private List<Assert> LyraAsserts { get; set; }

		[Inject]
		private ILiteDbContext dbCtx { get; set; }

        [Inject]
        public NavigationManager navigationManager { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            LoadRichData();
        }

        private void LoadRichData()
        {
			var db = dbCtx.Database;

            if (db.CollectionExists("Meta"))
            {
                var coll = db.GetCollection<SnapInfo>("Meta");
                Snap = coll.FindAll().FirstOrDefault();
            }

            if (db.CollectionExists("Asserts"))
            {
                var coll = db.GetCollection<Assert>("Asserts");
                LyraAsserts = coll.FindAll().Skip(1).ToList();  // don't display LYR
            }
        }

        public void SwapToken(string token)
        {
            //walletState.Value.stage = UIStage.SwapToken;
            //navigationManager.NavigateTo("/swap/" + token.Replace("/", "%2F"));
        }
    }
}
