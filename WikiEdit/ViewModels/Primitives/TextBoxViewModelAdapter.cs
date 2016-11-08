using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using Prism.Mvvm;

namespace WikiEdit.ViewModels.Primitives
{
    /// <summary>
    /// Provides a wrapper and abstraction for <see cref="TextEditor"/> so that it won't
    /// "pollute" view models.
    /// </summary>
    public class TextBoxViewModelAdapter : BindableBase
    {
        private TextEditor _Adaptee;

        public TextBoxViewModelAdapter()
        {
        }

        public TextEditor Adaptee
        {
            private get { return _Adaptee; }
            set
            {
                if (_Adaptee != value)
                {
                    if (_Adaptee != null)
                    {
                        _Adaptee.Document.TextChanged -= Document_TextChanged;
                    }
                    if (value != null)
                    {
                        value.Document.Text = Text ?? "";
                        value.Document.TextChanged += Document_TextChanged;
                    }
                    _Adaptee = value;
                }
            }
        }

        public int SelectionStart
        {
            get { return _Adaptee.SelectionStart; }
            set { _Adaptee.SelectionStart = value; }
        }

        public int SelectionLength
        {
            get { return _Adaptee.SelectionLength; }
            set { _Adaptee.SelectionStart = value; }
        }

        /// <summary>
        /// A special string instance used to mark the Text value as "invalidated".
        /// </summary>
        private static readonly string INVALIDATED_STRING = new string("INVALIDATED".ToCharArray());

        private string _Text;

        public string Text
        {
            get
            {
                if (ReferenceEquals(_Text, INVALIDATED_STRING))
                    _Text = _Adaptee.Dispatcher.AutoInvoke(() => _Adaptee.Text);
                return _Text;
            }
            set
            {
                if (_Adaptee == null)
                {
                    // Allows to set text before attached to TextEdit
                    _Text = value;
                    OnPropertyChanged();
                }
                else
                {
                    _Adaptee.Dispatcher.AutoInvoke(() => _Adaptee.Text = value);
                    // If the text hasn't been changed at all, the assertion will fail.
                    //Debug.Assert(ReferenceEquals(_Text, INVALIDATED_STRING));
                    _Text = value;
                }
            }
        }

        public void Select(int start, int length)
        {
            _Adaptee.Select(start, length);
        }

        public void ScrollTo(int line, int column)
        {
            _Adaptee.ScrollToLine(line);
        }

        private void Document_TextChanged(object sender, EventArgs e)
        {
            _Text = INVALIDATED_STRING;
            OnPropertyChanged(nameof(Text));
        }
    }
}
