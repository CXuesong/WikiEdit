using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Prism.Commands;
using Unclassified.TxLib;
using WikiClientLibrary;
using WikiEdit.Services;
using WikiEdit.ViewModels.TextEditors;

namespace WikiEdit.ViewModels.Documents
{
    public class PageEditorViewModel : DocumentViewModel
    {
        private readonly SettingsService _SettingsService;
        private readonly ITextEditorFactory _TextEditorFactory;

        public PageEditorViewModel(SettingsService settingsService,
            ITextEditorFactory textEditorFactory,
            WikiSiteViewModel wikiSite)
        {
            if (settingsService == null) throw new ArgumentNullException(nameof(settingsService));
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            _SettingsService = settingsService;
            _TextEditorFactory = textEditorFactory;
            WikiSite = wikiSite;
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
                var site = await WikiSite.GetSiteAsync();
                WikiPage = new Page(site, title);
                Title = WikiPage.Title;
                await RefetchPageAsync();
            }
            catch (Exception ex)
            {
                Status = Utility.GetExceptionMessage(ex);
            }
            IsBusy = false;
        }

        public WikiSiteViewModel WikiSite { get; }

        /// <summary>
        /// The page to be edited.
        /// </summary>
        public Page WikiPage { get; private set; }

        public override WikiSiteViewModel SiteContext => WikiSite;

        public override object DocumentContext => WikiPage;

        private async Task RefetchPageAsync()
        {
            Status = Tx.T("editor.fetching page", "title", WikiPage.Title);
            IsBusy = true;
            try
            {
                await WikiPage.RefreshAsync(PageQueryOptions.FetchContent);
                ReloadPageInformation();
                ReloadPageContent();
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
        }

        protected void ReloadPageContent()
        {
            // Switch content model, if necessary.
            if (_EditorContentModel != WikiPage.ContentModel)
            {
                var ls = _SettingsService.GetSettingsByWikiContentModel(WikiPage.ContentModel);
                if (string.IsNullOrEmpty(ls.LanguageName))
                {
                    TextEditor = null;
                }
                else
                {
                    TextEditor = _TextEditorFactory.CreateTextEditor(ls.LanguageName);
                    TextEditor.DocumentOutline = DocumentOutline;
                }
                _EditorContentModel = WikiPage.ContentModel;
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
                            WikiPage.Content = newContent;
                            Status = Tx.T("editor.submitting your edit");
                            IsBusy = true;
                            try
                            {
                                await WikiPage.UpdateContentAsync(EditorSummary, _EditorMinor, false,
                                    _EditorWatch == true
                                        ? AutoWatchBehavior.Watch
                                        : _EditorWatch == false
                                            ? AutoWatchBehavior.None
                                            : AutoWatchBehavior.Default);
                                Status = Tx.T("editor.submission success", "title", WikiPage.Title, "revid",
                                    Convert.ToString(WikiPage.LastRevisionId));
                                // refetch page information
                                Status = Tx.T("editor.fetching page", "title", WikiPage.Title);
                                await WikiPage.RefreshAsync();
                                ReloadPageInformation();
                            }
                            catch (Exception ex)
                            {
                                // Restore the content first.
                                WikiPage.Content = oldContent;
                                Status = Utility.GetExceptionMessage(ex);
                            }
                            finally
                            {
                                IsBusy = false;
                            }
                        },
                        () => !string.IsNullOrWhiteSpace(_EditorSummary));
                }
                return _SubmitEditCommand;
            }
        }

        #endregion
    }
}
