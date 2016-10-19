using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using Unclassified.TxLib;
using WikiEdit.Models;

namespace WikiEdit.ViewModels
{
    internal class AccountProfileViewModel : BindableBase
    {
        private string _UserName;
        private IReadOnlyList<string> _Groups;
        private bool _HasLoggedIn;

        public string UserName
        {
            get { return _UserName; }
            private set { SetProperty(ref _UserName, value); }
        }

        public IReadOnlyList<string> Groups
        {
            get { return _Groups; }
            private set { SetProperty(ref _Groups, value); }
        }

        public bool HasLoggedIn
        {
            get { return _HasLoggedIn; }
            private set
            {
                if (SetProperty(ref _HasLoggedIn, value))
                {
                    _LoginCommand?.RaiseCanExecuteChanged();
                    _LogoutCommand?.RaiseCanExecuteChanged();
                }
            }
        }


        public WikiSiteViewModel WikiSite { get; }

        private LoginViewModel _LoginViewModel;

        public LoginViewModel LoginViewModel
        {
            get { return _LoginViewModel; }
            private set
            {
                if (SetProperty(ref _LoginViewModel, value))
                {
                    _LoginCommand?.RaiseCanExecuteChanged();
                    _LogoutCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Refresh account information from <see cref="WikiSite"/>.
        /// </summary>
        private void Reload()
        {
            var site = WikiSite.GetSite();
            UserName = site.UserInfo.Name;
            Groups = site.UserInfo.Groups.ToArray();
            HasLoggedIn = site.UserInfo.IsUser;
        }

        private DelegateCommand _RefreshCommand;

        public DelegateCommand RefreshCommand
        {
            get
            {
                if (_RefreshCommand == null)
                {
                    _RefreshCommand = new DelegateCommand(async () =>
                    {
                        var site = await WikiSite.GetSiteAsync();
                        await site.RefreshUserInfoAsync();
                        Reload();
                    });
                }
                return _RefreshCommand;
            }
        }

        private DelegateCommand _LoginCommand;

        public DelegateCommand LoginCommand
        {
            get
            {
                if (_LoginCommand == null)
                {
                    _LoginCommand = new DelegateCommand(() =>
                    {
                        if (LoginViewModel == null)
                            LoginViewModel = new LoginViewModel(WikiSite, () =>
                            {
                                LoginViewModel = null;
                                Reload();
                            }) {UserName = UserName};
                    }, () => _LoginViewModel == null && !HasLoggedIn);
                }
                return _LoginCommand;
            }
        }

        private DelegateCommand _LogoutCommand;

        public DelegateCommand LogoutCommand
        {
            get
            {
                if (_LogoutCommand == null)
                {
                    _LogoutCommand = new DelegateCommand(async () =>
                    {
                        if (Utility.Confirm(Tx.T("logout confirmation")) == true)
                        {
                            var site = await WikiSite.GetSiteAsync();
                            await site.LogoutAsync();
                            Reload();
                        }
                    }, () => _LoginViewModel == null && HasLoggedIn);
                }
                return _LogoutCommand;
            }
        }

        public AccountProfileViewModel(WikiSiteViewModel siteVm) : this(siteVm, null)
        {
        }

        public AccountProfileViewModel(WikiSiteViewModel siteVm, WikiSite siteModel)
        {
            if (siteVm == null) throw new ArgumentNullException(nameof(siteVm));
            WikiSite = siteVm;
            if (siteModel != null)
            {
                _UserName = siteModel.UserName;
            }
        }
    }
}
