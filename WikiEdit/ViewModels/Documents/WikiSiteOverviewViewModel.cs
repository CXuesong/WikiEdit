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

        public WikiSiteOverviewViewModel(WikiSiteViewModel wikiSite)
        {
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            WikiSite = wikiSite;
            Title = WikiSite.DisplayName;
            PropertyChangedEventManager.AddHandler(WikiSite, WikiSite_PropertyChanged, nameof(WikiSite.DisplayName));
            BuildContentId(wikiSite.ApiEndpoint);
        }

        private void WikiSite_PropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            Title = WikiSite.DisplayName;
        }
    }
}
