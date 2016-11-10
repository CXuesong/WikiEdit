using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using WikiClientLibrary;
using WikiEdit.Models;
using WikiEdit.ViewModels;
using WikiEdit.ViewModels.Documents;
using WikiEdit.Views.Documents;

namespace WikiEdit.Services
{
    /// <summary>
    /// This factory interface is used to create various kinds of document view models.
    /// This interface is responsible for injecting the dependencies of these view models.
    /// </summary>
    internal interface IViewModelFactory
    {
        #region Documents

        WikiSiteOverviewViewModel CreateWikiSiteOverview(WikiSiteViewModel wikiSite);

        PageEditorViewModel CreatePageEditor(WikiSiteViewModel wikiSite);

        #endregion

        #region Encapsulated Models

        WikiSiteViewModel CreateWikiSiteViewModel();

        #endregion

        RecentChangeViewModel CreateRecentChange(RecentChangesEntry model, WikiSiteViewModel wikiSite);

        Task<PageEditorViewModel> OpenPageEditorAsync(WikiSiteViewModel wikiSite, string pageTitle);

        /// <summary>
        /// Open the page of the specified title, using an appropriate action.
        /// </summary>
        Task OpenPageAsync(WikiSiteViewModel wikiSite, string pageTitle);

        WikiSiteEditingViewModel CreateWikiSiteEditingViewModel(Action accpentCallback, Action cancelCallback);

        PageDiffViewModel CreatePageDiffViewModel(WikiSiteViewModel wikiSite);

        PageDiffViewModel OpenPageDiffViewModel(WikiSiteViewModel wikiSite, int revisionId1, int revisionId2);
    }

    internal class ViewModelFactory : IViewModelFactory
    {
        private readonly IEventAggregator _EventAggregator;
        private readonly IChildViewModelService _ChildViewModelService;
        private readonly SettingsService _SettingsService;
        private readonly ITextEditorFactory _TextEditorFactory;
        private readonly WikiEditSessionService _SessionService;

        public ViewModelFactory(IEventAggregator eventAggregator,
            IChildViewModelService childViewModelService,
            SettingsService settingsService, ITextEditorFactory textEditorFactory, 
            WikiEditSessionService sessionService)
        {
            _EventAggregator = eventAggregator;
            _ChildViewModelService = childViewModelService;
            _SettingsService = settingsService;
            _TextEditorFactory = textEditorFactory;
            _SessionService = sessionService;
        }

        public WikiSiteOverviewViewModel CreateWikiSiteOverview(WikiSiteViewModel wikiSite)
        {
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            return new WikiSiteOverviewViewModel(this, _SettingsService, wikiSite);
        }

        public PageEditorViewModel CreatePageEditor(WikiSiteViewModel wikiSite)
        {
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            return new PageEditorViewModel(_SettingsService, _TextEditorFactory, wikiSite);
        }

        /// <inheritdoc />
        public WikiSiteViewModel CreateWikiSiteViewModel()
        {
            return new WikiSiteViewModel(_EventAggregator, _SessionService);
        }

        public RecentChangeViewModel CreateRecentChange(RecentChangesEntry model, WikiSiteViewModel wikiSite)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            return new RecentChangeViewModel(this, wikiSite, model);
        }

        public async Task<PageEditorViewModel> OpenPageEditorAsync(WikiSiteViewModel wikiSite, string pageTitle)
        {
            var site = await wikiSite.GetSiteAsync();
            var normalizedTitle = WikiLink.NormalizeWikiLink(site, pageTitle);
            var editor = _ChildViewModelService.Documents
                .OfType<PageEditorViewModel>()
                .Where(vm => vm.SiteContext == wikiSite)
                .FirstOrDefault(vm => vm.WikiPage.Title == normalizedTitle);
            if (editor == null)
            {
                editor = CreatePageEditor(wikiSite);
                editor.SetWikiPageAsync(pageTitle).Forget();
                _ChildViewModelService.Documents.Add(editor);
            }
            editor.IsActive = true;
            return editor;
        }

        /// <inheritdoc />
        public async Task OpenPageAsync(WikiSiteViewModel wikiSite, string pageTitle)
        {
            // TODO Maybe show the page in the application, in the future.
            var url = await wikiSite.GetPageUrlAsync(pageTitle);
            Utility.OpenUrl(url);
        }

        /// <inheritdoc />
        public WikiSiteEditingViewModel CreateWikiSiteEditingViewModel(Action accpentCallback, Action cancelCallback)
        {
            return new WikiSiteEditingViewModel(_SessionService, accpentCallback, cancelCallback);
        }

        /// <inheritdoc />
        public PageDiffViewModel CreatePageDiffViewModel(WikiSiteViewModel wikiSite)
        {
            return new PageDiffViewModel(wikiSite);
        }

        /// <inheritdoc />
        public PageDiffViewModel OpenPageDiffViewModel(WikiSiteViewModel wikiSite, int revisionId1, int revisionId2)
        {
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            var viewer = _ChildViewModelService.Documents
                .OfType<PageDiffViewModel>().FirstOrDefault(vm =>
                        vm.SiteContext == wikiSite && vm.RevisionId1 == revisionId1 && vm.RevisionId2 == revisionId2);
            if (viewer == null)
            {
                viewer = CreatePageDiffViewModel(wikiSite);
                viewer.SetRevisionsAsync(revisionId1, revisionId2).Forget();
                _ChildViewModelService.Documents.Add(viewer);
            }
            viewer.IsActive = true;
            return viewer;
        }
    }
}
