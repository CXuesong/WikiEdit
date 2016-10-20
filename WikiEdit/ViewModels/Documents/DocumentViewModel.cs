using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;

namespace WikiEdit.ViewModels.Documents
{
    internal class DocumentViewModel : BindableBase
    {
        public event EventHandler Close;

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

        public string ContentId
        {
            get { return _ContentId; }
            set { SetProperty(ref _ContentId, value); }
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
                    _CloseCommand = new DelegateCommand(OnClose);
                }
                return _CloseCommand;
            }
        }

        #endregion

        protected virtual void OnClose()
        {
            Close?.Invoke(this, EventArgs.Empty);
        }
    }
}
