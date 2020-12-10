﻿using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GalaxyBudsClient.Interface.Elements;
using GalaxyBudsClient.Interface.Pages;
using Serilog;

namespace GalaxyBudsClient.Interface.Transition
{
 	public class PageContainer : UserControl
	{
		public Carousel Pager { get; }

		private object? _lastPageCache = null;
		
		public ViewModel PageViewModel = new ViewModel();
		public PageContainer()
		{   
			InitializeComponent();
			
			DataContext = PageViewModel;
			
			Pager = this.FindControl<Carousel>("Pager");
			var fadeTransition = new FadeTransition();
			fadeTransition.FadeOutComplete += async (sender, args) =>
			{		
				if (_lastPageCache is AbstractPage page)
				{
					page.OnPageHidden();
				}
			};
			Pager.PageTransition = fadeTransition;

			// Add placeholder page
			RegisterPages(new DummyPage(), new DummyPage2());
			SwitchPage(AbstractPage.Pages.Dummy);
		}

		public AbstractPage.Pages CurrentPage
		{
			get
			{
				if (Pager.SelectedItem is AbstractPage page)
				{
					return page.PageType;
				}

				return AbstractPage.Pages.Undefined;
			}
		}

		public void RegisterPage(AbstractPage page)
		{
			if (PageViewModel.HasPageType(page.PageType))
			{
				Log.Warning($"Page type '${page.PageType}' is already assigned. Disposing old page.");
				UnregisterPage(page);
			}
			PageViewModel.Items.Add(page);
		}

		public void RegisterPages(params AbstractPage[] pages)
		{
			foreach (var page in pages)
			{
				RegisterPage(page);	
			}
		}
        
		public bool UnregisterPage(AbstractPage page)
		{
			if (Equals(Pager.SelectedItem, page))
			{
				Log.Warning($"Page '${page.PageType}' to be unregistered is currently loaded");
			}

			return PageViewModel.Items.Remove(page);
		}

		public bool SwitchPage(AbstractPage.Pages page)
		{
			var target = PageViewModel.FindPage(page);

			if (target != null)
			{
				_lastPageCache = Pager.SelectedItem;
				Pager.SelectedItem = target;
				/* Call OnPageShown prematurely */
				target.OnPageShown();
			}

			return target != null;
		}
		
		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public class ViewModel
		{
			public ViewModel()
			{
				Items = new ObservableCollection<AbstractPage>();
			}

			public ObservableCollection<AbstractPage> Items { get; }
			
			public AbstractPage? FindPage(AbstractPage.Pages page, bool nullAware = false)
			{
				AbstractPage[] matches = Items.Where(abstractPage => abstractPage.PageType == page).ToArray();
				if (matches.Length < 1)
				{
					if (!nullAware)
					{
						Log.Error($"Page '{page}' is not assigned");
					}

					return null;
				} 
				if (matches.Length > 1)
				{
					Log.Warning($"Page '{page}' has multiple assignments. Choosing first one.");
				}
				
				return matches[0];
			}
        
			public bool HasPageType(AbstractPage.Pages page)
			{
				return FindPage(page, nullAware: true) != null;
			}
		}

	}
}
