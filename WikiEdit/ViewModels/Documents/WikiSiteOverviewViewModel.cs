﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Common;
using Prism.Events;
using Unclassified.TxLib;
using WikiClientLibrary;
using WikiClientLibrary.Generators;
using WikiEdit.Services;

namespace WikiEdit.ViewModels.Documents
{
    internal class WikiSiteOverviewViewModel : DocumentViewModel
    {
        private CancellationTokenSource reloadSiteInfoCts;

        private readonly IChildViewModelService _ChildViewModelService;
        private readonly SettingsService _SettingsService;

        public WikiSiteViewModel WikiSite { get; }

        public override object DocumentContext => WikiSite;

        public override WikiSiteViewModel SiteContext => WikiSite;

        public ObservableCollection<RecentChangeViewModel> RecentChanges { get; } =
            new ObservableCollection<RecentChangeViewModel>();

        public WikiSiteOverviewViewModel(
            IEventAggregator eventAggregator,
            IChildViewModelService childViewModelService,
            SettingsService settingsService,
            WikiSiteViewModel wikiSite)
        {
            if (eventAggregator == null) throw new ArgumentNullException(nameof(eventAggregator));
            if (childViewModelService == null) throw new ArgumentNullException(nameof(childViewModelService));
            if (settingsService == null) throw new ArgumentNullException(nameof(settingsService));
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            WikiSite = wikiSite;
            _ChildViewModelService = childViewModelService;
            _SettingsService = settingsService;
            if (!wikiSite.IsInitialized)
            {
                // Wait for site initialization
                IsBusy = true;
                Status = Tx.T("please wait");
                if (!wikiSite.IsBusy)
                    wikiSite.InitializeAsync().Forget();
                eventAggregator.GetEvent<SiteInfoRefreshedEvent>().Subscribe(OnSiteInfoRefreshed);
                eventAggregator.GetEvent<TaskFailedEvent>().Subscribe(OnSiteFailed);
            }
            Title = wikiSite.DisplayName;
            BuildContentId(wikiSite.ApiEndpoint);
        }

        #region Site Information

        private void OnSiteInfoRefreshed(WikiSiteViewModel site)
        {
            if (site == WikiSite)
            {
                if (reloadSiteInfoCts != null)
                {
                    reloadSiteInfoCts.Cancel();
                    reloadSiteInfoCts.Dispose();
                }
                reloadSiteInfoCts = new CancellationTokenSource();
                RelaodSiteInfoAsync(reloadSiteInfoCts.Token).Forget();
                NeedReinitializeSite = false;
            }
        }

        private void OnSiteFailed(object sender)
        {
            if (sender == WikiSite)
            {
                IsBusy = false;
                Status = WikiSite.Status;
                NeedReinitializeSite = true;
            }
        }

        private async Task RelaodSiteInfoAsync(CancellationToken cancellation)
        {
            IsBusy = true;
            Status = Tx.T("please wait");
            try
            {
                Title = WikiSite.DisplayName;
                const int RecentChangesCount = 50;
                var rcg = new RecentChangesGenerator(WikiSite.Site)
                {
                    PagingSize = RecentChangesCount,
                };
                var rc = await rcg.EnumRecentChangesAsync()
                    .Take(RecentChangesCount)
                    .Select(rce => new RecentChangeViewModel(rce))
                    .ToArray(cancellation);
                cancellation.ThrowIfCancellationRequested();

                RecentChanges.Clear();
                RecentChanges.AddRange(rc);
            }
            finally
            {
                IsBusy = false;
                Status = null;
            }
        }


        private DelegateCommand _ReinitializeSiteCommand;

        public DelegateCommand ReinitializeSiteCommand
        {
            get
            {
                if (_ReinitializeSiteCommand == null)
                {
                    _ReinitializeSiteCommand = new DelegateCommand(() =>
                    {
                        if (!WikiSite.IsInitialized)
                        {
                            IsBusy = true;
                            Status = Tx.T("please wait");
                            if (!WikiSite.IsBusy)
                                WikiSite.InitializeAsync().Forget();
                        }
                    }, () => _NeedReinitializeSite);
                }
                return _ReinitializeSiteCommand;
            }
        }

        private bool _NeedReinitializeSite;

        public bool NeedReinitializeSite
        {
            get { return _NeedReinitializeSite; }
            set
            {
                if (SetProperty(ref _NeedReinitializeSite, value))
                    _ReinitializeSiteCommand?.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Edit Page

        private string _EditPageTitle;

        public string EditPageTitle
        {
            get { return _EditPageTitle; }
            set
            {
                if (SetProperty(ref _EditPageTitle, value))
                {
                    UpdateEditPageAutoCompletionItemsAsync().Forget();
                    EditPageCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private async Task UpdateEditPageAutoCompletionItemsAsync()
        {
            // TODO cache search results
            if (string.IsNullOrWhiteSpace(_EditPageTitle)) return;
            if (!WikiSite.IsInitialized) return;
            var title = _EditPageTitle;
            var items = await WikiSite.GetAutoCompletionItemsAsync(title);
            if (title == _EditPageTitle)
            {
                // If we've fetched auto completion list fast enough...
                EditPageAutoCompletionItems = items.Select(i => i.Title).ToArray();
            }
        }

        private IList<string> _EditPageAutoCompletionItems;

        public IList<string> EditPageAutoCompletionItems
        {
            get { return _EditPageAutoCompletionItems; }
            set { SetProperty(ref _EditPageAutoCompletionItems, value); }
        }

        private DelegateCommand _EditPageCommand;

        public DelegateCommand EditPageCommand
        {
            get
            {
                if (_EditPageCommand == null)
                {
                    _EditPageCommand = new DelegateCommand(() =>
                    {
                        if (!WikiSite.IsInitialized) return;
                        try
                        {
                            _ChildViewModelService.Documents.AddAndActivate(
                                new PageEditorViewModel(_SettingsService, WikiSite, WikiSite.GetPage(EditPageTitle)));
                        }
                        catch (Exception ex)
                        {
                            Utility.ReportException(ex);
                        }
                    }, () => WikiSite.IsInitialized && !string.IsNullOrWhiteSpace(EditPageTitle));
                }
                return _EditPageCommand;
            }
        }

        #endregion

        protected override void OnClosed()
        {
            base.OnClosed();
            // Do some cleanup
            if (reloadSiteInfoCts != null)
            {
                reloadSiteInfoCts.Cancel();
                reloadSiteInfoCts.Dispose();
                reloadSiteInfoCts = null;
            }
        }
    }
}
