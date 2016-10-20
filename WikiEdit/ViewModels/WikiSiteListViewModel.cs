using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using WikiEdit.Controllers;
using WikiEdit.Services;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit.ViewModels
{
    internal class WikiSiteListViewModel : BindableBase
    {
        private readonly WikiEditController wikiEditController;
        private readonly IChildViewModelService childVmService;
        private WikiSiteViewModel _SelectedWikiSite;
        private bool _IsAccountProfileVisible;

        public WikiSiteViewModel SelectedWikiSite
        {
            get { return _SelectedWikiSite; }
            set
            {
                if (SetProperty(ref _SelectedWikiSite, value))
                    IsAccountProfileVisible = value != null;
            }
        }

        public bool IsAccountProfileVisible
        {
            get { return _IsAccountProfileVisible; }
            set { SetProperty(ref _IsAccountProfileVisible, value); }
        }

        public ObservableCollection<WikiSiteViewModel> WikiSites => wikiEditController.WikiSites;

        /// <summary>
        /// Called from view, notifies that an item has been double-clicked.
        /// </summary>
        public void NotifyWikiSiteDoubleClick(WikiSiteViewModel site)
        {
            if (site == null) return;
            var doc = childVmService.DocumentViewModels.GetOrCreate(site, () => new WikiSiteOverviewViewModel(site));
            doc.IsSelected = true;
            doc.IsActive = true;
        }

        public WikiSiteListViewModel(WikiEditController controller, IChildViewModelService childVmService)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            if (childVmService == null) throw new ArgumentNullException(nameof(childVmService));
            wikiEditController = controller;
            this.childVmService = childVmService;
        }
    }
}
