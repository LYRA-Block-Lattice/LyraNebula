using Fluxor;
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

        public Supply CurrentSupply { get; private set; }

        string hash;

        public void oninput(ChangeEventArgs args)
        {
            navigationManager.NavigateTo($"/showblock/{args.Value}");
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            //CurrentSupply = new Supply(dbCtx);
        }


    }
}

