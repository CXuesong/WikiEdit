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
using WikiEdit.Controllers;
using WikiEdit.Models;
using WikiEdit.Services;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit.ViewModels
{
    public class WikiSiteViewModel : BindableBase
    {
        private readonly WikiEditController _Controller;
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
                var s = await _Controller.CreateSiteAsync(_Model.ApiEndpoint);
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

        /// <summary>
        /// Given the prefix of a page, asynchronously search for a list of titles for auto completion.
        /// </summary>
        public async Task<IList<OpenSearchResultEntry>> GetAutoCompletionItemsAsync(string expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var site = await GetSiteAsync();
            var entries = await site.OpenSearchAsync(expression);
            // TODO cache results.
            return entries;
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
        internal WikiSiteViewModel(IEventAggregator eventAggregator, WikiEditController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            _Model = new WikiSite();
            _Controller = controller;
            _EventAggregator = eventAggregator;
            AccountProfile = new AccountProfileViewModel(eventAggregator, this);
        }

        /// <summary>
        /// Create an instance from existing model.
        /// </summary>
        internal WikiSiteViewModel(IEventAggregator eventAggregator, WikiEditController controller, WikiSite model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            if (eventAggregator == null) throw new ArgumentNullException(nameof(eventAggregator));
            _Model = model;
            _Controller = controller;
            _EventAggregator = eventAggregator;
            AccountProfile = new AccountProfileViewModel(eventAggregator, this, model);
        }
    }
}
