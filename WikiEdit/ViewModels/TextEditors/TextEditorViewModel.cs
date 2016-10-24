using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Prism.Mvvm;
using WikiEdit.Services;
using WikiEdit.ViewModels.Primitives;

namespace WikiEdit.ViewModels.TextEditors
{
    public abstract class TextEditorViewModel : BindableBase
    {
        private readonly SettingsService _SettingsService;

        protected Dispatcher Dispatcher { get; }

        public TextEditorViewModel(SettingsService settingsService)
        {
            if (settingsService == null) throw new ArgumentNullException(nameof(settingsService));
            _SettingsService = settingsService;
            Dispatcher = Dispatcher.CurrentDispatcher;
            TextDocument.Changed += TextDocument_Changed;
            LoadSettings();
        }

        public event EventHandler IsActiveChanged;

        public TextDocument TextDocument { get; } = new TextDocument();

        private IList<DocumentOutlineItem> _DocumentOutline;

        public IList<DocumentOutlineItem> DocumentOutline
        {
            get { return _DocumentOutline; }
            set
            {
                if (SetProperty(ref _DocumentOutline, value))
                {
                    InvalidateDocumentOutline(true);
                }
            }
        }

        private int _SelectionStart;

        public int SelectionStart
        {
            get { return _SelectionStart; }
            set { SetProperty(ref _SelectionStart, value); }
        }

        private int _SelectionLength;

        public int SelectionLength
        {
            get { return _SelectionLength; }
            set { SetProperty(ref _SelectionLength, value); }
        }

        /// <summary>
        /// Called when <see cref="DocumentOutline"/> needs to be refreshed.
        /// Note that this functing might not be invoked in the main thread.
        /// </summary>
        protected virtual void OnRefreshDocumentOutline()
        {

        }

        protected virtual void OnIsActiveChanged()
        {
            IsActiveChanged?.Invoke(this, EventArgs.Empty);
        }

        private RuntimeTextEditorLanguageSettings _LanguageSettings;

        public RuntimeTextEditorLanguageSettings LanguageSettings
        {
            get { return _LanguageSettings; }
            set { SetProperty(ref _LanguageSettings, value); }
        }

        private void LoadSettings()
        {
            LanguageSettings = _SettingsService.GetSettingsByLanguage("Wikitext");
        }

        public static readonly TimeSpan DocumentAnalysisDelay = TimeSpan.FromSeconds(5);

        private CancellationTokenSource impendingAnalysisCts;

        private void TextDocument_Changed(object sender, DocumentChangeEventArgs e)
        {
            InvalidateDocumentOutline();
        }

        public void InvalidateDocumentOutline()
        {
            InvalidateDocumentOutline(false);
        }

        public void InvalidateDocumentOutline(bool noDelay)
        {
            if (impendingAnalysisCts != null)
            {
                impendingAnalysisCts.Cancel();
                impendingAnalysisCts.Dispose();
                impendingAnalysisCts = null;
            }
            Action taskAction = OnRefreshDocumentOutline;
            // Run the analysis in background.
            if (noDelay)
            {
                var analysisTask = new Task(ct => taskAction(), TaskCreationOptions.LongRunning);
                analysisTask.Start(TaskScheduler.Default);
            }
            else
            {
                impendingAnalysisCts = new CancellationTokenSource();
                Task.Delay(DocumentAnalysisDelay, impendingAnalysisCts.Token)
                    .ContinueWith((t, s) => taskAction(), null, CancellationToken.None,
                        TaskContinuationOptions.LongRunning | TaskContinuationOptions.NotOnFaulted |
                        TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
            }
        }
    }
}
