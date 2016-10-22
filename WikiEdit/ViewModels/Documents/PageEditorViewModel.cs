using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using WikiClientLibrary;

namespace WikiEdit.ViewModels.Documents
{
    internal class PageEditorViewModel : DocumentViewModel
    {
        public PageEditorViewModel(WikiSiteViewModel wikiSite, Page wikiPage)
        {
            if (wikiPage == null) throw new ArgumentNullException(nameof(wikiPage));
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            Debug.Assert(wikiSite.Site == wikiPage.Site);
            WikiSite = wikiSite;
            WikiPage = wikiPage;
            Title = WikiPage.Title;
        }

        public WikiSiteViewModel WikiSite { get; }

        /// <summary>
        /// The page to be edited.
        /// </summary>
        public Page WikiPage { get; }

        public override WikiSiteViewModel SiteContext => WikiSite;

        public override object DocumentContext => WikiPage;

        #region Edit Submission

        private string _EditSummary;

        public string EditSummary
        {
            get { return _EditSummary; }
            set { SetProperty(ref _EditSummary, value); }
        }

        private DelegateCommand _SubmitEditCommand;

        public DelegateCommand SubmitEditCommand
        {
            get
            {
                if (_SubmitEditCommand == null)
                {
                    _SubmitEditCommand = new DelegateCommand(() => { });
                }
                return _SubmitEditCommand;
            }
        }

        #endregion
    }
}
