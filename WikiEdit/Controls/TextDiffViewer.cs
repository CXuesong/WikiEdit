using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
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
    [TemplatePart(Name = "PART_Presenter", Type = typeof(ItemsControl))]
    [TemplatePart(Name = "PART_PreviousDiffButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_NextDiffButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_SummaryTextBlock", Type = typeof(TextBlock))]
    public class TextDiffViewer : Control
    {

        public static readonly DependencyProperty Text1Property =
            DependencyProperty.Register("Text1", typeof(string), typeof(TextDiffViewer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, TextChangedCallback));

        public static readonly DependencyProperty Text2Property =
            DependencyProperty.Register("Text2", typeof(string), typeof(TextDiffViewer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, TextChangedCallback));

        private List<DiffLine> diffLines;
        private ListCollectionView diffLinesView;
        private string _DiffSummary;

        private static void TextChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var v = (TextDiffViewer)sender;
            v.InvalidateDiffDocument();
        }

        private bool updatingDiff;
        private bool diffInvalidated;

        private void InvalidateDiffDocument()
        {
            diffInvalidated = true;
            UpdateDiffDocumentAsync();
        }

        private async void UpdateDiffDocumentAsync()
        {
            if (!IsLoaded) return;
            if (!diffInvalidated) return;
            // Concurrency: We assume the invoker is on main thread.
            if (updatingDiff) return;
            updatingDiff = true;
            try
            {
                if (summaryTextBlock != null)
                    summaryTextBlock.Text = Tx.T("please wait");
                RETRY:
                await Task.Delay(10);
                string t1 = Text1, t2 = Text2;
                string text1 = t1 ?? "", text2 = t2 ?? "";
                Action diffAction = () =>
                {
                    // Diff
                    var diff = new diff_match_patch {Diff_Timeout = 10};
                    var diffs = diff.diff_main(text1, text2);
                    diff.diff_cleanupSemantic(diffs);
                    int add = 0, addl = 0, remove = 0, removel = 0;
                    foreach (var d in diffs)
                    {
                        if (d.operation == Operation.INSERT)
                        {
                            add++;
                            addl += d.text.Length;
                        }
                        else if (d.operation == Operation.DELETE)
                        {
                            remove++;
                            removel += d.text.Length;
                        }
                    }
                    // 
                    _DiffSummary = Tx.T("diff.summary", "length", (text2.Length - text1.Length).ToString("+#;-#;0"),
                        "add", add + "", "addlength", addl + "",
                        "remove", remove + "", "removelength", removel + "");
                    diffLines = DiffToLines(diffs);
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
                diffLinesView = new ListCollectionView(diffLines);
                // Ensure we've compared the latest text
                if (t1 != Text1 || t2 != Text2)
                    goto RETRY;
                diffInvalidated = false;
                if (summaryTextBlock != null)
                    summaryTextBlock.Text = _DiffSummary;
                if (presenter != null)
                {
                    presenter.ItemsSource = diffLinesView;
                    // Move to the first difference
                    for (int l = 0; l < diffLinesView.Count; l++)
                    {
                        var diff = (DiffLine)diffLinesView.GetItemAt(l);
                        if (diff.Segments.Any(s => s.Style != DiffSegmentStyle.Normal))
                        {
                            var l1 = l;
                            await Dispatcher.BeginInvoke(DispatcherPriority.Input,
                                (Action) (() => SelectLineByIndex(l1)));
                            break;
                        }
                    }
                }
            }
            finally
            {
                updatingDiff = false;
            }
        }

        private static List<DiffLine> DiffToLines(IList<Diff> diffs)
        {
            Debug.Assert(diffs != null);
            var lines = new List<DiffLine>();
            DiffLine currentLine = null;
            var lineCounter1 = 0;
            var lineCounter2 = 0;
            Action<bool, bool> NextLine = (left, right) =>
            {
                int? line1 = null, line2 = null;
                if (left)
                {
                    lineCounter1++;
                    line1 = lineCounter1;
                }
                if (right)
                {
                    lineCounter2++;
                    line2 = lineCounter2;
                }
                currentLine = new DiffLine(line1, line2);
                lines.Add(currentLine);
            };
            NextLine(true, true);
            foreach (var d in diffs)
            {
                var style = DiffSegmentStyle.Normal;
                switch (d.operation)
                {
                    case Operation.INSERT:
                        style = DiffSegmentStyle.Insertion;
                        break;
                    case Operation.DELETE:
                        style = DiffSegmentStyle.Deletion;
                        break;
                }
                var segmentLines = d.text.Split('\n');
                for (int i = 0; i < segmentLines.Length; i++)
                {
                    var l = segmentLines[i];
                    currentLine.Segments.Add(new DiffSegment(l, style));
                    if (i < segmentLines.Length - 1)
                    {
                        NextLine(d.operation == Operation.EQUAL || d.operation == Operation.DELETE,
                            d.operation == Operation.EQUAL || d.operation == Operation.INSERT);
                    }
                }
            }
            return lines;
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

        private ListBox presenter;
        private TextBlock summaryTextBlock;
        private Button previousDiffButton, nextDiffButton;

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            presenter = GetTemplateChild("PART_Presenter") as ListBox;
            summaryTextBlock = GetTemplateChild("PART_SummaryTextBlock") as TextBlock;
            if (previousDiffButton != null)
                previousDiffButton.Click -= PreviousDiffButton_Click;
            if (nextDiffButton != null)
                nextDiffButton.Click -= NextDiffButton_Click;
            previousDiffButton = GetTemplateChild("PART_PreviousDiffButton") as Button;
            nextDiffButton = GetTemplateChild("PART_NextDiffButton") as Button;
            if (previousDiffButton != null)
                previousDiffButton.Click += PreviousDiffButton_Click;
            if (nextDiffButton != null)
                nextDiffButton.Click += NextDiffButton_Click;
            if (summaryTextBlock != null)
                summaryTextBlock.Text = _DiffSummary;
            if (presenter != null)
            {
                if (diffLinesView == null)
                    UpdateDiffDocumentAsync();
                else
                    presenter.ItemsSource = diffLinesView;
            }
        }

        private void TextDiffViewer_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDiffDocumentAsync();
        }

        private void NextDiffButton_Click(object sender, RoutedEventArgs e)
        {
            if (diffLinesView == null) return;
            for (var l = diffLinesView.CurrentPosition + 1; l < diffLinesView.Count; l++)
            {
                var diff = (DiffLine)diffLinesView.GetItemAt(l);
                if (diff.Segments.Any(s => s.Style != DiffSegmentStyle.Normal))
                {
                    SelectLineByIndex(l);
                    return;
                }
            }
        }

        private void PreviousDiffButton_Click(object sender, RoutedEventArgs e)
        {
            if (diffLinesView == null) return;
            for (var l = diffLinesView.CurrentPosition - 1; l >= 0; l--)
            {
                var diff = (DiffLine) diffLinesView.GetItemAt(l);
                if (diff.Segments.Any(s => s.Style != DiffSegmentStyle.Normal))
                {
                    SelectLineByIndex(l);
                    return;
                }
            }
        }

        private void SelectLineByIndex(int index)
        {
            if (diffLinesView == null) return;
            diffLinesView.MoveCurrentToPosition(index);
            if (presenter == null) return;
            var item = diffLinesView.GetItemAt(index);
            presenter.ScrollIntoView(item);
        }

        static TextDiffViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextDiffViewer), new FrameworkPropertyMetadata(typeof(TextDiffViewer)));
        }

        public TextDiffViewer()
        {
            Loaded += TextDiffViewer_Loaded;
        }
    }

    [TemplatePart(Name = "PART_LineIndicator1", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_LineIndicator2", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_LineContent", Type = typeof(TextBlock))]
    public class DiffLinePresenter : Control
    {
        static DiffLinePresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DiffLinePresenter),
                new FrameworkPropertyMetadata(typeof(DiffLinePresenter)));
        }

        public DiffLinePresenter()
        {
            this.DataContextChanged += DiffContentPresenter_DataContextChanged;
        }

        private TextBlock lineIndicator1, lineIndicator2, lineContent;
        private Style _InsertionStyle;
        private Style _DeletionStyle;

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _InsertionStyle = TryFindResource("InsertionMarker") as Style;
            _DeletionStyle = TryFindResource("DeletionMarker") as Style;
            lineIndicator1 = GetTemplateChild("PART_LineIndicator1") as TextBlock;
            lineIndicator2 = GetTemplateChild("PART_LineIndicator2") as TextBlock;
            lineContent = GetTemplateChild("PART_LineContent") as TextBlock;
            UpdatePresenter();
        }

        private void UpdatePresenter()
        {
            var context = DataContext as DiffLine;
            if (context == null)
            {
                if (lineIndicator1 != null) lineIndicator1.Text = null;
                if (lineIndicator2 != null) lineIndicator2.Text = null;
                if (lineContent != null) lineContent.Text = null;
                return;
            }
            if (lineIndicator1 != null) lineIndicator1.Text = Convert.ToString(context.LineNumber1);
            if (lineIndicator2 != null) lineIndicator2.Text = Convert.ToString(context.LineNumber2);
            if (lineContent != null)
            {
                lineContent.Inlines.Clear();
                if (context.Segments.Count > 0)
                {
                    foreach (var segment in context.Segments)
                    {
                        var run = SegmentToRun(segment.Text, segment.Style);
                        lineContent.Inlines.Add(run);
                    }
                    var lastSegmentStyle = context.Segments[context.Segments.Count - 1].Style;
                    lineContent.Inlines.Add(SegmentToRun("¶", lastSegmentStyle));
                }
            }
        }

        private Run SegmentToRun(string text, DiffSegmentStyle style)
        {
            var run = new Run(text);
            switch (style)
            {
                case DiffSegmentStyle.Insertion:
                    run.Style = _InsertionStyle;
                    break;
                case DiffSegmentStyle.Deletion:
                    run.Style = _DeletionStyle;
                    break;
            }
            return run;
        }

        private void DiffContentPresenter_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdatePresenter();
        }
    }

    internal class DiffLine
    {
        public DiffLine(int? lineNumber1, int? lineNumber2)
        {
            LineNumber1 = lineNumber1;
            LineNumber2 = lineNumber2;
        }

        public int? LineNumber1 { get; }

        public int? LineNumber2 { get; }

        public List<DiffSegment> Segments { get; } = new List<DiffSegment>();

    }

    internal class DiffSegment
    {
        public DiffSegment(string text, DiffSegmentStyle style)
        {
            Text = text;
            Style = style;
        }

        public string Text { get; }

        public DiffSegmentStyle Style { get; }
    }

    internal enum DiffSegmentStyle
    {
        Normal = 0,
        Insertion,
        Deletion
    }
}
