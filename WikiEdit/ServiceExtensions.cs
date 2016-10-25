using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikiClientLibrary;
using WikiEdit.Services;
using WikiEdit.ViewModels;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit
{
    public static class ServiceExtensions
    {
        public static bool CloseByWikiSite(this DocumentViewModelCollection vms,
            WikiSiteViewModel wikiSite)
        {
            return CloseByWikiSite(vms, wikiSite, null);
        }

        public static bool CloseByWikiSite(this DocumentViewModelCollection vms,
            WikiSiteViewModel wikiSite, DocumentViewModel excepts)
        {
            if (vms == null) throw new ArgumentNullException(nameof(vms));
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            return vms.CloseAll(vm => vm.SiteContext == wikiSite && vm != excepts);
        }
    }
}
