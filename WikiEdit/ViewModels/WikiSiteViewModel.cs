using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using Prism.Mvvm;
using Unclassified.TxLib;
using WikiClientLibrary;
using WikiEdit.Controllers;
using WikiEdit.Models;

namespace WikiEdit.ViewModels
{
    public class WikiSiteViewModel : BindableBase
    {
        private readonly WikiEditController _Controller;
        private readonly IEventAggregator _EventAggregator;
        private string _Name;
        private string _ApiEndpoint;
        private DateTimeOffset _LastAccessTime;
        private Site _Site;
        private string _SiteName;
        private string _SiteUrl;
        
        #region User Settings

        /// <summary>
        /// User-defined site name.
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set
            {
                if (SetProperty(ref _Name, value))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string ApiEndpoint
        {
            get { return _ApiEndpoint; }
            set { SetProperty(ref _ApiEndpoint, value); }
        }

        public DateTimeOffset LastAccessTime
        {
            get { return _LastAccessTime; }
            set { SetProperty(ref _LastAccessTime, value); }
        }

        #endregion

        #region Persistable Site Information

        /// <summary>
        /// Site title.
        /// </summary>
        public string SiteName
        {
            get { return _SiteName; }
            set
            {
                if (SetProperty(ref _SiteName, value))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }


        /// <summary>
        /// Site URL.
        /// </summary>
        public string SiteUrl
        {
            get { return _SiteUrl; }
            set { SetProperty(ref _SiteUrl, value); }
        }


        private string _MediaWikiVersion;

        public string MediaWikiVersion
        {
            get { return _MediaWikiVersion; }
            set { SetProperty(ref _MediaWikiVersion, value); }
        }


        #endregion

        public string DisplayName => string.IsNullOrWhiteSpace(_Name) ? _SiteName : _Name;

        public AccountProfileViewModel AccountProfile { get; }

        private bool _IsBusy;

        public bool IsBusy
        {
            get { return _IsBusy; }
            set { SetProperty(ref _IsBusy, value); }
        }

        private string _Status;

        public string Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }

        #region Actions

        /// <summary>
        /// Given the prefix of a page, asynchronously search for a list of titles for auto completion.
        /// </summary>
        public async Task<IList<OpenSearchResultEntry>> GetAutoCompletionItemsAsync(string expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (!IsInitialized) throw new InvalidOperationException();
            var entries = await Site.OpenSearchAsync(expression);
            // TODO cache results.
            return entries;
        }

        public Page GetPage(string title)
        {
            if (title == null) throw new ArgumentNullException(nameof(title));
            return new Page(Site, title);
        }

        #endregion

        public async Task InitializeAsync()
        {
            if (_Site == null)
            {
                Status = Tx.T("please wait");
                IsBusy = true;
                try
                {
                    _Site = await _Controller.CreateSiteAsync(_ApiEndpoint);
                    // Load site information.
                    SiteName = _Site.SiteInfo.SiteName;
                    SiteUrl = _Site.SiteInfo.ServerUrl;
                    MediaWikiVersion = _Site.SiteInfo.Generator;
                    // Publish events.
                    _EventAggregator.GetEvent<SiteInfoRefreshedEvent>().Publish(this);
                    _EventAggregator.GetEvent<AccountInfoRefreshedEvent>().Publish(this);
                    OnPropertyChanged(nameof(IsInitialized));
                    Status = null;
                }
                catch (Exception ex)
                {
                    Status = ex.Message;
                    _EventAggregator.GetEvent<TaskFailedEvent>().Publish(this);
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        public async Task RefreshAccountInfoAsync()
        {
            if (IsBusy) throw new InvalidOperationException();
            if (IsInitialized)
            {
                try
                {
                    IsBusy = true;
                    Status = Tx.T("account profile.fetching");
                    await Site.RefreshUserInfoAsync();
                    _EventAggregator.GetEvent<AccountInfoRefreshedEvent>().Publish(this);
                    Status = null;
                }
                catch (Exception ex)
                {
                    Status = ex.ToString();
                    _EventAggregator.GetEvent<TaskFailedEvent>().Publish(this);
                }
                finally
                {
                    IsBusy = false;
                }
            }
            else
            {
                await InitializeAsync();
            }
        }

        public bool IsInitialized => _Site != null;

        public Site Site
        {
            get
            {
                if (_Site == null)
                    throw new InvalidOperationException(
                        "InitializeSiteAsync should have finished before trying to get the property value.");
                return _Site;
            }
        }

        internal WikiSite ToModel() => new WikiSite
        {
            Name = _Name,
            SiteName = _SiteName,
            ApiEndpoint = _ApiEndpoint,
            LastAccessTime = _LastAccessTime,
            UserName = AccountProfile.UserName,
        };

        /// <summary>
        /// Create a new Wiki site instance.
        /// </summary>
        internal WikiSiteViewModel(IEventAggregator eventAggregator, WikiEditController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
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
            _Name = model.Name;
            _SiteName = model.SiteName;
            _ApiEndpoint = model.ApiEndpoint;
            _LastAccessTime = model.LastAccessTime;
            _Controller = controller;
            _EventAggregator = eventAggregator;
            AccountProfile = new AccountProfileViewModel(eventAggregator, this, model);
        }
    }
}
