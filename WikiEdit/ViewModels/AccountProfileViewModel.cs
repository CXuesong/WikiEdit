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
    public class AccountProfileViewModel : BindableBase
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
        private async Task ReloadAsync()
        {
            var site = await WikiSite.GetSiteAsync();
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
                        if (IsBusy) return;
                        IsBusy = true;
                        try
                        {
                            await WikiSite.RefreshAccountInfoAsync();
                        }
                        catch (Exception ex)
                        {
                            Status = Utility.GetExceptionMessage(ex);
                        }
                        IsBusy = false;
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
                            LoginViewModel = new LoginViewModel(WikiSite, async successful =>
                            {
                                LoginViewModel = null;
                                if (successful) await ReloadAsync();
                            }, (b, s) =>
                            {
                                IsBusy = b;
                                Status = s;
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
                        if (IsBusy) return;
                        if (Utility.Confirm(Tx.T("logout confirmation")) == true)
                        {
                            if (IsBusy) return;
                            IsBusy = true;
                            Status = Tx.T("please wait");
                            try
                            {
                                var site = await WikiSite.GetSiteAsync();
                                await site.LogoutAsync();
                                Status = null;
                            }
                            catch (Exception ex)
                            {
                                Status = Utility.GetExceptionMessage(ex);
                            }
                            finally
                            {
                                IsBusy = false;
                            }
                            await ReloadAsync();
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

        internal AccountProfileViewModel(IEventAggregator eventAggregator, WikiSiteViewModel wikiSite, WikiSite siteModel)
        {
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            if (eventAggregator == null) throw new ArgumentNullException(nameof(eventAggregator));
            WikiSite = wikiSite;
            // Try to load cached account info
            if (siteModel != null)
            {
                _UserName = siteModel.UserName;
                _Groups = siteModel.UserGroups?.ToList();
            }
            wikiSite.AccountRefreshedEvent.Subscribe(() => ReloadAsync().Forget());
        }
    }
}
