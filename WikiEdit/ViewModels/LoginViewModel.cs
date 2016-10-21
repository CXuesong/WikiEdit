using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Unclassified.TxLib;

namespace WikiEdit.ViewModels
{
    internal class LoginViewModel : BindableBase, INotifyDataErrorInfo
    {
        /// <summary>
        /// The action used to close current login view.
        /// </summary>
        private readonly Action<bool> _CloseViewAction;
        private readonly ErrorsContainer<string> errors;
        private string _UserName;

        public WikiSiteViewModel WikiSite { get; }

        public string Heading => Tx.T("login into wikiname", "name", WikiSite.DisplayName);


        public string UserName
        {
            get { return _UserName; }
            set
            {
                if (SetProperty(ref _UserName, value))
                {
                    if (string.IsNullOrEmpty(value))
                        errors.SetErrors(nameof(UserName), new[] {Tx.T("field is required")});
                    else
                        errors.ClearErrors(nameof(UserName));
                }
            }
        }

        #region Commands/Callbacks


        public async Task LoginAsync(SecureString password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (errors.HasErrors) return;
            if (string.IsNullOrEmpty(UserName))
            {
                // Make validation error show.
                UserName = null;
                UserName = "";
            }
            if (WikiSite.IsBusy) return;
            WikiSite.IsBusy = true;
            WikiSite.Status = Tx.T("please wait");
            try
            {
                // It seems not safe enough.
                // Maybe adjustments will be applied to WikiClientLibrary later.
                var pp = IntPtr.Zero;
                try
                {
                    pp = Marshal.SecureStringToGlobalAllocUnicode(password);
                    var ps = Marshal.PtrToStringUni(pp);
                    await WikiSite.Site.LoginAsync(_UserName, ps);
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(pp);
                }
                WikiSite.Status = null;
                _CloseViewAction(true);
            }
            catch (Exception ex)
            {
                WikiSite.Status = ex.Message;
            }
            finally
            {
                WikiSite.IsBusy = false;
            }
        }


        private DelegateCommand _CancelCommand;

        public DelegateCommand CancelCommand
        {
            get
            {
                if (_CancelCommand == null)
                {
                    _CancelCommand = new DelegateCommand(() => _CloseViewAction(false));
                }
                return _CancelCommand;
            }
        }


        #endregion

        // statusChangedAction : IsWorking, Status
        public LoginViewModel(WikiSiteViewModel siteVm, Action<bool> closeViewAction)
        {
            if (siteVm == null) throw new ArgumentNullException(nameof(siteVm));
            if (closeViewAction == null) throw new ArgumentNullException(nameof(closeViewAction));
            WikiSite = siteVm;
            _CloseViewAction = closeViewAction;
            errors = new ErrorsContainer<string>(OnErrorsChanged);
        }

        public IEnumerable GetErrors(string propertyName)
            => errors.GetErrors(propertyName);
        public bool HasErrors => errors.HasErrors;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }
}
