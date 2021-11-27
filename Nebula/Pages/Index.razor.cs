using Fluxor;
using LiteDB;
using Lyra.Core.API;
using Microsoft.AspNetCore.Components;
using Nebula.Data;
using Nebula.Data.Lyra;
using Nebula.Store.BlockSearchUseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Nebula.Data.Lyra.Supply;

namespace Nebula.Pages
{
    public partial class Index
    {
        [Inject]
        public NavigationManager navigationManager { get; set; }

        [Inject]
        private ILiteDbContext dbCtx { get; set; }

        public Supply CurrentSupply { get; private set; }

        string hash;

        public void oninput()
        {
            if(hash != null)
                navigationManager.NavigateTo($"/showblock/{hash}");
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            CurrentSupply = new Supply(dbCtx);
        }


    }
}

