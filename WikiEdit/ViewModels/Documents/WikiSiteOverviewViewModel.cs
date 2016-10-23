using System;
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
    public class WikiSiteOverviewViewModel : DocumentViewModel
    {
        private readonly IViewModelFactory _ViewModelFactory;
        private CancellationTokenSource reloadSiteInfoCts;

        public WikiSiteViewModel WikiSite { get; }

        public override object DocumentContext => WikiSite;

        public override WikiSiteViewModel SiteContext => WikiSite;

        public ObservableCollection<RecentChangeViewModel> RecentChanges { get; } =
            new ObservableCollection<RecentChangeViewModel>();

        public WikiSiteOverviewViewModel(IViewModelFactory viewModelFactory, SettingsService settingsService, WikiSiteViewModel wikiSite)
        {
            if (viewModelFactory == null) throw new ArgumentNullException(nameof(viewModelFactory));
            if (settingsService == null) throw new ArgumentNullException(nameof(settingsService));
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            WikiSite = wikiSite;
            _ViewModelFactory = viewModelFactory;
            Title = wikiSite.DisplayName;
            BuildContentId(wikiSite.ApiEndpoint);
            InitializeAsync().Forget();
        }

        private async Task InitializeAsync()
        {
            IsBusy = true;
            Status = Tx.T("please wait");
            try
            {
                await WikiSite.GetSiteAsync();
                Title = WikiSite.DisplayName;
                NeedReinitializeSite = false;
            }
            catch (Exception ex)
            {
                Status = ex.Message;
                NeedReinitializeSite = true;
            }
            IsBusy = false;
            RefreshRecentActivities();
        }

        protected override void OnIsBusyChanged()
        {
            base.OnIsBusyChanged();
            _ReinitializeSiteCommand?.RaiseCanExecuteChanged();
            _EditPageCommand?.RaiseCanExecuteChanged();
        }

        #region Site Information

        private void RefreshRecentActivities()
        {
            if (reloadSiteInfoCts != null)
            {
                reloadSiteInfoCts.Cancel();
                reloadSiteInfoCts.Dispose();
            }
            reloadSiteInfoCts = new CancellationTokenSource();
            RefreshRecentActivitiesAsync(reloadSiteInfoCts.Token).Forget();
        }

        private async Task RefreshRecentActivitiesAsync(CancellationToken cancellation)
        {
            IsBusy = true;
            Status = Tx.T("please wait");
            try
            {
                const int RecentChangesCount = 50;
                var rcg = new RecentChangesGenerator(await WikiSite.GetSiteAsync())
                {
                    PagingSize = RecentChangesCount,
                };
                var rc = await rcg.EnumRecentChangesAsync()
                    .Take(RecentChangesCount)
                    .Select(rce => _ViewModelFactory.CreateRecentChange(rce, WikiSite))
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
                        InitializeAsync().Forget();
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
                    _EditPageCommand = new DelegateCommand(async () =>
                    {
                        try
                        {
                            await _ViewModelFactory.OpenPageEditorAsync(WikiSite, EditPageTitle);
                        }
                        catch (Exception ex)
                        {
                            Utility.ReportException(ex);
                        }
                    }, () => !IsBusy && !string.IsNullOrWhiteSpace(EditPageTitle));
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
