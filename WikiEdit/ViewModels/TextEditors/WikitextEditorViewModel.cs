using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml.Linq;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using WikiEdit.Services;
using WikiEdit.ViewModels.Primitives;

namespace WikiEdit.ViewModels.TextEditors
{
    public class WikitextEditorViewModel : TextEditorViewModel
    {
        public WikitextEditorViewModel(SettingsService settingsService) : base(settingsService)
        {
            
        }

        protected override void OnRefreshDocumentOutline()
        {
            base.OnRefreshDocumentOutline();
            var parser = new WikitextParser();
            string documentText = null;
            Dispatcher.AutoInvoke(() => documentText = TextDocument.Text);
            var root = parser.Parse(documentText);
            var headings = root.EnumDescendants().OfType<Heading>();
            Dispatcher.AutoInvoke(() =>
            {
                DocumentOutline.Clear();
                var levelStack = new Stack<Tuple<Heading, DocumentOutlineItem>>();
                foreach (var h in headings)
                {
                    var outline = new DocumentOutlineItem {Text = string.Join(null, h.Inlines), OutlineContext = h};
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
            if (span.HasSpanInfo)
            {
                SelectionStart = span.Start;
                SelectionLength = span.Length;
            }
        }
    }
}
