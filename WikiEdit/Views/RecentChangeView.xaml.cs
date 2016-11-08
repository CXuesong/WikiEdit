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
using WikiEdit.ViewModels;

namespace WikiEdit.Views
{
    /// <summary>
    /// RecentChangeView.xaml 的交互逻辑
    /// </summary>
    public partial class RecentChangeView : UserControl
    {
        public RecentChangeView()
        {
            InitializeComponent();
        }

        private void RecentChangeView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var vm = DataContext as RecentChangeViewModel;
            if (vm != null) DetailRichTextBox.Document = vm.DetailDocument;
        }
    }
}
