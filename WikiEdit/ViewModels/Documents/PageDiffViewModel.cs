using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unclassified.TxLib;
using WikiClientLibrary;
using WikiEdit.Models;
using WikiEdit.Services;

namespace WikiEdit.ViewModels.Documents
{
    public class PageDiffViewModel : DocumentViewModel
    {
        public PageDiffViewModel(WikiSiteViewModel wikiSite)
        {
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            SiteContext = wikiSite;
        }

        /// <summary>
        /// Sets the revision ids to diff. Note that even the pages can be different from each other.
        /// </summary>
        public async Task SetRevisionsAsync(int revisionId1, int revisionId2)
        {
            RevisionId1 = revisionId1;
            RevisionId2 = revisionId2;
            Revision1 = Revision2 = null;
            Title = Tx.T("page diff.general title", "name1", revisionId1.ToString(), "name2", revisionId2.ToString());
            IsBusy = true;
            Status = Tx.T("page diff.fetching", "revs", Tx.EnumAnd(revisionId1.ToString(), revisionId2.ToString()));
            try
            {
                Revision r1, r2;
                var site = await SiteContext.GetSiteAsync();
                if (revisionId1 == revisionId2)
                {
                    // O' Rly?
                    r1 = r2 = await Revision.FetchRevisionAsync(site, revisionId1);
                }
                else
                {
                    var rs = await Revision.FetchRevisionsAsync(site, revisionId1, revisionId2).ToArray();
                    r1 = rs[0];
                    r2 = rs[1];
                }
                SetRevisions(r1, r2);
                Status = null;
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Sets the revisions to diff.
        /// </summary>
        public void SetRevisions(Revision revision1, Revision revision2)
        {
            if (revision1 == null) throw new ArgumentNullException(nameof(revision1));
            if (revision2 == null) throw new ArgumentNullException(nameof(revision2));
            Debug.Assert(revision1.Page.Site == SiteContext.GetSiteAsync().Result);
            Debug.Assert(revision1.Page.Site == revision2.Page.Site);
            RevisionId1 = revision1.Id;
            RevisionId2 = revision2.Id;
            DeltaContentLength = revision2.ContentLength - revision1.ContentLength;
            if (revision1.Page.Title == revision2.Page.Title)
                Title = Tx.T("page diff.title", "name", revision1.Page.Title);
            else
                Title = Tx.T("page diff.general title", "name1", revision1.Page.Title, "name2", revision2.Page.Title);
            Revision1 = revision1;
            Revision2 = revision2;
            // Just throw the rest of the job to TextDiffViewer…
        }


        private int _RevisionId1;

        public int RevisionId1
        {
            get { return _RevisionId1; }
            private set { SetProperty(ref _RevisionId1, value); }
        }


        private int _RevisionId2;

        public int RevisionId2
        {
            get { return _RevisionId2; }
            private set { SetProperty(ref _RevisionId2, value); }
        }


        private Revision _Revision1;

        public Revision Revision1
        {
            get { return _Revision1; }
            private set { SetProperty(ref _Revision1, value); }
        }


        private Revision _Revision2;

        public Revision Revision2
        {
            get { return _Revision2; }
            private set { SetProperty(ref _Revision2, value); }
        }


        private int _DeltaContentLength;

        public int DeltaContentLength
        {
            get { return _DeltaContentLength; }
            private set { SetProperty(ref _DeltaContentLength, value); }
        }

    }
}
