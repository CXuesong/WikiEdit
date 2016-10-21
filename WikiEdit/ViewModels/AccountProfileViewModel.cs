using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Events;
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

        private bool _IsBusy;

        public bool IsBusy
        {
            get { return _IsBusy; }
            set { SetProperty(ref _IsBusy, value); }
        }

        private string _Status;

        public string Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
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
            var site = WikiSite.Site;
            UserName = site.UserInfo.Name;
            Groups = site.UserInfo.Groups.ToArray();
            HasLoggedIn = site.UserInfo.IsUser;
        }

        #region Commands

        private DelegateCommand _RefreshCommand;

        public DelegateCommand RefreshCommand
        {
            get
            {
                if (_RefreshCommand == null)
                {
                    _RefreshCommand = new DelegateCommand(async () =>
                    {
                        if (!WikiSite.IsBusy) await WikiSite.RefreshAccountInfoAsync();
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
                            LoginViewModel = new LoginViewModel(WikiSite,
                                successful =>
                                {
                                    LoginViewModel = null;
                                    if (successful) Reload();
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
                            if (WikiSite.IsBusy) return;
                            WikiSite.IsBusy = true;
                            WikiSite.Status = Tx.T("please wait");
                            try
                            {
                                await WikiSite.Site.LogoutAsync();
                                WikiSite.Status = null;
                            }
                            catch (Exception ex)
                            {
                                WikiSite.Status = ex.Message;
                            }
                            finally
                            {
                                WikiSite.IsBusy = false;
                            }
                            Reload();
                        }
                    }, () => _LoginViewModel == null && HasLoggedIn);
                }
                return _LogoutCommand;
            }
        }

        #endregion

        public AccountProfileViewModel(IEventAggregator eventAggregator, WikiSiteViewModel siteVm) 
            : this(eventAggregator, siteVm, null)
        {
        }

        public AccountProfileViewModel(IEventAggregator eventAggregator, WikiSiteViewModel siteVm, WikiSite siteModel)
        {
            if (siteVm == null) throw new ArgumentNullException(nameof(siteVm));
            if (eventAggregator == null) throw new ArgumentNullException(nameof(eventAggregator));
            WikiSite = siteVm;
            if (siteModel != null)
            {
                _UserName = siteModel.UserName;
            }
            eventAggregator.GetEvent<AccountInfoRefreshedEvent>().Subscribe(site =>
            {
                if (site == WikiSite) Reload();
            });
        }
    }
}
