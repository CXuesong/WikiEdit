﻿using System;
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

namespace WikiEdit.ViewModels.Documents
{
    public class PageEditorViewModel : DocumentViewModel
    {
        private readonly SettingsService _SettingsService;

        public PageEditorViewModel(SettingsService settingsService, WikiSiteViewModel wikiSite, Page wikiPage)
        {
            if (settingsService == null) throw new ArgumentNullException(nameof(settingsService));
            if (wikiPage == null) throw new ArgumentNullException(nameof(wikiPage));
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            Debug.Assert(wikiSite.GetSiteAsync().Result == wikiPage.Site);
            _SettingsService = settingsService;
            WikiSite = wikiSite;
            WikiPage = wikiPage;
            Title = WikiPage.Title;
            LoadSettings();
            LoadPageAsync().Forget();
        }

        public WikiSiteViewModel WikiSite { get; }

        /// <summary>
        /// The page to be edited.
        /// </summary>
        public Page WikiPage { get; }

        public override WikiSiteViewModel SiteContext => WikiSite;

        public override object DocumentContext => WikiPage;

        private void LoadSettings()
        {
            LanguageSettings = _SettingsService.GetSettingsByWikiContentModel(WikiPage.ContentModel);
            PageContentHighlightingDefinition = HighlightingManager.Instance.GetDefinition(LanguageSettings.LanguageName);
        }

        private async Task LoadPageAsync()
        {
            Status = Tx.T("editor.fetching page", "title", WikiPage.Title);
            IsBusy = true;
            try
            {
                await WikiPage.RefreshAsync(PageQueryOptions.FetchContent);
                PageContentDocument.Text = WikiPage.Content;
                LoadSettings();
                Status = null;
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region Editor

        private RuntimeTextEditorLanguageSettings _LanguageSettings;

        public RuntimeTextEditorLanguageSettings LanguageSettings
        {
            get { return _LanguageSettings; }
            set { SetProperty(ref _LanguageSettings, value); }
        }


        private IHighlightingDefinition _PageContentHighlightingDefinition;

        public IHighlightingDefinition PageContentHighlightingDefinition
        {
            get { return _PageContentHighlightingDefinition; }
            set { SetProperty(ref _PageContentHighlightingDefinition, value); }
        }

        public TextDocument PageContentDocument { get; } = new TextDocument();

        #endregion

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
                            if (string.IsNullOrWhiteSpace(EditorSummary) && WikiPage.Content != PageContentDocument.Text)
                                return;
                            WikiPage.Content = PageContentDocument.Text;
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
                                await WikiPage.RefreshAsync();
                            }
                            catch (Exception ex)
                            {
                                Status = ex.Message;
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