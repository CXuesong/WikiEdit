﻿using System;
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

namespace WikiEdit.Views
{
    /// <summary>
    /// AccountProfileView.xaml 的交互逻辑
    /// </summary>
    public partial class AccountProfileView : UserControl
    {
        public AccountProfileView()
        {
            InitializeComponent();
            VisualStateManager.GoToState(this, "LoginCollapsed", false);
        }

        private void LoginView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            VisualStateManager.GoToState(this, LoginView.DataContext != null ? "LoginExpanded" : "LoginCollapsed", true);
        }
    }
}
