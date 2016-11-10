using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Unclassified.TxLib;
using WikiEdit.Services;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit.ViewModels
{
    internal class WikiSiteListViewModel : BindableBase
    {
        private readonly WikiEditSessionService _SessionService;
        private readonly IChildViewModelService _ChildViewModelService;
        private readonly IViewModelFactory _ViewModelFactory;
        private WikiSiteViewModel _SelectedWikiSite;

        public WikiSiteViewModel SelectedWikiSite
        {
            get { return _SelectedWikiSite; }
            set
            {
                if (SetProperty(ref _SelectedWikiSite, value))
                    _RemoveWikiSiteCommand?.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<WikiSiteViewModel> WikiSites => _SessionService.WikiSites;

        /// <summary>
        /// Called from view, notifies that an item has been double-clicked.
        /// </summary>
        public void NotifyWikiSiteDoubleClick(WikiSiteViewModel site)
        {
            if (site == null) return;
            OpenWikiSiteOverview(site);
        }

        private WikiSiteOverviewViewModel OpenWikiSiteOverview(WikiSiteViewModel site)
        {
            return _ChildViewModelService.Documents.ActivateOrCreate(site,
                () => _ViewModelFactory.CreateWikiSiteOverview(site));
        }

        private DelegateCommand _AddWikiSiteCommand;

        public DelegateCommand AddWikiSiteCommand
        {
            get
            {
                if (_AddWikiSiteCommand == null)
                {
                    _AddWikiSiteCommand = new DelegateCommand(() =>
                    {
                        var site = _ViewModelFactory.CreateWikiSiteViewModel();
                        site.SiteName = Tx.T("wiki site.new site name");
                        WikiSites.Add(site);
                        SelectedWikiSite = site;
                        var overview = OpenWikiSiteOverview(site);
                        overview.EditWikiSiteCommand.Execute();
                    });
                }
                return _AddWikiSiteCommand;
            }
        }

        private DelegateCommand _RemoveWikiSiteCommand;

        public DelegateCommand RemoveWikiSiteCommand
        {
            get
            {
                if (_RemoveWikiSiteCommand == null)
                {
                    _RemoveWikiSiteCommand = new DelegateCommand(() =>
                        {
                            if (SelectedWikiSite == null) return;
                            if (Utility.Confirm(Tx.T("confirm.remove", "name", SelectedWikiSite.DisplayName)) == false)
                                return;
                            if (!_ChildViewModelService.Documents.CloseByWikiSite(SelectedWikiSite))
                                return;
                            WikiSites.Remove(SelectedWikiSite);
                            SelectedWikiSite = null;
                        }
                        , () => SelectedWikiSite != null);
                }
                return _RemoveWikiSiteCommand;
            }
        }

        public WikiSiteListViewModel(IChildViewModelService childViewModelService,
            IViewModelFactory viewModelFactory,
            WikiEditSessionService sessionService)
        {
            _SessionService = sessionService;
            _ViewModelFactory = viewModelFactory;
            _ChildViewModelService = childViewModelService;
        }
    }
}
