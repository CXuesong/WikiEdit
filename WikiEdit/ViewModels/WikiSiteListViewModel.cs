using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using WikiEdit.Controllers;

namespace WikiEdit.ViewModels
{
    internal class WikiSiteListViewModel : BindableBase
    {
        private readonly WikiEditController wikiEditController;
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

        public WikiSiteListViewModel(WikiEditController controller)
        {
            wikiEditController = controller;
        }
    }
}
