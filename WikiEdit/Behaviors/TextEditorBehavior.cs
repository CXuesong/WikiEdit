using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;
using ICSharpCode.AvalonEdit;

namespace WikiEdit.Behaviors
{
    /// <summary>
    /// A helper behavior class that enables MVVM binding of some non-dependency properties.
    /// </summary>
    internal class TextEditorBehavior : Behavior<TextEditor>
    {
        public static readonly DependencyProperty SelectionStartProperty =
            DependencyProperty.Register("SelectionStart", typeof(int), typeof(TextEditorBehavior),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionStartChanged));

        public static readonly DependencyProperty SelectionLengthProperty =
            DependencyProperty.Register("SelectionLength", typeof(int), typeof(TextEditorBehavior),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionLengthChanged));

        private int _SelectionStart, _SelectionLength;

        public int SelectionStart
        {
            get { return (int) GetValue(SelectionStartProperty); }
            set { SetValue(SelectionStartProperty, value); }
        }

        public int SelectionLength
        {
            get { return (int) GetValue(SelectionLengthProperty); }
            set { SetValue(SelectionLengthProperty, value); }
        }

        /// <inheritdoc />
        protected override void OnAttached()
        {
            base.OnAttached();
            Debug.Assert(AssociatedObject != null);
            AssociatedObject.TextArea.SelectionChanged += TextArea_SelectionChanged;
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            base.OnDetaching();
            Debug.Assert(AssociatedObject != null);
            AssociatedObject.TextArea.SelectionChanged -= TextArea_SelectionChanged;
        }

        private void TextArea_SelectionChanged(object sender, EventArgs e)
        {
            SelectionStart = _SelectionStart = AssociatedObject.SelectionStart;
            SelectionLength = _SelectionLength = AssociatedObject.SelectionLength;
        }

        private static void OnSelectionStartChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (TextEditorBehavior) sender;
            // Decides whther the property change is raised externally.
            if (behavior._SelectionStart != (int) e.NewValue)
                behavior.AssociatedObject.SelectionStart = (int) e.NewValue;
        }

        private static void OnSelectionLengthChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (TextEditorBehavior)sender;
            // Decides whther the property change is raised externally.
            if (behavior._SelectionLength != (int) e.NewValue)
                behavior.AssociatedObject.SelectionLength = (int) e.NewValue;
        }
    }
}
