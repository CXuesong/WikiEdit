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
    public class TextDiffViewer : Control
    {

        public static readonly DependencyProperty Text1Property =
            DependencyProperty.Register("Text1", typeof(string), typeof(TextDiffViewer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, TextChangedCallback));

        public static readonly DependencyProperty Text2Property =
            DependencyProperty.Register("Text2", typeof(string), typeof(TextDiffViewer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, TextChangedCallback));

        private static readonly TextDecorationCollection deletedSegmentDecoration;
        private FlowDocument diffDocument;

        private static void TextChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var v = (TextDiffViewer)sender;
            v.InvalidateDiffDocumentAsync();
        }

        private bool updatingDiff;
        private async void InvalidateDiffDocumentAsync()
        {
            // Concurrency: We assume the invoker is on main thread.
            if (updatingDiff) return;
            updatingDiff = true;
            try
            {
                RETRY:
                await Task.Yield();
                var diff = new diff_match_patch();
                var text1 = Text1;
                var text2 = Text2;
                List<Diff> diffs = null;
                Action diffAction = () =>
                {
                    diffs = diff.diff_main(Text1 ?? "", Text2 ?? "");
                    diff.diff_cleanupSemantic(diffs);
                };
                if (Math.Max(text1?.Length ?? 0, text2?.Length ?? 0) > 1024*1024)
                {
                    await Task.Factory.StartNew(diffAction, CancellationToken.None,
                        TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
                else
                {
                    diffAction();
                }
                if (text1 != Text1 || text2 != Text2)
                    goto RETRY;
                diffDocument = DiffToDocument(diffs);
                if (_PresenterTextBox != null)
                    _PresenterTextBox.Document = diffDocument;
            }
            finally
            {
                updatingDiff = false;
            }
        }

        private static FlowDocument DiffToDocument(IList<Diff> diffs)
        {
            Debug.Assert(diffs != null);
            var doc = new FlowDocument();
            var lastParagraph = new Paragraph();
            foreach (var d in diffs)
            {
                var run = new Run(d.text);
                switch (d.operation)
                {
                    case Operation.INSERT:
                        run.Foreground = Brushes.Black;
                        run.Background = Brushes.PaleGreen;
                        break;
                    case Operation.DELETE:
                        run.Foreground = Brushes.Black;
                        run.TextDecorations = deletedSegmentDecoration;
                        break;
                }
                lastParagraph.Inlines.Add(run);
            }
            doc.Blocks.Add(lastParagraph);
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

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _PresenterTextBox = GetTemplateChild("PART_Presenter") as RichTextBox;
            if (_PresenterTextBox != null && diffDocument != null)
                _PresenterTextBox.Document = diffDocument;
        }

        static TextDiffViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextDiffViewer), new FrameworkPropertyMetadata(typeof(TextDiffViewer)));
            deletedSegmentDecoration = new TextDecorationCollection
            {
                new TextDecoration {Location = TextDecorationLocation.Strikethrough, Pen = new Pen(Brushes.Crimson, 1)}
            };
            deletedSegmentDecoration.Freeze();
        }
    }
}
