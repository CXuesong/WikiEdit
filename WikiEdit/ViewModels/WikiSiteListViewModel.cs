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
        private readonly IEventAggregator _EventAggregator;
        private readonly WikiEditController wikiEditController;
        private readonly IChildViewModelService childVmService;
        private WikiSiteViewModel _SelectedWikiSite;

        public WikiSiteViewModel SelectedWikiSite
        {
            get { return _SelectedWikiSite; }
            set { SetProperty(ref _SelectedWikiSite, value); }
        }

        public ObservableCollection<WikiSiteViewModel> WikiSites => wikiEditController.WikiSites;

        /// <summary>
        /// Called from view, notifies that an item has been double-clicked.
        /// </summary>
        public void NotifyWikiSiteDoubleClick(WikiSiteViewModel site)
        {
            if (site == null) return;
            var doc = childVmService.Documents.GetOrCreate(site, 
                () => new WikiSiteOverviewViewModel(_EventAggregator, childVmService, site));
            doc.IsActive = true;
        }

        public WikiSiteListViewModel(IEventAggregator eventAggregator, IChildViewModelService childVmService, WikiEditController controller)
        {
            if (eventAggregator == null) throw new ArgumentNullException(nameof(eventAggregator));
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            if (childVmService == null) throw new ArgumentNullException(nameof(childVmService));
            _EventAggregator = eventAggregator;
            wikiEditController = controller;
            this.childVmService = childVmService;
        }
    }
}
