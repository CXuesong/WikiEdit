using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WikiEdit.Controls
{
    /// <summary>
    /// A text box that supports auto completion and can show
    /// a list of candidates.
    /// </summary>
    public class AutoCompleteTextBox : TextBox
    {
        // Referenced and heavily modified from http://www.codeproject.com/Articles/44920/A-Reusable-WPF-Autocomplete-TextBox .
        ListBox listBox;
        Func<object, string, bool> filter;

        // Binding hack - not really necessary.
        //DependencyObject dummy = new DependencyObject();
        private readonly FrameworkElement dummy = new FrameworkElement();

        public Func<object, string, bool> Filter
        {
            get { return filter; }
            set
            {
                if (filter != value)
                {
                    filter = value;
                    if (listBox != null)
                    {
                        if (filter != null)
                            listBox.Items.Filter = FilterFunc;
                        else
                            listBox.Items.Filter = null;
                    }
                }
            }
        }

        #region ItemsSource Dependency Property

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            ItemsControl.ItemsSourceProperty.AddOwner(
                typeof(AutoCompleteTextBox),
                new UIPropertyMetadata(null, OnItemsSourceChanged));

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var actb = d as AutoCompleteTextBox;
            actb?.OnItemsSourceChanged(e.NewValue as IEnumerable);
        }

        protected void OnItemsSourceChanged(IEnumerable itemsSource)
        {
            if (listBox == null) return;
            listBox.ItemsSource = itemsSource;
        }

        public bool IsDropDownOpen
        {
            get { return (bool)GetValue(IsDropDownOpenProperty); }
            set { SetValue(IsDropDownOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsDropDownOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register("IsDropDownOpen", typeof(bool), typeof(AutoCompleteTextBox),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region Binding Dependency Property

        public string TextBinding
        {
            get { return (string) GetValue(TextBindingProperty); }
            set { SetValue(TextBindingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Binding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextBindingProperty =
            DependencyProperty.Register("TextBinding", typeof(string), typeof(AutoCompleteTextBox),
                new UIPropertyMetadata(null));

        #endregion

        #region ItemTemplate Dependency Property

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate) GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemTemplateProperty =
            ItemsControl.ItemTemplateProperty.AddOwner(
                typeof(AutoCompleteTextBox),
                new UIPropertyMetadata(null, OnItemTemplateChanged));

        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var actb = d as AutoCompleteTextBox;
            if (actb == null) return;
            actb.OnItemTemplateChanged(e.NewValue as DataTemplate);
        }

        private void OnItemTemplateChanged(DataTemplate p)
        {
            if (listBox == null) return;
            listBox.ItemTemplate = p;
        }

        #endregion

        #region ItemContainerStyle Dependency Property

        public Style ItemContainerStyle
        {
            get { return (Style) GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemContainerStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemContainerStyleProperty =
            ItemsControl.ItemContainerStyleProperty.AddOwner(
                typeof(AutoCompleteTextBox),
                new UIPropertyMetadata(null, OnItemContainerStyleChanged));

        private static void OnItemContainerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var actb = d as AutoCompleteTextBox;
            if (actb == null) return;
            actb.OnItemContainerStyleChanged(e.NewValue as Style);
        }

        private void OnItemContainerStyleChanged(Style p)
        {
            if (listBox == null) return;
            listBox.ItemContainerStyle = p;
        }

        #endregion

        #region MaxCompletions Dependency Property

        public int MaxCompletions
        {
            get { return (int) GetValue(MaxCompletionsProperty); }
            set { SetValue(MaxCompletionsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxCompletions.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxCompletionsProperty =
            DependencyProperty.Register("MaxCompletions", typeof(int), typeof(AutoCompleteTextBox),
                new UIPropertyMetadata(int.MaxValue));

        #endregion

        #region ItemTemplateSelector Dependency Property

        public DataTemplateSelector ItemTemplateSelector
        {
            get { return (DataTemplateSelector) GetValue(ItemTemplateSelectorProperty); }
            set { SetValue(ItemTemplateSelectorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemTemplateSelector.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemTemplateSelectorProperty =
            ItemsControl.ItemTemplateSelectorProperty.AddOwner(typeof(AutoCompleteTextBox),
                new UIPropertyMetadata(null, OnItemTemplateSelectorChanged));

        private static void OnItemTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var actb = d as AutoCompleteTextBox;
            if (actb == null) return;
            actb.OnItemTemplateSelectorChanged(e.NewValue as DataTemplateSelector);
        }

        private void OnItemTemplateSelectorChanged(DataTemplateSelector p)
        {
            if (listBox == null) return;
            listBox.ItemTemplateSelector = p;
        }

        #endregion

        static AutoCompleteTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoCompleteTextBox),
                new FrameworkPropertyMetadata(typeof(AutoCompleteTextBox)));
        }

        private void SetTextValueBySelection(object obj, bool moveFocus)
        {
            IsDropDownOpen = false;
            Dispatcher.Invoke(() =>
            {
                Focus();
                if (moveFocus)
                    MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }, DispatcherPriority.Background);

            // Retrieve the Binding object from the control.
            var originalBinding = BindingOperations.GetBinding(this, TextBindingProperty);
            if (originalBinding == null) return;

            // Binding hack - not really necessary.
            //Binding newBinding = new Binding()
            //{
            //    Path = new PropertyPath(originalBinding.Path.Path, originalBinding.Path.PathParameters),
            //    XPath = originalBinding.XPath,
            //    Converter = originalBinding.Converter,
            //    ConverterParameter = originalBinding.ConverterParameter,
            //    ConverterCulture = originalBinding.ConverterCulture,
            //    StringFormat = originalBinding.StringFormat,
            //    TargetNullValue = originalBinding.TargetNullValue,
            //    FallbackValue = originalBinding.FallbackValue
            //};
            //newBinding.Source = obj;
            //BindingOperations.SetBinding(dummy, TextProperty, newBinding);

            // Set the dummy's DataContext to our selected object.
            dummy.DataContext = obj;

            // Apply the binding to the dummy FrameworkElement.
            BindingOperations.SetBinding(dummy, TextProperty, originalBinding);

            // Get the binding's resulting value.
            Text = dummy.GetValue(TextProperty).ToString();
            listBox.SelectedIndex = -1;
            SelectAll();
        }

        private bool FilterFunc(object obj)
        {
            return filter(obj, Text);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            listBox = Template.FindName("PART_ListBox", this) as ListBox;
            if (listBox != null)
            {
                listBox.PreviewMouseDown += listBox_MouseUp;
                listBox.KeyDown += listBox_KeyDown;
                OnItemsSourceChanged(ItemsSource);
                OnItemTemplateChanged(ItemTemplate);
                OnItemContainerStyleChanged(ItemContainerStyle);
                OnItemTemplateSelectorChanged(ItemTemplateSelector);
                if (filter != null)
                    listBox.Items.Filter = FilterFunc;
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            var fs = FocusManager.GetFocusScope(this);
            var o = FocusManager.GetFocusedElement(fs);
            if (o != listBox) IsDropDownOpen = false;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            var fs = FocusManager.GetFocusScope(this);
            var o = FocusManager.GetFocusedElement(fs);
            if (e.Key == Key.Escape)
            {
                IsDropDownOpen = false;
                Focus();
            }
            else if (listBox != null && o == this)
            {
                IsDropDownOpen = true;
                if (e.Key == Key.Up || e.Key == Key.Down)
                    listBox.Focus();
            }
        }

        void listBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject) e.OriginalSource;
            while ((dep != null) && !(dep is ListBoxItem))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep == null) return;
            var item = listBox.ItemContainerGenerator.ItemFromContainer(dep);
            if (item == null) return;
            SetTextValueBySelection(item, false);
        }

        void listBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
                SetTextValueBySelection(listBox.SelectedItem, false);
            else if (e.Key == Key.Tab)
                SetTextValueBySelection(listBox.SelectedItem, true);
        }
    }

    public static class ListBoxItemBehavior
    {
        public static bool GetSelectOnMouseOver(DependencyObject obj)
        {
            return (bool) obj.GetValue(SelectOnMouseOverProperty);
        }

        public static void SetSelectOnMouseOver(DependencyObject obj, bool value)
        {
            obj.SetValue(SelectOnMouseOverProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectOnMouseOver.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectOnMouseOverProperty =
            DependencyProperty.RegisterAttached("SelectOnMouseOver", typeof(bool), typeof(ListBoxItemBehavior),
                new UIPropertyMetadata(false, OnSelectOnMouseOverChanged));

        static void OnSelectOnMouseOverChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var lbi = d as ListBoxItem;
            if (lbi == null) return;
            bool bNew = (bool) e.NewValue, bOld = (bool) e.OldValue;
            if (bNew == bOld) return;
            if (bNew)
                lbi.MouseEnter += lbi_MouseEnter;
            else
                lbi.MouseEnter -= lbi_MouseEnter;
        }

        static void lbi_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var lbi = (ListBoxItem) sender;
            lbi.IsSelected = true;
            var listBox = ItemsControl.ItemsControlFromItemContainer(lbi);
            var focusedElement = (FrameworkElement) FocusManager.GetFocusedElement(FocusManager.GetFocusScope(listBox));
            if (focusedElement != null && focusedElement.IsDescendantOf(listBox))
                lbi.Focus();
        }
    }
}
