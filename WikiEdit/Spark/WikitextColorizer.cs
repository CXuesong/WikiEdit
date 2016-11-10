using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

namespace WikiEdit.Spark
{
    /// <summary>
    /// Colorize Wikitext by syntax.
    /// </summary>
    public class WikitextColorizer : DocumentColorizingTransformer
    {
        private double baseFontSize = 9;
        private Typeface boldTypeface;
        private Dictionary<TextView, ColorizerHelper> helperDict = new Dictionary<TextView, ColorizerHelper>();
        private int lineStart, lineEnd;

        public static readonly WikitextColorizer Default = new WikitextColorizer();

        /// <summary>
        /// Colors used to mark different levels of Template/ArgumentRef.
        /// </summary>
        private static readonly IList<Color> BracesPalette = new[]
        {
            "#00ffff", "#0099ff", "#6600ff", "#ff00ff",
        }.Select(WpfUtility.ColorFromString).ToArray();

        private static readonly IList<Brush> BracesBackgroundBrushes;

        private static readonly IList<TextDecorationCollection> _BracesNameDecorations = new List<TextDecorationCollection>();

        static WikitextColorizer()
        {
            BracesBackgroundBrushes = BracesPalette.Select(c =>
            {
                c.ScA = 0.1F;
                Brush brush = new SolidColorBrush(c);
                brush.Freeze();
                return brush;
            }).ToArray();
        }

        /// <summary>
        /// Generates multiple underlines for the title of the braces.
        /// </summary>
        private TextDecorationCollection GetBracesTitleDecorations(int level)
        {
            while (level >= _BracesNameDecorations.Count)
            {
                var i = _BracesNameDecorations.Count;
                var dec = i == 0
                    ? new TextDecorationCollection()
                    : _BracesNameDecorations[_BracesNameDecorations.Count - 1].CloneCurrentValue();
                var color = BracesPalette[i%BracesPalette.Count];
                Brush brush = new SolidColorBrush(color);
                brush.Freeze();
                var pen = new Pen(brush, 1) {DashStyle = DashStyles.Dash};
                pen.Freeze();
                dec.Add(new TextDecoration
                    {
                        Location = TextDecorationLocation.Underline,
                        Pen = pen,
                        PenOffset = -0.5*i,
                    }
                );
                dec.Freeze();
                _BracesNameDecorations.Add(dec);
            }
            return _BracesNameDecorations[level];
        }

        /// <inheritdoc />
        protected override void ColorizeLine(DocumentLine docLine)
        {
            var ast = helperDict[CurrentContext.TextView].AstRoot;
            if (ast == null) return;
            baseFontSize = CurrentContext.GlobalTextRunProperties.FontRenderingEmSize;
            var baseTypeface = CurrentContext.GlobalTextRunProperties.Typeface;
            if (boldTypeface == null || !Equals(boldTypeface.FontFamily, baseTypeface.FontFamily))
            {
                boldTypeface = new Typeface(baseTypeface.FontFamily, baseTypeface.Style, FontWeights.Bold,
                    baseTypeface.Stretch);
            }
            lineStart = docLine.Offset;
            lineEnd = docLine.EndOffset;
            foreach (var line in ast.EnumChildren())
            {
                ColorizeSegment(line, 0);
            }
        }

        private void SafeChangeLinePart(int startOffset, int endOffset, Action<VisualLineElement> action)
        {
            if (startOffset < lineStart) startOffset = lineStart;
            if (endOffset > lineEnd) endOffset = lineEnd;
            if (startOffset >= endOffset) return;
            ChangeLinePart(startOffset, endOffset, action);
        }

