using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Unclassified.TxLib;
using WikiClientLibrary;
using WikiEdit.Models;
using WikiEdit.ViewModels;

namespace WikiEdit.Spark
{
    /// <summary>
    /// Wikitext auto completion handler.
    /// </summary>
    class WikitextAutoCompletionHandler
    {
        public const int MaxWikiLinkAutoCompletionLength = 300;

        private readonly TextEditor _TextEditor;
        private volatile int _CurrentAutoCompletionTaskToken;
        private CompletionWindow _CompletionWindow;
        private static readonly Regex ImplicitAutoCompletionTriggerMatcher = new Regex(@"[^\]\|\}]");
        private static readonly Regex AutoCompletionPrefixMatcher = new Regex(@"((?<D>\[\[|\{\{)(?<T>[^\]\|\}]*))$", RegexOptions.RightToLeft);
        private static readonly Regex WikiLinkSuffixMatcher = new Regex(@"[\|\]]");
        private static readonly Regex TemplateSuffixMatcher = new Regex(@"[\|\}]");
        private Regex AutoCompletionSuffixMatcher;

        public WikitextAutoCompletionHandler(TextEditor textEditor)
        {
            if (textEditor == null) throw new ArgumentNullException(nameof(textEditor));
            _TextEditor = textEditor;
            _TextEditor.TextArea.TextEntering += TextArea_TextEntering;
            _TextEditor.TextArea.TextEntered += TextArea_TextEntered;
            _TextEditor.TextArea.KeyDown += TextArea_KeyDown;
        }

        /// <summary>
        /// Optional, uses this to provide full completion for page titles.
        /// </summary>
        public WikiSiteViewModel WikiSite { get; set; }

