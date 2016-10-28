using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// WikiSitesListView.xaml 的交互逻辑
    /// </summary>
    public partial class WikiSiteListView : UserControl
    {
        public WikiSiteListView()
        {
            InitializeComponent();
        }

        private void WikiSitesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as WikiSiteListViewModel;
            Debug.Assert(vm != null);
            var os = e.OriginalSource as DependencyObject;
            if (os == null) return;
            var source = WpfUtility.FindAncestor<ListViewItem>(os);
            if (source == null) return;
            vm.NotifyWikiSiteDoubleClick((WikiSiteViewModel) source.DataContext);
        }
    }
}
