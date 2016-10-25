using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Unclassified.TxLib;
using WikiClientLibrary;
using WikiEdit.Controllers;

namespace WikiEdit.ViewModels
{
    internal class WikiSiteEditingViewModel : BindableBase, INotifyDataErrorInfo
    {
        private readonly WikiEditController _WikiEditController;
        private readonly ErrorsContainer<string> _ErrorsContainer;
        private readonly Action _AcceptCallback, _CancelCallback;
        private readonly TaskScheduler _MainTaskScheduler;
        private string _Name;
        private bool _NeedValidateApiEndpoint;

        public WikiSiteEditingViewModel(WikiEditController wikiEditController, Action acceptCallback, Action cancelCallback)
        {
            if (wikiEditController == null) throw new ArgumentNullException(nameof(wikiEditController));
            if (acceptCallback == null) throw new ArgumentNullException(nameof(acceptCallback));
            if (cancelCallback == null) throw new ArgumentNullException(nameof(cancelCallback));
            _WikiEditController = wikiEditController;
            _AcceptCallback = acceptCallback;
            _CancelCallback = cancelCallback;
            _ErrorsContainer = new ErrorsContainer<string>(OnErrorsChanged);
            _MainTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        public void LoadFromWikiSite(WikiSiteViewModel wikiSite)
        {
            Name = wikiSite.Name;
            SiteName = wikiSite.SiteName;
            UserName = wikiSite.AccountProfile.UserName;
            // So that it won't trigger UpdateSiteInfoAsync
            SetValidatedApiEndpoint(wikiSite.ApiEndpoint);
        }

        private bool _IsBusy;

        public bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                if (SetProperty(ref _IsBusy, value))
                    _OkCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _Status;

        public string Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }


        #region User Input

        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }

        private string _ApiEndpoint;

        public string ApiEndpoint
        {
            get { return _ApiEndpoint; }
            set
            {
                if (SetProperty(ref _ApiEndpoint, value))
                        InvalidateSiteInfo();
            }
        }

        public void SetValidatedApiEndpoint(string url)
        {
            if (SetProperty(ref _ApiEndpoint, url, nameof(ApiEndpoint)))
                _NeedValidateApiEndpoint = false;
        }

        private static bool BasicValidateApiEndpoint(string endpointUrl)
        {
            Uri u;
            if (Uri.TryCreate(endpointUrl, UriKind.Absolute, out u)) return true;
            if (Uri.TryCreate("http://" + endpointUrl, UriKind.Absolute, out u)) return true;
            return false;
        }

        public string SiteNameHint
            => Tx.T("wiki site.use site configuration") + " " + SiteName;

        private DelegateCommand _OkCommand;

        public DelegateCommand OkCommand
        {
            get
            {
                if (_OkCommand == null)
                {
                    _OkCommand = new DelegateCommand(async () =>
                        {
                            if (_NeedValidateApiEndpoint)
                                await UpdateSiteInfoAsync();
                            if (!HasErrors)
                                _AcceptCallback();
                        },
                        () => !IsBusy && !HasErrors);
                }
                return _OkCommand;
            }
        }

        private DelegateCommand _CancelCommand;

        public DelegateCommand CancelCommand
        {
            get
            {
                if (_CancelCommand == null)
                {
                    _CancelCommand = new DelegateCommand(() => _CancelCallback());
                }
                return _CancelCommand;
            }
        }

        #endregion

        #region Site Info

        private string _SiteName;
        private string _UserName;

        public string SiteName
        {
            get { return _SiteName; }
            set
            {
                if (SetProperty(ref _SiteName, value)) OnPropertyChanged(nameof(SiteNameHint));
            }
        }

        public string UserName
        {
            get { return _UserName; }
            set { SetProperty(ref _UserName, value); }
        }

        public void InvalidateSiteInfo()
        {
            var endPoint = ApiEndpoint;
            _NeedValidateApiEndpoint = true;
            if (!BasicValidateApiEndpoint(endPoint)) return;
            Task.Delay(TimeSpan.FromSeconds(1))
                .ContinueWith(t =>
                {
                    if (endPoint != ApiEndpoint) return;
                    UpdateSiteInfoAsync().Forget();
                }, _MainTaskScheduler);
        }

        public async Task UpdateSiteInfoAsync()
        {
            if (string.IsNullOrWhiteSpace(ApiEndpoint))
            {
                _ErrorsContainer.SetErrors(nameof(ApiEndpoint), Tx.T("errors.field is required"));
                return;
            }
            if (IsBusy) return;
            IsBusy = true;
            Status = Tx.T("wiki site.validating api endpoint");
            var urlToValidate = ApiEndpoint;
            try
            {
                // Search for API endpoint URL
                var endPoint = await Site.SearchApiEndpointAsync(_WikiEditController.WikiClient, urlToValidate);
                if (endPoint == null)
                {
                    _ErrorsContainer.SetErrors(nameof(ApiEndpoint), Tx.T("errors.invalid api endpoint"));
                    Status = Tx.T("errors.invalid api endpoint");
                    return;
                }
                Status = Tx.T("please wait");
                // Gather site information
                var site = await _WikiEditController.CreateSiteAsync(endPoint);
                SiteName = site.SiteInfo.SiteName;
                UserName = site.UserInfo.Name;
                // Clear validation errors
                _ErrorsContainer.ClearErrors(null);
                _ErrorsContainer.ClearErrors(nameof(ApiEndpoint));
                Status = null;
                if (urlToValidate == ApiEndpoint)
                {
                    SetValidatedApiEndpoint(endPoint);
                    _NeedValidateApiEndpoint = false;
                }
            }
            catch (Exception ex)
            {
                Status = Utility.GetExceptionMessage(ex);
                _ErrorsContainer.SetErrors(null, ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region INotifyDataErrorInfo

        /// <inheritdoc />
        public IEnumerable GetErrors(string propertyName)
        {
            return _ErrorsContainer.GetErrors(propertyName);
        }

        /// <inheritdoc />
        public bool HasErrors => _ErrorsContainer.HasErrors;

        /// <inheritdoc />
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        #endregion
    }
}
