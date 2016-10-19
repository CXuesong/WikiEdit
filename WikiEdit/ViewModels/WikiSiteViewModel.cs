using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using WikiClientLibrary;
using WikiEdit.Controllers;
using WikiEdit.Models;

namespace WikiEdit.ViewModels
{
    internal class WikiSiteViewModel : BindableBase
    {
        private readonly WikiEditController _Controller;
        private string _Name;
        private string _ApiEndpoint;
        private DateTimeOffset _LastAccessTime;
        private Site _Site;
        private string _SiteName;

        /// <summary>
        /// User-defined site name.
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set
            {
                if (SetProperty(ref _Name, value))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary>
        /// Site title.
        /// </summary>
        public string SiteName
        {
            get { return _SiteName; }
            set
            {
                if (SetProperty(ref _SiteName, value))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string DisplayName => string.IsNullOrWhiteSpace(_Name) ? _SiteName : _Name;

        public string ApiEndpoint
        {
            get { return _ApiEndpoint; }
            set { SetProperty(ref _ApiEndpoint, value); }
        }

        public DateTimeOffset LastAccessTime
        {
            get { return _LastAccessTime; }
            set { SetProperty(ref _LastAccessTime, value); }
        }

        public AccountProfileViewModel AccountProfile { get; }

        public async Task<Site> GetSiteAsync()
        {
            if (_Site == null)
            {
                _Site = await _Controller.CreateSiteAsync(_ApiEndpoint);
            }
            return _Site;
        }

        public Site GetSite()
        {
            if (_Site == null) throw new InvalidOperationException();
            return _Site;
        }

        public WikiSite ToModel() => new WikiSite
        {
            Name = _Name,
            SiteName = _SiteName,
            ApiEndpoint = _ApiEndpoint,
            LastAccessTime = _LastAccessTime,
            UserName = AccountProfile.UserName,
        };

        public WikiSiteViewModel(WikiEditController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            _Controller = controller;
            AccountProfile = new AccountProfileViewModel(this);
        }

        public WikiSiteViewModel(WikiSite model, WikiEditController controller)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            _Name = model.Name;
            _SiteName = model.SiteName;
            _ApiEndpoint = model.ApiEndpoint;
            _LastAccessTime = model.LastAccessTime;
            _Controller = controller;
            AccountProfile = new AccountProfileViewModel(this, model);
        }
    }
}
