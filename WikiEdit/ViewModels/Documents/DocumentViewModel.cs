using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;

namespace WikiEdit.ViewModels.Documents
{
    public class DocumentViewModel : BindableBase
    {
        public event EventHandler Activated;

        public event CancelEventHandler Closing;

        public event EventHandler Closed;

        #region View Properties

        private string _Title;
        private string _TitleToolTip;
        private bool _IsSelected;
        private bool _IsActive;
        private string _ContentId;

        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }

        public string TitleToolTip
        {
            get { return _TitleToolTip; }
            set { SetProperty(ref _TitleToolTip, value); }
        }

        public bool IsSelected
        {
            get { return _IsSelected; }
            set { SetProperty(ref _IsSelected, value); }
        }

        public bool IsActive
        {
            get { return _IsActive; }
            set
            {
                if (SetProperty(ref _IsActive, value))
                {
                    if (value) OnActivated();
                }
            }
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
        private bool _IsBusy;
        private string _Status;

        public DelegateCommand CloseCommand
        {
            get
            {
                if (_CloseCommand == null)
                {
                    _CloseCommand = new DelegateCommand(() => Close());
                }
                return _CloseCommand;
            }
        }

        #endregion

        public bool Close()
        {
            var e = new CancelEventArgs();
            OnClosing(e);
            // Normally, ChildViewModelService will be responsible to close the document.
            if (!e.Cancel)
            {
                OnClosed();
                return true;
            }
            return false;
        }

        /// <summary>
        /// This proeprty is used identify the attached data
        /// source of the document, so that the document view model
        /// can be activated by e.g. WikiSiteViewModel .
        /// </summary>
        public virtual object DocumentContext => null;

        /// <summary>
        /// The context <see cref="WikiSiteViewModel"/> of the document.
        /// </summary>
        public virtual WikiSiteViewModel SiteContext => null;

        public bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                if (SetProperty(ref _IsBusy, value)) OnIsBusyChanged();
            }
        }

        public string Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }

        protected virtual void OnActivated()
        {
            Activated?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnClosing(CancelEventArgs e)
        {
            Closing?.Invoke(this, e);
        }

        protected virtual void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnIsBusyChanged()
        {
            
        }
    }
}
