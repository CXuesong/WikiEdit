using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Prism.Commands;
using Unclassified.TxLib;
using WikiClientLibrary;
using WikiEdit.Services;
using WikiEdit.Spark;
using WikiEdit.ViewModels.TextEditors;

namespace WikiEdit.ViewModels.Documents
{
    public class PageEditorViewModel : DocumentViewModel
    {
        private readonly SettingsService _SettingsService;
        private readonly ITextEditorFactory _TextEditorFactory;
        private readonly DispatcherTimer autoRefetchTimer;
        private readonly DispatcherTimer relativeDatesUpdateTimer;
        private DateTime lastRefreshTime;

        public PageEditorViewModel(SettingsService settingsService,
            ITextEditorFactory textEditorFactory,
            WikiSiteViewModel wikiSite)
        {
            if (settingsService == null) throw new ArgumentNullException(nameof(settingsService));
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            _SettingsService = settingsService;
            _TextEditorFactory = textEditorFactory;
            SiteContext = wikiSite;
            autoRefetchTimer = new DispatcherTimer();
            relativeDatesUpdateTimer = new DispatcherTimer();
            autoRefetchTimer.Tick += AutoRefetchTimer_Tick;
            relativeDatesUpdateTimer.Interval = TimeSpan.FromSeconds(10);
            relativeDatesUpdateTimer.Tick += (_, e) => UpdateRelativeDates();
            LoadSettings();
            settingsService.SettingsChangedEvent.Subscribe(LoadSettings);
        }

        private string _ProtectionAlertText;

        public string ProtectionAlertText
        {
            get { return _ProtectionAlertText; }
            set { SetProperty(ref _ProtectionAlertText, value); }
        }

        private string _AlertText;

        public string AlertText
        {
            get { return _AlertText; }
            set { SetProperty(ref _AlertText, value); }
        }

        /// <summary>
        /// Sets the wiki page to edit.
        /// </summary>
        public async Task SetWikiPageAsync(string title)
        {
            if (title == null) throw new ArgumentNullException(nameof(title));
            IsBusy = true;
            try
            {
                Title = title;
                var site = await SiteContext.GetSiteAsync();
                WikiPage = new Page(site, title);
                Title = WikiPage.Title;
                TitleToolTip = WikiPage.Title + " - " + SiteContext.DisplayName;
                await RefetchPageAsync(true, true);
            }
            catch (Exception ex)
            {
                Status = Utility.GetExceptionMessage(ex);
            }
            IsBusy = false;
        }

        /// <summary>
        /// The page to be edited.
        /// </summary>
        public Page WikiPage { get; private set; }

        public override object DocumentContext => WikiPage;

