using LiteDB;
using Lyra;
using Lyra.Core.API;
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
        protected override void OnInitialized()
        {
            base.OnInitialized();

            LoadRichData();
        }

        private void LoadRichData()
        {
			var dbfn = "LiteDb/RichList.db";
			using (var db = new LiteDatabase(dbfn))
			{
				if(db.CollectionExists("Meta"))
                {
					var coll = db.GetCollection<SnapInfo>("Meta");
					Snap = coll.FindAll().FirstOrDefault();
				}

				if (db.CollectionExists("Asserts"))
				{
					var coll = db.GetCollection<Assert>("Asserts");
					LyraAsserts = coll.FindAll().ToList();
				}
			}
		}
    }
}
