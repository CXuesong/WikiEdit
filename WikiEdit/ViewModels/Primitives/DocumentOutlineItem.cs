using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace WikiEdit.ViewModels.Primitives
{
    /// <summary>
    /// Represents an items in the Document Outline view.
    /// </summary>
    public class DocumentOutlineItem : BindableBase
    {
        public event EventHandler DoubleClick;

        private string _Text;
        private object _OutlineContext;
        private bool _IsSelected;

        public string Text
        {
            get { return _Text; }
            set { SetProperty(ref _Text, value); }
        }

        public bool IsSelected
        {
            get { return _IsSelected; }
            set { SetProperty(ref _IsSelected, value); }
        }


        public object OutlineContext
        {
            get { return _OutlineContext; }
            set { SetProperty(ref _OutlineContext, value); }
        }

        public ObservableCollection<DocumentOutlineItem> Children { get; } = new ObservableCollection<DocumentOutlineItem>();

        protected virtual void OnDoubleClick()
        {
            DoubleClick?.Invoke(this, EventArgs.Empty);
        }

        #region Invoked by View

        internal void NotifyDoubleClick()
        {
            OnDoubleClick();
        }

        #endregion
    }
}