        private async Task RefetchPageAsync(bool fetchContent, bool replaceContent = false)
        {
            Status = Tx.T("editor.fetching page", "title", WikiPage.Title);
            IsBusy = true;
            try
            {
                await WikiPage.RefreshAsync(fetchContent ? PageQueryOptions.FetchContent : PageQueryOptions.None);
                lastRefreshTime = DateTime.Now;
                UpdateRelativeDates();
                ReloadPageInformation();
                if (fetchContent && replaceContent) ReloadPageContent();
                Status = null;
            }
            catch (Exception ex)
            {
                Status = Utility.GetExceptionMessage(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
        protected void ReloadPageInformation()
        {
            Title = WikiPage.Title;
            TitleToolTip = WikiPage.Title + " - " + SiteContext.DisplayName;
            if (WikiPage.Protections.Count == 0)
            {
                ProtectionAlertText = null;
            }
            else
            {
                ProtectionAlertText = Tx.T("page.protected prompt")
                                      + string.Join(null, WikiPage.Protections.Select(p =>
                                          Tx.T("page.protected prompt.1",
                                              "expiry", Tx.Time(p.Expiry, TxTime.YearMonthDay | TxTime.HourMinute),
                                              "group", p.Level,
                                              "action", Tx.SafeText("page actions:" + p.Type))));
            }
            // Notifies VM that the "content" of wikipage has been changed.
            OnPropertyChanged(nameof(WikiPage));
        }

        protected void ReloadPageContent()
        {
            // Switch content model, if necessary.
            if (EditorContentModel != WikiPage.ContentModel)
            {
                var ls = _SettingsService.GetSettingsByWikiContentModel(WikiPage.ContentModel);
                if (string.IsNullOrEmpty(ls.LanguageName))
                {
                    TextEditor = null;
                }
                else
                {
                    TextEditor = _TextEditorFactory.CreateTextEditor(ls.LanguageName, true);
                    TextEditor.DocumentOutline = DocumentOutline;
                }
                EditorContentModel = WikiPage.ContentModel;
            }
            if (TextEditor != null)
            {
                TextEditor.TextBox.Text = WikiPage.Content;
                TextEditor.InvalidateDocumentOutline(true);
            }
        }

        private string _EditorLanguage;

        public string EditorLanguage
        {
            get { return _EditorLanguage; }
            set { SetProperty(ref _EditorLanguage, value); }
        }

        private string _EditorContentModel;

        public string EditorContentModel
        {
            get { return _EditorContentModel; }
            set { SetProperty(ref _EditorContentModel, value); }
        }

        private TextEditorViewModel _TextEditor;

        public TextEditorViewModel TextEditor
        {
            get { return _TextEditor; }
            set { SetProperty(ref _TextEditor, value); }
        }

        #region Edit Submission

        private string _EditorSummary;

        public string EditorSummary
        {
            get { return _EditorSummary; }
            set
            {
                if (SetProperty(ref _EditorSummary, value))
                    SubmitEditCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _EditorMinor;

        public bool EditorMinor
        {
            get { return _EditorMinor; }
            set { SetProperty(ref _EditorMinor, value); }
        }

        private bool? _EditorWatch = null;

        public bool? EditorWatch
        {
            get { return _EditorWatch; }
            set { SetProperty(ref _EditorWatch, value); }
        }

        private DelegateCommand _GenerateSummaryCommand;

        public DelegateCommand GenerateSummaryCommand
        {
            get
            {
                if (_GenerateSummaryCommand == null)
                {
                    _GenerateSummaryCommand = new DelegateCommand(() =>
                    {
                        if (IsBusy) return;
                        switch (EditorContentModel)
                        {
                            case "wikitext":
                                EditorSummary = SummaryBuilder.BuildWikitextSummary(WikiPage.Content,
                                    TextEditor.TextBox.Text);
                                break;
                            default:
                                MessageBox.Show("TODO: For now this feature only supports wikitext.");
                                break;
                        }
                    }, () => !IsBusy);
                }
                return _GenerateSummaryCommand;
            }
        }

        private DelegateCommand _SubmitEditCommand;

        public DelegateCommand SubmitEditCommand
        {
            get
            {
                if (_SubmitEditCommand == null)
                {
                    _SubmitEditCommand = new DelegateCommand(async () =>
                        {
                            if (IsBusy) return;
                            if (TextEditor == null) return;
                            var oldContent = WikiPage.Content;
                            var newContent = TextEditor.TextBox.Text;
                            if (string.IsNullOrWhiteSpace(EditorSummary) && newContent != oldContent)
                                return;
                            AlertText = Status = Tx.T("editor.submitting your edit");
                            IsBusy = true;
                            WikiPage.Content = newContent;
                            try
                            {
                                await WikiPage.UpdateContentAsync(EditorSummary, _EditorMinor, false,
                                    _EditorWatch == true
                                        ? AutoWatchBehavior.Watch
                                        : _EditorWatch == false
                                            ? AutoWatchBehavior.None
                                            : AutoWatchBehavior.Default);
                                AlertText = Status = Tx.T("editor.submission success", "title", WikiPage.Title, "revid",
                                    Convert.ToString(WikiPage.LastRevisionId));
                                // refetch page information
                                Status = Tx.T("editor.fetching page", "title", WikiPage.Title);
                                await RefetchPageAsync(false);
                                Status = null;
                            }
                            catch (Exception ex)
                            {
                                // Restore the content first.
                                WikiPage.Content = oldContent;
                                Status = Utility.GetExceptionMessage(ex);
                                // Edit conflict detected.
                                if (ex is OperationConflictException)
                                {
                                    await DetectExternalRevisionAsync();
                                }
                            }
                            finally
                            {
                                IsBusy = false;
                            }
                        },
                        () => !IsBusy && !string.IsNullOrWhiteSpace(_EditorSummary));
                }
                return _SubmitEditCommand;
            }
        }

        #endregion

        public void UpdateRelativeDates()
        {
            relativeDatesUpdateTimer.Interval = Utility.FitRelativeDateUpdateInterval(DateTime.Now - lastRefreshTime);
            OnPropertyChanged(nameof(LastFetchedTimeExpression));
        }

        #region Revision Refetching

        public string LastFetchedTimeExpression
            => Tx.TC("editor.last fetched at") + Tx.RelativeTime(lastRefreshTime);

        private DelegateCommand _RefetchLastRevisionCommand;

        public DelegateCommand RefetchLastRevisionCommand
        {
            get
            {
                if (_RefetchLastRevisionCommand == null)
                {
                    _RefetchLastRevisionCommand = new DelegateCommand(() =>
                    {
                        if (IsBusy) return;
                        DetectExternalRevisionAsync().Forget();
                    }, () => !IsBusy);
                }
                return _RefetchLastRevisionCommand;
            }
        }

        public async Task DetectExternalRevisionAsync()
        {
            if (WikiPage == null) return;
            var revision = WikiPage.LastRevisionId;
            if (revision == 0) return;
            await RefetchPageAsync(false);
            if (WikiPage.LastRevisionId != revision)
            {
                AlertText = Tx.T("editor.external revision detected",
                    "time", Tx.Time(WikiPage.LastRevision.TimeStamp, TxTime.YearMonthDay | TxTime.HourMinuteSecond),
                    "user", WikiPage.LastRevision.UserName);
                await RefetchPageAsync(true);
            }
        }

        private void AutoRefetchTimer_Tick(object sender, EventArgs e)
        {
            if (!IsBusy)
            {
                DetectExternalRevisionAsync().Forget();
            }
        }

        #endregion

        /// <inheritdoc />
        protected override void OnIsBusyChanged()
        {
            base.OnIsBusyChanged();
            _SubmitEditCommand?.RaiseCanExecuteChanged();
            _RefetchLastRevisionCommand?.RaiseCanExecuteChanged();
            _GenerateSummaryCommand?.RaiseCanExecuteChanged();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == nameof(IsSelected))
            {
                autoRefetchTimer.IsEnabled = IsSelected;
                relativeDatesUpdateTimer.IsEnabled = IsSelected;
                if (IsSelected)
                {
                    UpdateRelativeDates();
                    if (DateTime.Now - lastRefreshTime > autoRefetchTimer.Interval && !IsBusy)
                        DetectExternalRevisionAsync().Forget();
                }
            }
        }

        public void LoadSettings()
        {
            var interval = _SettingsService.RawSettings.TextEditor.LastRevisionAutoRefetchInterval;
            if (interval < TimeSpan.FromSeconds(10)) interval = TimeSpan.FromSeconds(30);
            autoRefetchTimer.Interval = interval;
        }

        /// <inheritdoc />
        protected override void OnClosed()
        {
            base.OnClosed();
            autoRefetchTimer.Stop();
        }

    }
}
