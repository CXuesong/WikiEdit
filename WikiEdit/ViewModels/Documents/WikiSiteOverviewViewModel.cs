using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Xceed.Wpf.AvalonDock.Layout;

namespace WikiEdit.ViewModels.Documents
{
    internal class WikiSiteOverviewViewModel : DocumentViewModel
    {
        public WikiSiteViewModel WikiSite { get; }

        public WikiSiteOverviewViewModel(WikiSiteViewModel wikiSite)
        {
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            WikiSite = wikiSite;
            Title = WikiSite.DisplayName;
        }
    }
}
