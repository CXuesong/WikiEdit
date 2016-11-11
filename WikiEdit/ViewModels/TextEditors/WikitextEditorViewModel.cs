using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml.Linq;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using WikiEdit.Services;
using WikiEdit.Spark;
using WikiEdit.ViewModels.Primitives;

namespace WikiEdit.ViewModels.TextEditors
{
    public class WikitextEditorViewModel : TextEditorViewModel
    {
        private WikitextAutoCompletionHandler autoCompletionHandler;

        public WikitextEditorViewModel(SettingsService settingsService) : base(settingsService)
        {
            
        }

        /// <inheritdoc />
        public override void InitializeTextEditor(TextEditor textEditor)
        {
            base.InitializeTextEditor(textEditor);
            autoCompletionHandler = new WikitextAutoCompletionHandler(textEditor) {WikiSite = this.WikiSite};
        }

        protected override void OnRefreshDocumentOutline()
        {
            base.OnRefreshDocumentOutline();
            // Show document headings
            var parser = new WikitextParser();
            var documentText = TextBox.Text;
            Heading[] headings = null;
            if (!string.IsNullOrWhiteSpace(documentText))
            {
                var root = parser.Parse(documentText);
                headings = root.EnumDescendants().OfType<Heading>().ToArray();
            }
            Dispatcher.AutoInvoke(() =>
            {
                DocumentOutline.Clear();
                if (headings == null) return;
                var levelStack = new Stack<Tuple<Heading, DocumentOutlineItem>>();
                foreach (var h in headings)
                {
                    var outline = new DocumentOutlineItem
                    {
                        Text = string.Join(null, h.Inlines).Trim(),
                        OutlineContext = h
                    };
                    outline.DoubleClick += OutlineItem_DoubleClick;
                    while (levelStack.Count > 0)
                    {
                        var lastLevel = levelStack.Peek().Item1.Level;
                        if (lastLevel < h.Level)
                        {
                            // Append as child item.
                            levelStack.Peek().Item2.Children.Add(outline);
                            goto NEXT;
                        }
                        // Sibling or upper levels.
                        levelStack.Pop();
                    }
                    // levelStack.Count == 0
                    DocumentOutline.Add(outline);
                    levelStack.Push(Tuple.Create(h, outline));
                    NEXT:
                    ;
                }
            });
        }

        private void OutlineItem_DoubleClick(object sender, EventArgs e)
        {
            // Navigates to the selected node.
            var outline = (DocumentOutlineItem) sender;
            var span = (IWikitextSpanInfo) outline.OutlineContext;
            var line = (IWikitextLineInfo) outline.OutlineContext;
            if (span.HasSpanInfo)
                TextBox.Select(span.Start, span.Length);
            if (line.HasLineInfo())
                TextBox.ScrollTo(line.LineNumber, line.LinePosition);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == nameof(WikiSite))
            {
                if (autoCompletionHandler != null)
                    autoCompletionHandler.WikiSite = WikiSite;
            }
        }
    }
}
