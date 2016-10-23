using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using WikiClientLibrary;
using WikiEdit.ViewModels;
using WikiEdit.ViewModels.Documents;
using WikiEdit.Views.Documents;

namespace WikiEdit.Services
{
    /// <summary>
    /// This factory interface is used to create various kinds of document view models.
    /// This interface is responsible for injecting the dependencies of these view models.
    /// </summary>
    public interface IViewModelFactory
    {
        WikiSiteOverviewViewModel CreateWikiSiteOverview(WikiSiteViewModel wikiSite);

        PageEditorViewModel CreatePageEditor(Page page, WikiSiteViewModel wikiSite);

        RecentChangeViewModel CreateRecentChange(RecentChangesEntry model, WikiSiteViewModel wikiSite);

        Task<PageEditorViewModel> OpenPageEditorAsync(WikiSiteViewModel wikiSite, string pageTitle);
    }

    internal class ViewModelFactory : IViewModelFactory
    {
        private readonly IEventAggregator _EventAggregator;
        private readonly IChildViewModelService _ChildViewModelService;
        private readonly SettingsService _SettingsService;

        public ViewModelFactory(IEventAggregator eventAggregator,
            IChildViewModelService childViewModelService,
            SettingsService settingsService)
        {
            _EventAggregator = eventAggregator;
            _ChildViewModelService = childViewModelService;
            _SettingsService = settingsService;
        }

        public WikiSiteOverviewViewModel CreateWikiSiteOverview(WikiSiteViewModel wikiSite)
        {
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            return new WikiSiteOverviewViewModel(this, _SettingsService, wikiSite);
        }

        public PageEditorViewModel CreatePageEditor(Page page, WikiSiteViewModel wikiSite)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            return new PageEditorViewModel(_SettingsService, wikiSite, page);
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
            var editor = _ChildViewModelService.Documents.OfType<PageEditorViewModel>()
                .FirstOrDefault(vm => vm.WikiPage.Title == normalizedTitle);
            if (editor == null)
            {
                var page = new Page(site, normalizedTitle);
                editor = CreatePageEditor(page, wikiSite);
                _ChildViewModelService.Documents.AddAndActivate(editor);
            }
            editor.IsActive = true;
            return editor;
        }
    }
}
