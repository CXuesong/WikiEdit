using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using Prism.Mvvm;
using Unclassified.TxLib;
using WikiClientLibrary;
using WikiEdit.Models;
using WikiEdit.Services;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit.ViewModels
{
    public class WikiSiteViewModel : BindableBase
    {
        private readonly WikiEditSessionService _SessionService;
        private readonly IEventAggregator _EventAggregator;
        private readonly WikiSite _Model;

        // We need some weak event listeners… PubSubEvent can do this.
        /// <summary>
        /// Fires when the <see cref="Site"/> instance has been invalidated.
        /// </summary>
        public readonly PubSubEvent SiteInvalidatedEvent = new PubSubEvent();

        /// <summary>
        /// Fires when the <see cref="Site"/> instance/content has been refreshed.
        /// </summary>
        public readonly PubSubEvent SiteRefreshedEvent = new PubSubEvent();

        /// <summary>
        /// Fires when the account information has been refreshed.
        /// </summary>
        public readonly PubSubEvent AccountRefreshedEvent = new PubSubEvent();

        #region User Settings

        /// <summary>
        /// User-defined site name.
        /// </summary>
        public string Name
        {
            get { return _Model.Name; }
            set
            {
                if (_Model.Name == value) return;
                _Model.Name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string ApiEndpoint
        {
            get { return _Model.ApiEndpoint; }
            set
            {
                if (_Model.ApiEndpoint == value) return;
                _Model.ApiEndpoint = value;
                OnPropertyChanged();
            }
        }

        public DateTimeOffset LastAccessTime
        {
            get { return _Model.LastAccessTime; }
            set
            {
                if (_Model.LastAccessTime == value) return;
                _Model.LastAccessTime = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Persistable Site Information

        /// <summary>
        /// Site title.
        /// </summary>
        public string SiteName
        {
            get { return _Model.SiteName; }
            set
            {
                if (_Model.SiteName == value) return;
                _Model.SiteName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }


        /// <summary>
        /// Site URL.
        /// </summary>
        public string SiteUrl
        {
            get { return _Model.SiteUrl; }
            private set
            {
                if (_Model.SiteUrl == value) return;
                _Model.SiteUrl = value;
                OnPropertyChanged();
            }
        }

        public string MediaWikiVersion
        {
            get { return _Model.MediaWikiVersion; }
            private set
            {
                if (_Model.MediaWikiVersion == value) return;
                _Model.MediaWikiVersion = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public string DisplayName => string.IsNullOrWhiteSpace(_Model.Name) ? _Model.SiteName : _Model.Name;

        public AccountProfileViewModel AccountProfile { get; }

        /// <summary>
        /// DO NOT directly use this field. Use <see cref="GetSiteAsync"/> instead.
        /// </summary>
        private Site _Site;
        private Task<Site> _GetSiteTask;
        private CancellationTokenSource _GetSiteTaskCts;

        /// <summary>
        /// Asynchronously get/create a WCL Site instance.
        /// </summary>
        public Task<Site> GetSiteAsync()
        {
            if (_Site != null) return Task.FromResult(_Site);
            if (_GetSiteTask == null)
            {
                _GetSiteTaskCts?.Dispose();
                if (!Utility.ValidateApiEndpointBasic(_Model.ApiEndpoint))
                    throw new InvalidOperationException(Tx.T("errors.invalid api endpoint"));
                _GetSiteTaskCts = new CancellationTokenSource();
                _GetSiteTask = InitializeSiteAsync(_GetSiteTaskCts.Token);
            }
            return _GetSiteTask;
        }

        /// <summary>
        /// Invalidates the current <see cref="Site"/> instance maintined
        /// by the view model.
        /// </summary>
        public void InvalidateSite()
        {
            _GetSiteTaskCts?.Cancel();
            if (_Site == null) return;
            _Site = null;
            SiteInvalidatedEvent.Publish();
        }

        private async Task<Site> InitializeSiteAsync(CancellationToken ct)
        {
            try
            {
                Debug.Assert(_Site == null);
                var s = await _SessionService.CreateSiteAsync(_Model.ApiEndpoint);
                ct.ThrowIfCancellationRequested();
                Debug.Assert(_Site == null);
                _Site = s;
                PullSiteInfo();
                // Publish events.
                SiteRefreshedEvent.Publish();
                AccountRefreshedEvent.Publish();
                return _Site;
            }
            finally
            {
                _GetSiteTaskCts.Dispose();
                _GetSiteTaskCts = null;
                _GetSiteTask = null;
            }
        }

        private void PullSiteInfo()
        {
            Debug.Assert(_Site != null);
            // Load site information.
            SiteName = _Site.SiteInfo.SiteName;
            SiteUrl = _Site.SiteInfo.ServerUrl;
            MediaWikiVersion = _Site.SiteInfo.Generator;
            // Load user information.
            _Model.UserName = _Site.UserInfo.Name;
            _Model.UserGroups = _Site.UserInfo.Groups;
            LastAccessTime = DateTimeOffset.Now;
        }

        #region Actions

        private readonly Dictionary<string, Tuple<IList<OpenSearchResultEntry>, DateTime>> _AutoCompletionItemsCache
            = new Dictionary<string, Tuple<IList<OpenSearchResultEntry>, DateTime>>();


        private readonly Dictionary<string, Tuple<PageSummary, DateTime>> _PageSummaryCache
            = new Dictionary<string, Tuple<PageSummary, DateTime>>();

        private readonly TimeSpan _CacheExpiry = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Given the prefix of a page, asynchronously search for a list of titles for auto completion.
        /// </summary>
        /// <remarks>Do not modify the returned list.</remarks>
        public async Task<IList<OpenSearchResultEntry>> GetAutoCompletionItemsAsync(string expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var normalizedExpression = WikiLink.NormalizeWikiLink(await GetSiteAsync(), expression);
            var cached = _AutoCompletionItemsCache.TryGetValue(normalizedExpression);
            if (cached != null && DateTime.Now - cached.Item2 < _CacheExpiry)
                return cached.Item1;
            var site = await GetSiteAsync();
            var entries = await site.OpenSearchAsync(expression);
            _AutoCompletionItemsCache[normalizedExpression] = Tuple.Create(entries, DateTime.Now);
            return entries;
        }

        /// <summary>
        /// Given the summary of a page asynchronously.
        /// </summary>
        /// <exception cref="ArgumentNullException">title is null.</exception>
        /// <exception cref="ArgumentException">title is invalid.</exception>
        /// <remarks>Do not modify the returned objects.</remarks>
        public async Task<IList<PageSummary>> GetPageSummaryAsync(IEnumerable<string> titles)
        {
            if (titles == null) throw new ArgumentNullException(nameof(titles));
            var site = await GetSiteAsync();
            var titleList = titles.Select(t => WikiLink.NormalizeWikiLink(site, t)).ToArray();
            var pagesToCheck = new List<Page>();
            var pagesToFetch = new List<Page>();
            var pagesToCheckRedirect = new List<Tuple<Page, PageSummary>>();
            foreach (var title in titleList)
            {
                var cached = _PageSummaryCache.TryGetValue(title);
                if (cached != null && DateTime.Now - cached.Item2 < _CacheExpiry)
                {
                    continue;
                }
                pagesToCheck.Add(new Page(site, title));
            }
            await pagesToCheck.RefreshAsync();
            foreach (var page in pagesToCheck)
            {
                var cached = _PageSummaryCache.TryGetValue(page.Title);
                // Check if the cache is obsolete.
                if (cached != null && page.LastRevisionId == cached.Item1.LastRevisionId)
                {
                    _PageSummaryCache[page.Title] = Tuple.Create(cached.Item1, DateTime.Now);
                    continue;
                }
                // We need to fetch the whole page.
                pagesToFetch.Add(page);
            }
            // Fetch basic information.
            await pagesToFetch.RefreshAsync();
            foreach (var page in pagesToFetch)
            {
                var summary = new PageSummary
                {
                    Title = page.Title,
                    LastRevisionId = page.LastRevisionId,
                    LastRevisionTime = page.LastRevision?.TimeStamp ?? DateTime.MinValue,
                    LastRevisionUser = page.LastRevision?.UserName,
                    ContentLength = page.ContentLength,
                };
                //summary.Description = 
                _PageSummaryCache[page.Title] = Tuple.Create(summary, DateTime.Now);
                if (page.IsRedirect)
                {
                    pagesToCheckRedirect.Add(Tuple.Create(page, summary));
                }
            }
            // Fetch redirects.
            await pagesToCheckRedirect.Select(t => t.Item1).RefreshAsync(PageQueryOptions.ResolveRedirects);
            foreach (var tp in pagesToCheckRedirect)
            {
                var path = tp.Item1.RedirectPath.ToList();
                // Add & complete redirect path
                path.Add(tp.Item1.Title);
                tp.Item2.RedirectPath = path;
            }
            return titleList.Select(t => _PageSummaryCache[t].Item1).ToArray();
        }

        public void CrushCache()
        {
            var now = DateTime.Now;
            foreach (var key in _AutoCompletionItemsCache
                .Where(p => p.Value.Item2 - now > _CacheExpiry)
                .Select(p => p.Key).ToArray())
                _AutoCompletionItemsCache.Remove(key);
            foreach (var key in _PageSummaryCache
                .Where(p => p.Value.Item2 - now > _CacheExpiry)
                .Select(p => p.Key).ToArray())
                _PageSummaryCache.Remove(key);
        }

        private static readonly Uri DummyHttp = new Uri("http://dummy");

        /// <summary>
        /// Gets the full page URL from the specified page title.
        /// </summary>
        public Task<string> GetPageUrlAsync(string pageTitle)
        {
            return GetPageUrlAsync(pageTitle, null);
        }

        /// <summary>
        /// Gets the full page URL from the specified page title.
        /// </summary>
        public async Task<string> GetPageUrlAsync(string pageTitle, string query)
        {
            if (pageTitle == null) throw new ArgumentNullException(nameof(pageTitle));
            var site = await GetSiteAsync();
            // Infers the absolute URL
            var uri = new Uri(DummyHttp, site.SiteInfo.ServerUrl);
            uri = new Uri(uri, site.SiteInfo.ArticlePath);
            var pageUrl = uri.ToString();
            pageUrl = pageUrl.Replace("$1", pageTitle.Trim());
            if (query != null) pageUrl = pageUrl + "?" + query;
            return pageUrl;
        }

        #endregion

        public async Task RefreshAccountInfoAsync()
        {
            if (_Site == null)
            {
                await GetSiteAsync();
                return;
            }
            var site = await GetSiteAsync();
            await site.RefreshUserInfoAsync();
            AccountRefreshedEvent.Publish();
        }

        internal WikiSite GetModel() => _Model;

        /// <summary>
        /// Create a new Wiki site instance.
        /// </summary>
        internal WikiSiteViewModel(IEventAggregator eventAggregator, WikiEditSessionService sessionService)
        {
            if (sessionService == null) throw new ArgumentNullException(nameof(sessionService));
            _Model = new WikiSite();
            _SessionService = sessionService;
            _EventAggregator = eventAggregator;
            AccountProfile = new AccountProfileViewModel(eventAggregator, this);
        }

        /// <summary>
        /// Create an instance from existing model.
        /// </summary>
        internal WikiSiteViewModel(IEventAggregator eventAggregator, WikiEditSessionService sessionService, WikiSite model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (sessionService == null) throw new ArgumentNullException(nameof(sessionService));
            if (eventAggregator == null) throw new ArgumentNullException(nameof(eventAggregator));
            _Model = model;
            _SessionService = sessionService;
            _EventAggregator = eventAggregator;
            AccountProfile = new AccountProfileViewModel(eventAggregator, this, model);
        }
    }
}