        private void TextArea_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && e.SystemKey == Key.Right)
                ShowAutoCompletionBoxAsync(true).Forget();
        }

        private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text != "" && _CompletionWindow != null)
            {
                Debug.Assert(AutoCompletionSuffixMatcher != null);
                // Accept current selection when the suffix has been typed.
                if (AutoCompletionSuffixMatcher.IsMatch(e.Text))
                {
                    _CompletionWindow.CompletionList.RequestInsertion(EventArgs.Empty);
                }
            }
        }

        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (ImplicitAutoCompletionTriggerMatcher.IsMatch(e.Text))
                ShowAutoCompletionBoxAsync(false).Forget();
        }

        private Task ShowAutoCompletionBoxAsync(bool force)
        {
#pragma warning disable 420
            var token = Interlocked.Increment(ref _CurrentAutoCompletionTaskToken);
#pragma warning restore 420
            return ShowAutoCompletionBoxAsyncCore(token, force);
        }

        private async Task ShowAutoCompletionBoxAsyncCore(int token, bool force)
        {
            if (WikiSite == null) return;
            await Task.Delay(100);
            if (token != _CurrentAutoCompletionTaskToken) return;
            if (_TextEditor.SelectionStart >= 2)
            {
                var currentLine = _TextEditor.Document.GetLineByOffset(_TextEditor.SelectionStart);
                var caretPos = _TextEditor.SelectionStart - currentLine.Offset;
                var currentText = _TextEditor.Document.GetText(currentLine);
                var match = AutoCompletionPrefixMatcher.Match(currentText, 0, caretPos);
                if (!match.Success) return;
                var prefix = match.Groups["D"].Value;
                var expression = match.Groups["T"].Value;
                if (string.IsNullOrWhiteSpace(expression)) return;
                if (!force && expression.Length > MaxWikiLinkAutoCompletionLength) return;
                _CompletionWindow = new CompletionWindow(_TextEditor.TextArea);
                _CompletionWindow.Closed += (_, e) => _CompletionWindow = null;
                var acitems = _CompletionWindow.CompletionList.CompletionData;
                acitems.Clear();
                switch (prefix) {
                    case "[[":
                        // Do not inverse the order of the following 2 statements.
                        AutoCompletionSuffixMatcher = WikiLinkSuffixMatcher;
                        acitems.AddRange(await GetPageTitleCompletionDataAsync(expression));
                        break;
                    case "{{":
                        AutoCompletionSuffixMatcher = TemplateSuffixMatcher;
                        acitems.AddRange(await GetPageTitleCompletionDataAsync(expression));
                        break;
                    default:
                        return;
                }
                if (token != _CurrentAutoCompletionTaskToken || _CompletionWindow == null)
                {
                    _CompletionWindow = null;
                    return;
                }
                // Show completion window first.
                _CompletionWindow.Show();
                // Then fetch detailed page information.
                var detailed = await GetDetailedPageTitleCompletionDataAsync(
                    acitems.OfType<BasicWikiPageCompletionData>().Select(d => d.CanonicalTitle));
                if (token != _CurrentAutoCompletionTaskToken) return;
                if (_CompletionWindow != null)
                {
                    acitems.Clear();
                    acitems.AddRange(detailed);
                }
            }
        }
        private async Task<IEnumerable<ICompletionData>> GetPageTitleCompletionDataAsync(string expression)
        {
            Debug.Assert(WikiSite != null);
            var site = await WikiSite.GetSiteAsync();
            var list = await WikiSite.GetAutoCompletionItemsAsync(expression);
            // Generate auto completion for all namespace aliases.
            return list.SelectMany(item =>
            {
                var link = WikiLink.Parse(site, item.Title);
                return link.Namespace.Aliases.Concat(new[] {link.Namespace.CanonicalName, link.Namespace.CustomName})
                    .Distinct().Where(n => n != null)
                    .Select(n => new BasicWikiPageCompletionData((n == "" ? "" : (n + ":")) + link.Title,
                        item.Title, item.Description));
            });
        }

        private async Task<IEnumerable<ICompletionData>> GetDetailedPageTitleCompletionDataAsync(IEnumerable<string> pageTitles)
        {
            Debug.Assert(WikiSite != null);
            var site = await WikiSite.GetSiteAsync();
            var list = await WikiSite.GetPageSummaryAsync(pageTitles);
            // Generate auto completion for all namespace aliases.
            return list.SelectMany(item =>
            {
                var link = WikiLink.Parse(site, item.Title);
                return link.Namespace.Aliases.Concat(new[] {link.Namespace.CanonicalName, link.Namespace.CustomName})
                    .Distinct().Where(n => n != null)
                    .Select(n => new DetailedWikiPageCompletionData((n == "" ? "" : (n + ":")) + link.Title,
                        item));
            });
        }

        private abstract class WikiPageCompletionData : ICompletionData
        {
            public WikiPageCompletionData(string title, string canonicalTitle)
            {
                if (title == null) throw new ArgumentNullException(nameof(title));
                if (canonicalTitle == null) throw new ArgumentNullException(nameof(canonicalTitle));
                Title = title;
                CanonicalTitle = canonicalTitle;
                Image = new BitmapImage(new Uri("/WikiEdit;component/Images/Document.png", UriKind.Relative));
                Priority = 0;
            }

            public string Title { get; }

            public string CanonicalTitle { get; }

            /// <inheritdoc />
            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                var currentLine = textArea.Document.GetLineByOffset(textArea.Caret.Offset);
                var caretPos = textArea.Caret.Offset - currentLine.Offset;
                var currentText = textArea.Document.GetText(currentLine);
                var match = AutoCompletionPrefixMatcher.Match(currentText, 0, caretPos);
                if (!match.Success) return;
                textArea.Document.Replace(currentLine.Offset + match.Groups["T"].Index, match.Groups["T"].Length,
                    CanonicalTitle);
            }

            /// <inheritdoc />
            public virtual ImageSource Image { get; }

            /// <inheritdoc />
            public abstract string Text { get; }

            /// <inheritdoc />
            public abstract object Content { get; }

            /// <inheritdoc />
            public abstract object Description { get; }

            /// <inheritdoc />
            public virtual double Priority { get; }
        }

        private class BasicWikiPageCompletionData : WikiPageCompletionData
        {
            private readonly string _Description;

            public BasicWikiPageCompletionData(string title, string canonicalTitle, string description)
                : base(title, canonicalTitle)
            {
                _Description = canonicalTitle + "\n\n" + description;
            }

            /// <inheritdoc />
            public override string Text => Title;

            /// <inheritdoc />
            public override object Content => Title;

            /// <inheritdoc />
            public override object Description => _Description;
        }

        private class DetailedWikiPageCompletionData : WikiPageCompletionData
        {
            private readonly string _Description;

            public DetailedWikiPageCompletionData(string title, PageSummary pageSummary)
                : base(title, pageSummary.Title)
            {
                if (pageSummary == null) throw new ArgumentNullException(nameof(pageSummary));
                _Description = pageSummary.Title + "\n\n" + pageSummary.Description + "\n\n"
                               + Tx.TC("page.content length") + Tx.DataSize(pageSummary.ContentLength) + "\n"
                               + Tx.TC("page.last revision by") + pageSummary.LastRevisionUser + " " +
                               Tx.Time(pageSummary.LastRevisionTime, TxTime.YearMonthDay | TxTime.HourMinuteSecond);
                if (pageSummary.RedirectPath != null && pageSummary.RedirectPath.Count > 0)
                    _Description += "\n" + Tx.TC("page.redirect") + string.Join("→", pageSummary.RedirectPath);
            }

            /// <inheritdoc />
            public override string Text => Title;

            /// <inheritdoc />
            public override object Content => Title;

            /// <inheritdoc />
            public override object Description => _Description;
        }
    }
}
