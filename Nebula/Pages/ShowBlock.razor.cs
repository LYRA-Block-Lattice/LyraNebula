﻿using Fluxor;
using Microsoft.AspNetCore.Components;
using Nebula.Store.BlockSearchUseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Pages
{
	public partial class ShowBlock
	{
		[Inject]
		public NavigationManager navigationManager { get; set; }

		[Parameter] 
		public string hash { get; set; }
		[Parameter]
		public long height { get; set; }

		[Inject]
		private IState<BlockSearchState> searchState { get; set; }

		[Inject]
		private IDispatcher Dispatcher { get; set; }

		string search;
		private int dot { get; set; }
		protected override void OnInitialized()
		{
			base.OnInitialized();
			Dispatcher.Dispatch(new BlockSearchAction(hash, height));
		}

        protected override async Task OnParametersSetAsync()
        {
			Dispatcher.Dispatch(new BlockSearchAction(hash, height));

			await base.OnParametersSetAsync();
        }

		public void oninput(ChangeEventArgs args)
		{
			navigationManager.NavigateTo($"/showblock/{args.Value}");
		}
	}
}
