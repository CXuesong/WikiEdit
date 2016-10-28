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

namespace WikiEdit.Controls
{
    /// <summary>
    /// A tip label that can be "closed" by user.
    /// </summary>
    [TemplatePart(Name = "PART_CloseButton", Type = typeof(Button))]
    public class AlertLabel : Control
    {
        public static readonly DependencyProperty IsDismissibleProperty =
            DependencyProperty.Register("IsDismissible", typeof(bool), typeof(AlertLabel),
                new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(AlertLabel),
                new FrameworkPropertyMetadata("Alert text.", FrameworkPropertyMetadataOptions.Journal, OnTextChanged));

        public bool IsDismissible
        {
            get { return (bool)GetValue(IsDismissibleProperty); }
            set { SetValue(IsDismissibleProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        private Button closeButton;

        private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var al = (AlertLabel)sender;
            al.Visibility = e.NewValue == null ? Visibility.Collapsed : Visibility.Visible;
        }

        static AlertLabel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AlertLabel), new FrameworkPropertyMetadata(typeof(AlertLabel)));
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (closeButton != null)
                closeButton.Click -= CloseButton_Click;
            closeButton = GetTemplateChild("PART_CloseButton") as Button;
            if (closeButton != null)
                closeButton.Click += CloseButton_Click;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }
    }
}
