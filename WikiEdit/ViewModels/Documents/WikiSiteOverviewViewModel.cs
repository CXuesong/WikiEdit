using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Prism.Commands;
using Prism.Common;
using Prism.Events;
using Unclassified.TxLib;
using WikiClientLibrary;
using WikiClientLibrary.Generators;
using WikiEdit.Services;
using WikiEdit.Views;

namespace WikiEdit.ViewModels.Documents
{
    internal class WikiSiteOverviewViewModel : DocumentViewModel
    {
        private readonly IViewModelFactory _ViewModelFactory;
        private CancellationTokenSource reloadSiteInfoCts;

        public override object DocumentContext => SiteContext;

        public ObservableCollection<RecentChangeViewModel> RecentChangesList { get; } =
            new ObservableCollection<RecentChangeViewModel>();

        public ListCollectionView RecentChangesView { get; }

        public WikiSiteOverviewViewModel(IViewModelFactory viewModelFactory, SettingsService settingsService, WikiSiteViewModel wikiSite)
        {
            if (viewModelFactory == null) throw new ArgumentNullException(nameof(viewModelFactory));
            if (settingsService == null) throw new ArgumentNullException(nameof(settingsService));
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            SiteContext = wikiSite;
            RecentChangesView = new ListCollectionView(RecentChangesList);
            RecentChangesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RecentChangeViewModel.TimeStamp), DateGroupingConverter.Default));
            _ViewModelFactory = viewModelFactory;
            Title = ToolTip = wikiSite.DisplayName;
            BuildContentId(wikiSite.ApiEndpoint);
            RefreshSiteInfoAsync().Forget();
        }

        private async Task RefreshSiteInfoAsync()
        {
            IsBusy = true;
            Status = Tx.T("please wait");
            try
            {
                await SiteContext.GetSiteAsync();
                Title = ToolTip = SiteContext.DisplayName;
            }
            catch (Exception ex)
            {
                Status = Utility.GetExceptionMessage(ex);
            }
            finally
            {
                IsBusy = false;
            }
            await RefreshRecentActivitiesAsync();
        }

        protected override void OnIsBusyChanged()
        {
            base.OnIsBusyChanged();
            _RefreshSiteCommand?.RaiseCanExecuteChanged();
            _EditPageCommand?.RaiseCanExecuteChanged();
        }

        #region Site Information

        private async Task RefreshRecentActivitiesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            Status = Tx.T("please wait");
            try
            {
                var rcg = new RecentChangesGenerator(await SiteContext.GetSiteAsync())
                {
                    PagingSize = RecentActivitiesPagingSize + 10,
                };
                var rcs = await rcg.EnumRecentChangesAsync()
                    .Take(RecentActivitiesPagingSize).ToArray();
                RecentChangesList.Clear();
                foreach (var rc in rcs)
                {
                    var vm = _ViewModelFactory.CreateRecentChange(rc, SiteContext);
                    var date = rc.TimeStamp.Date;
                    RecentChangesList.Add(vm);
                }
            }
            finally
            {
                IsBusy = false;
                Status = null;
            }
        }

        private DelegateCommand _RefreshSiteCommand;

        public DelegateCommand RefreshSiteCommand
        {
            get
            {
                if (_RefreshSiteCommand == null)
                {
                    _RefreshSiteCommand = new DelegateCommand(() =>
                    {
                        //SiteContext.InvalidateSite();
                        // We'll also invalidate all the opened page editors.
                        RefreshSiteInfoAsync().Forget();
                    }, () => !IsBusy);
                }
                return _RefreshSiteCommand;
            }
        }
        private WikiSiteEditingViewModel _WikiSiteEditor;

        public WikiSiteEditingViewModel WikiSiteEditor
        {
            get { return _WikiSiteEditor; }
            private set { SetProperty(ref _WikiSiteEditor, value); }
        }

        private DelegateCommand _EditWikiSiteCommand;

        public DelegateCommand EditWikiSiteCommand
        {
            get
            {
                if (_EditWikiSiteCommand == null)
                {
                    _EditWikiSiteCommand = new DelegateCommand(() =>
                    {
                        if (WikiSiteEditor != null) return;
                        WikiSiteEditor = _ViewModelFactory.CreateWikiSiteEditingViewModel(
                            () =>
                            {
                                SiteContext.Name = WikiSiteEditor.Name;
                                SiteContext.ApiEndpoint = WikiSiteEditor.ApiEndpoint;
                                SiteContext.InvalidateSite();
                                RefreshSiteInfoAsync().Forget();
                                WikiSiteEditor = null;
                            }, () => WikiSiteEditor = null);
                        WikiSiteEditor.LoadFromWikiSite(SiteContext);
                    }, () => WikiSiteEditor == null);
                }
                return _EditWikiSiteCommand;
            }
        }

        public static IList<int> PagingSizeChoices { get; } = new[] {25, 50, 100, 200};

        private int _RecentActivitiesPagingSize = 50;
                    
        public int RecentActivitiesPagingSize
        {
            get { return _RecentActivitiesPagingSize; }
            set
            {
                if (SetProperty(ref _RecentActivitiesPagingSize, value))
                    RefreshRecentActivitiesAsync().Forget();
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
            var items = await SiteContext.GetAutoCompletionItemsAsync(title);
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
                            await _ViewModelFactory.OpenPageEditorAsync(SiteContext, EditPageTitle);
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

        private class DateGroupingConverter : IValueConverter
        {
            public static readonly DateGroupingConverter Default = new DateGroupingConverter();

            /// <inheritdoc />
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return Tx.Time((DateTime) value, TxTime.YearMonthDay | TxTime.DowLong);
            }

            /// <inheritdoc />
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
