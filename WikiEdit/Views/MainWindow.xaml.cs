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
using Fluent;
using Microsoft.Practices.Unity;
using WikiEdit.ViewModels;
using WikiEdit.ViewModels.Documents;
using Xceed.Wpf.AvalonDock.Layout;

namespace WikiEdit.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {

        [Dependency]
        internal MainWindowViewModel ViewModel
        {
            get { return (MainWindowViewModel) DataContext; }
            set { DataContext = value; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// Select Style for docking tabs.
    /// </summary>
    internal class AvalonDockStyleSelector : StyleSelector
    {
        public Style DocumentStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is DocumentViewModel)
                return DocumentStyle;
            return base.SelectStyle(item, container);
        }
    }

    internal class AvalonDockDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LayoutDocumentTemplate { get; set; }

        /// <inheritdoc />
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is LayoutDocument)
                return LayoutDocumentTemplate;
            return base.SelectTemplate(item, container);
        }
    }
}
