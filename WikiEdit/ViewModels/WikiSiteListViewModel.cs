using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using Prism.Mvvm;
using WikiEdit.Controllers;
using WikiEdit.Services;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit.ViewModels
{
    internal class WikiSiteListViewModel : BindableBase
    {
        private readonly WikiEditController _WikiEditController;
        private readonly IChildViewModelService _ChildViewModelService;
        private readonly IViewModelFactory _ViewModelFactory;
        private WikiSiteViewModel _SelectedWikiSite;

        public WikiSiteViewModel SelectedWikiSite
        {
            get { return _SelectedWikiSite; }
            set { SetProperty(ref _SelectedWikiSite, value); }
        }

        public ObservableCollection<WikiSiteViewModel> WikiSites => _WikiEditController.WikiSites;

        /// <summary>
        /// Called from view, notifies that an item has been double-clicked.
        /// </summary>
        public void NotifyWikiSiteDoubleClick(WikiSiteViewModel site)
        {
            if (site == null) return;
            var doc = _ChildViewModelService.Documents.ActivateOrCreate(site,
                () => _ViewModelFactory.CreateWikiSiteOverview(site));
        }

        public WikiSiteListViewModel(IChildViewModelService childViewModelService,
            IViewModelFactory viewModelFactory,
            WikiEditController controller)
        {
            _WikiEditController = controller;
            _ViewModelFactory = viewModelFactory;
            _ChildViewModelService = childViewModelService;
        }
    }
}
