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

namespace WikiEdit.Views.Documents
{
    /// <summary>
    /// PageEditorView.xaml 的交互逻辑
    /// </summary>
    public partial class PageEditorView : UserControl
    {
        public PageEditorView()
        {
            InitializeComponent();
        }

        private void LastRevisionCommentButton_OnClick(object sender, RoutedEventArgs e)
        {
            LastRevisionCommentPopup.IsOpen = true;
        }
    }
}