        // braceLevel : 0: out of braces; > 0: in braces
        private void ColorizeSegment(Node node, int braceLevel)
        {
            var si = (IWikitextSpanInfo)node;
            if (!si.HasSpanInfo) return;
            if (si.Start > lineEnd || si.Start + si.Length < lineStart) return;
            var heading = node as Heading;
            if (heading != null)
            {
                // Enlarge the line
                var fontSize = (Math.Max(0, 6 - heading.Level) * 0.4 + 1) * baseFontSize;
                SafeChangeLinePart(si.Start, si.Start + si.Length, e =>
                {
                    e.TextRunProperties.SetFontRenderingEmSize(fontSize);
                    e.TextRunProperties.SetFontHintingEmSize(fontSize);
                });
                if (heading.Inlines.Count > 0)
                {
                    // Make the markers bold
                    var contentRange = GetChildrenSpan(heading);
                    if (contentRange.Item1 < contentRange.Item2)
                    {
                        SafeChangeLinePart(si.Start, contentRange.Item1, e =>
                        {
                            e.TextRunProperties.SetTypeface(boldTypeface);
                        });
                        SafeChangeLinePart(contentRange.Item2, si.Start + si.Length, e =>
                        {
                            e.TextRunProperties.SetTypeface(boldTypeface);
                        });
                    }
                }
                goto PROCESS_CHILDREN;
            }
            var template = node as Template;
            var argref = node as ArgumentReference;
            if (template != null || argref != null)
            {
                var brush = BracesBackgroundBrushes[braceLevel%BracesPalette.Count];
                var dec = GetBracesTitleDecorations(braceLevel%BracesPalette.Count);
                // Colorfy the block
                braceLevel += 1;
                SafeChangeLinePart(si.Start, si.Start + si.Length, e =>
                {
                    e.TextRunProperties.SetBackgroundBrush(brush);
                });
                // Emphasize the title
                var title = template != null ? (IWikitextSpanInfo) template.Name : argref.Name;
                Debug.Assert(title.HasSpanInfo);
                SafeChangeLinePart(title.Start, title.Start + title.Length, e =>
                {
                    e.TextRunProperties.SetTextDecorations(dec);
                });
                goto PROCESS_CHILDREN;
            }
            var comment = node as Comment;
            if (comment != null)
            {
                SafeChangeLinePart(si.Start, si.Start + si.Length, e =>
                {
                    e.TextRunProperties.SetForegroundBrush(Brushes.ForestGreen);
                });
                goto PROCESS_CHILDREN;
            }
            PROCESS_CHILDREN:
            foreach (var child in node.EnumChildren())
            {
                ColorizeSegment(child, braceLevel);
            }
        }

        private Tuple<int, int> GetChildrenSpan(Heading heading)
        {
            Debug.Assert(heading != null);
            IWikitextSpanInfo si;
            if (heading.Inlines.Count == 0)
            {
                si = heading;
                Debug.Assert(si.HasSpanInfo);
                return Tuple.Create(si.Start + si.Length/2, si.Start + si.Length/2);
            }
            si = heading.Inlines.FirstNode;
            Debug.Assert(si.HasSpanInfo);
            var start = si.Start;
            si = heading.Inlines.LastNode;
            Debug.Assert(si.HasSpanInfo);
            return Tuple.Create(start, si.Start + si.Length);
        }

        /// <inheritdoc />
        protected override void OnAddToTextView(TextView textView)
        {
            base.OnAddToTextView(textView);
            var helper = new ColorizerHelper(textView);
            helperDict.Add(textView, helper);
            helper.InvalidateAst();
        }

        /// <inheritdoc />
        protected override void OnRemoveFromTextView(TextView textView)
        {
            base.OnRemoveFromTextView(textView);
            helperDict.Remove(textView);
        }

        /// <summary>
        /// Per TextView colorizer.
        /// </summary>
        private class ColorizerHelper
        {
            private volatile bool documentAstInvalidated = false;

            private TextDocument currentDocument;

            public TextView TextView { get; }

            public Wikitext AstRoot { get; private set; }

            public ColorizerHelper(TextView textView)
            {
                if (textView == null) throw new ArgumentNullException(nameof(textView));
                TextView = textView;
                TextView_DocumentChanged(this, EventArgs.Empty);
            }

            private void TextView_DocumentChanged(object sender, EventArgs e)
            {
                if (currentDocument != null)
                    currentDocument.TextChanged -= CurrentDocument_TextChanged;
                currentDocument = TextView.Document;
                if (currentDocument != null)
                    currentDocument.TextChanged += CurrentDocument_TextChanged;
                InvalidateAst();
            }

            private void CurrentDocument_TextChanged(object sender, EventArgs e)
            {
                InvalidateAst();
            }

            public void InvalidateAst()
            {
                if (currentDocument == null) return;
                    if (!documentAstInvalidated)
                {
                    if (currentDocument.TextLength < 10 * 1024)
                    {
                        Parse();
                    }
                    else
                    {
                        // background
                        documentAstInvalidated = true;
                        // 1s for ~200 KB
                        var delay = Math.Min(currentDocument.TextLength/200, 5000);
                        if (AstRoot == null)
                        {
                            // We want a fast initialization.
                            delay = 10;
                        }
                        Task.Delay(delay).ContinueWith((t, s) => Parse(), null, CancellationToken.None,
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning,
                            TaskScheduler.Default);
                    }
                }
            }

            private void Parse()
            {
                if (TextView != null)
                {
                    var parser = new WikitextParser();
                    var text = TextView.Dispatcher.AutoInvoke(() => TextView.Document.Text);
                    var sw = Stopwatch.StartNew();
                    var ast = parser.Parse(text);
                    Trace.WriteLine("Parsed " + text.Length + " chars in " + sw.Elapsed);
                    documentAstInvalidated = false;
                    TextView.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        AstRoot = ast;
                        TextView.Redraw();
                    }));
                }
                documentAstInvalidated = false;
            }
        }
    }
}