using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using Prism.Mvvm;
using Xceed.Wpf.AvalonDock.Layout;

namespace WikiEdit.ViewModels.Documents
{
    internal class WikiSiteOverviewViewModel : DocumentViewModel
    {
        public WikiSiteViewModel WikiSite { get; }

        public override object ContentSource => WikiSite;

        public WikiSiteOverviewViewModel(IEventAggregator eventAggregator, WikiSiteViewModel wikiSite)
        {
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            WikiSite = wikiSite;
            if (!wikiSite.IsBusy && !wikiSite.IsInitialized)
#pragma warning disable 4014    // Just notify WikiSite need to be initialized.
                wikiSite.InitializeAsync();
#pragma warning restore 4014
            Title = wikiSite.DisplayName;
            eventAggregator.GetEvent<SiteInfoRefreshedEvent>();
            BuildContentId(wikiSite.ApiEndpoint);
        }

        private void OnSiteInfoRefreshed()
        {
            Title = WikiSite.DisplayName;
        }
    }
}
