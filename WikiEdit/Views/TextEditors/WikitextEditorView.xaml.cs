using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using WikiEdit.Spark;
using WikiEdit.ViewModels.TextEditors;

namespace WikiEdit.Views.TextEditors
{
    /// <summary>
    /// WikitextEditorView.xaml 的交互逻辑
    /// </summary>
    public partial class WikitextEditorView : UserControl
    {
        public WikitextEditorView()
        {
            InitializeComponent();
            TextEditor.TextArea.TextView.LineTransformers.Add(WikitextColorizer.Default);
        }

        private void WikitextEditorView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var vm = e.NewValue as TextEditorViewModel;
            vm?.InitializeTextEditor(TextEditor);
        }
    }
}
