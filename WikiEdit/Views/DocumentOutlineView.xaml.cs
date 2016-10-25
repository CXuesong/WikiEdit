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
using WikiEdit.ViewModels.Primitives;

namespace WikiEdit.Views
{
    /// <summary>
    /// DocumentOutlineView.xaml 的交互逻辑
    /// </summary>
    public partial class DocumentOutlineView : UserControl
    {
        public DocumentOutlineView()
        {
            InitializeComponent();
        }

        private void DocumentOutlineItem_OnPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dc = ((FrameworkElement)sender).DataContext as DocumentOutlineItem;
            if (dc == null) return;
            dc.NotifyDoubleClick();
            // We do not want to fire the same event on parent nodes
            e.Handled = true;
        }
    }
}
