using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DiffMatchPatch;
using ICSharpCode.AvalonEdit.Document;
using Unclassified.TxLib;

namespace WikiEdit.Controls
{
    /// <summary>
    /// 按照步骤 1a 或 1b 操作，然后执行步骤 2 以在 XAML 文件中使用此自定义控件。
    ///
    /// 步骤 1a) 在当前项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根 
    /// 元素中: 
    ///
    ///     xmlns:MyNamespace="clr-namespace:WikiEdit.Controls"
    ///
    ///
    /// 步骤 1b) 在其他项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根 
    /// 元素中: 
    ///
    ///     xmlns:MyNamespace="clr-namespace:WikiEdit.Controls;assembly=WikiEdit.Controls"
    ///
    /// 您还需要添加一个从 XAML 文件所在的项目到此项目的项目引用，
    /// 并重新生成以避免编译错误: 
    ///
    ///     在解决方案资源管理器中右击目标项目，然后依次单击
    ///     “添加引用”->“项目”->[浏览查找并选择此项目]
    ///
    ///
    /// 步骤 2)
    /// 继续操作并在 XAML 文件中使用控件。
    ///
    ///     <MyNamespace:TextDiffViewer/>
    ///
    /// </summary>
    [TemplatePart(Name = "PART_Presenter", Type = typeof(RichTextBox))]
    [TemplatePart(Name = "PART_PreviousDiffButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_NextDiffButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_SummaryLabel", Type = typeof(Label))]
    public class TextDiffViewer : Control
    {

        public static readonly DependencyProperty Text1Property =
            DependencyProperty.Register("Text1", typeof(string), typeof(TextDiffViewer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, TextChangedCallback));

        public static readonly DependencyProperty Text2Property =
            DependencyProperty.Register("Text2", typeof(string), typeof(TextDiffViewer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, TextChangedCallback));

        private FlowDocument _DiffDocument;
        private string _DiffSummary;

        private static void TextChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var v = (TextDiffViewer)sender;
            if (v._PresenterTextBox != null)
                v.InvalidateDiffDocumentAsync();
        }

        private bool updatingDiff;
        private async void InvalidateDiffDocumentAsync()
        {
            // Concurrency: We assume the invoker is on main thread.
            if (updatingDiff) return;
            updatingDiff = true;
            if (_SummaryLabel != null) _SummaryLabel.Content = Tx.T("please wait");
            try
            {
                RETRY:
                await Task.Delay(100);
                string t1 = Text1, t2 = Text2;
                string text1 = t1 ?? "", text2 = t2 ?? "";
                List<Diff> diffs = null;
                string summary = null;
                Action diffAction = () =>
                {
                    var diff = new diff_match_patch {Diff_Timeout = 10};
                    diffs = diff.diff_main(text1, text2);
                    diff.diff_cleanupSemantic(diffs);
                    int add=0, addl=0, remove=0, removel=0;
                    foreach (var d in diffs)
                    {
                        if (d.operation == Operation.INSERT)
                        {
                            add++;
                            addl += d.text.Length;
                        } else if (d.operation == Operation.DELETE)
                        {
                            remove++;
                            removel += d.text.Length;
                        }
                    }
                    summary = Tx.T("diff.summary", "length", Convert.ToString(text2.Length - text1.Length),
                        "add", add + "", "addlength", addl + "",
                        "remove", remove + "", "removelength", removel + "");
                };
                if (Math.Max(text1.Length, text2.Length) > 1024*1024)
                {
                    await Task.Factory.StartNew(diffAction, CancellationToken.None,
                        TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
                else
                {
                    diffAction();
                }
                if (t1 != Text1 || t2 != Text2)
                    goto RETRY;
                _DiffDocument = DiffToDocument(diffs);
                _DiffSummary = summary;
                if (_PresenterTextBox != null)
                    _PresenterTextBox.Document = _DiffDocument;
                if (_SummaryLabel != null)
                    _SummaryLabel.Content = _DiffSummary;
            }
            finally
            {
                updatingDiff = false;
            }
        }

        private FlowDocument DiffToDocument(IList<Diff> diffs)
        {
            Debug.Assert(diffs != null);
            var doc = new FlowDocument();
            Paragraph lastParagraph = null;
            var lfImageStyle = TryFindResource("LineFeedMarkerImage") as Style;
            var insertionStyle = TryFindResource("InsertionMarker") as Style;
            var deletionStyle = TryFindResource("DeletionMarker") as Style;
            var lineCounter1 = 0;
            var lineCounter2 = 0;
            Action<bool, bool> NextLine = (left, right) =>
            {
                var marker = "";
                if (left)
                {
                    lineCounter1++;
                    marker += $"{lineCounter1,6}";
                }
                else
                {
                    marker += "      ";
                }
                marker += " ";
                if (right)
                {
                    lineCounter2++;
                    marker += $"{lineCounter2,6}";
                }
                else
                {
                    marker += "      ";
                }
                lastParagraph = new Paragraph
                {
                    KeepWithNext = true,
                    TextIndent = -20,
                };
                marker += " ";
                lastParagraph.Inlines.Add(marker);
                doc.Blocks.Add(lastParagraph);
            };
            NextLine(true, true);
            foreach (var d in diffs)
            {
                Style style = null;
                switch (d.operation)
                {
                    case Operation.INSERT:
                        style = insertionStyle;
                        break;
                    case Operation.DELETE:
                        style = deletionStyle;
                        break;
                }
                var lines = d.text.Split('\n');
                if (lines.Length == 1)
                {
                    var run = new Run(d.text) {Style = style};
                    lastParagraph.Inlines.Add(run);
                }
                else
                {
                    foreach (var l in lines)
                    {
                        var span = new Span(new Run(l)) {Style = style};
                        var lfImage = new Image
                        {
                            Style = lfImageStyle,
                        };
                        var markerContainer = new InlineUIContainer(lfImage);
                        span.Inlines.Add(markerContainer);
                        lastParagraph.Inlines.Add(span);
                        NextLine(d.operation == Operation.EQUAL || d.operation == Operation.DELETE,
                            d.operation == Operation.EQUAL || d.operation == Operation.INSERT);
                    }
                }
            }
            return doc;
        }

        public string Text1
        {
            get { return (string)GetValue(Text1Property); }
            set { SetValue(Text1Property, value); }
        }

        public string Text2
        {
            get { return (string)GetValue(Text2Property); }
            set { SetValue(Text2Property, value); }
        }

        private RichTextBox _PresenterTextBox;
        private ContentControl _SummaryLabel;

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _PresenterTextBox = GetTemplateChild("PART_Presenter") as RichTextBox;
            _SummaryLabel = GetTemplateChild("PART_SummaryLabel") as ContentControl;
            if (_SummaryLabel != null)
                _SummaryLabel.Content = _DiffSummary;
            if (_PresenterTextBox != null && _DiffDocument == null)
                InvalidateDiffDocumentAsync();
        }

        static TextDiffViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextDiffViewer), new FrameworkPropertyMetadata(typeof(TextDiffViewer)));
        }
    }
}
