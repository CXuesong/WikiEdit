using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Prism.Commands;
using Prism.Mvvm;
using WikiEdit.ViewModels.Primitives;

namespace WikiEdit.ViewModels.Documents
{
    public class DocumentViewModel : BindableBase
    {
        public event EventHandler Activated;

        public event CancelEventHandler Closing;

        public event EventHandler Closed;

        private WikiSiteViewModel _SiteContext;

        #region Docking View Properties

        private string _Title;
        private string _ToolTip;
        private bool _IsSelected;
        private bool _IsActive;
        private string _ContentId;

        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }

        public string ToolTip
        {
            get { return _ToolTip; }
            set { SetProperty(ref _ToolTip, value); }
        }

        /// <summary>
        /// Whether the document page is visible to user.
        /// </summary>
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

        private static readonly ImageSource BusyIndicatorImage =
            (ImageSource) Application.Current.FindResource("Images/BusyIndicator");

        public ImageSource HeaderImage
        {
            get
            {
                if (_IsBusy) return BusyIndicatorImage;
                return null;
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
        /// <value>If the document has no such context, the value can be <c>null</c>.</value>
        public WikiSiteViewModel SiteContext
        {
            get { return _SiteContext; }
            set { SetProperty(ref _SiteContext, value); }
        }

        public bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                if (SetProperty(ref _IsBusy, value))
                {
                    OnIsBusyChanged();
                    OnPropertyChanged(nameof(HeaderImage));
                }
            }
        }

        public string Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }

        /// <summary>
        /// Items for document outline.
        /// </summary>
        public ObservableCollection<DocumentOutlineItem> DocumentOutline { get; } =
            new ObservableCollection<DocumentOutlineItem>();

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
