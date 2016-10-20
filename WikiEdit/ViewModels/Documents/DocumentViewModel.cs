using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;

namespace WikiEdit.ViewModels.Documents
{
    internal class DocumentViewModel : BindableBase
    {
        public event CancelEventHandler Closing;

        public event EventHandler Closed;

        #region View Properties

        private string _Title;
        private bool _IsSelected;
        private bool _IsActive;
        private string _ContentId;

        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }

        public bool IsSelected
        {
            get { return _IsSelected; }
            set { SetProperty(ref _IsSelected, value); }
        }

        public bool IsActive
        {
            get { return _IsActive; }
            set { SetProperty(ref _IsActive, value); }
        }

        /// <summary>
        /// ContentId of the document panel, which later can be
        /// used to identify the panel in UI controls.
        /// </summary>
        public string ContentId
        {
            get { return _ContentId; }
            set { SetProperty(ref _ContentId, value); }
        }

        protected void BuildContentId(string id)
        {
            ContentId = GetType().Name + "/" + id;
        }

        #endregion

        #region Commands

        private DelegateCommand _CloseCommand;

        public DelegateCommand CloseCommand
        {
            get
            {
                if (_CloseCommand == null)
                {
                    _CloseCommand = new DelegateCommand(() =>
                    {
                        var e = new CancelEventArgs();
                        OnClose(e);
                        // Normally, ChildViewModelService will be responsible to close the document.
                        if (!e.Cancel)
                            OnClosed();
                    });
                }
                return _CloseCommand;
            }
        }

        #endregion

        /// <summary>
        /// This proeprty is used identify the attached data
        /// source of the document, so that the document view model
        /// can be activated by e.g. WikiSiteViewModel .
        /// </summary>
        public virtual object ContentSource => null;

        protected virtual void OnClose(CancelEventArgs e)
        {
            Closing?.Invoke(this, e);
        }

        protected virtual void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }
}
