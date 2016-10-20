using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Prism.Mvvm;
using WikiClientLibrary;
using WikiClientLibrary.Client;
using WikiEdit.Models;
using WikiEdit.ViewModels;

namespace WikiEdit.Controllers
{
    /// <summary>
    /// A running wiki editing session.
    /// </summary>
    internal class WikiEditController : BindableBase
    {
        public WikiClient WikiClient { get; private set; }

        public ObservableCollection<WikiSiteViewModel> WikiSites { get; } = new ObservableCollection<WikiSiteViewModel>();

        public async Task<Site> CreateSiteAsync(string apiEndpoint)
        {
            if (apiEndpoint == null) throw new ArgumentNullException(nameof(apiEndpoint));
            var site = await Site.CreateAsync(WikiClient, apiEndpoint);
            return site;
        }

        private void ResetWikiClient()
        {
            WikiClient = new WikiClient
            {
                ClientUserAgent = $"WikiEdit/1.0 ({Environment.OSVersion};.NET CLR {Environment.Version})"
            };
        }

        #region Persistence

        private WikiEditSession storage = new WikiEditSession();

        private static readonly JsonSerializer StorageSerializer = new JsonSerializer
        {
            ContractResolver = new WikiEditSessionContractResolver(),
        };

        /// <summary>
        /// Clear current wiki session.
        /// </summary>
        public void Clear()
        {
            WikiSites.Clear();
            ResetWikiClient();
        }

        /// <summary>
        /// Populates the controller with demo settings.
        /// </summary>
        public void FillDemo()
        {
            var newItems = new[]
            {
                new WikiSiteViewModel(this)
                {
                    Name = "Test2 Wikipedia",
                    ApiEndpoint = "https://test2.wikipedia.org/w/api.php",
                },
                new WikiSiteViewModel(this)
                {
                    Name = "EN Wikipedia",
                    ApiEndpoint = "https://en.wikipedia.org/w/api.php",
                },
                new WikiSiteViewModel(this)
                {
                    Name = "FR Wikipedia",
                    ApiEndpoint = "https://fr.wikipedia.org/w/api.php",
                },
                new WikiSiteViewModel(this)
                {
                    Name = "JA Wikipedia",
                    ApiEndpoint = "https://ja.wikipedia.org/w/api.php",
                },
                new WikiSiteViewModel(this)
                {
                    Name = "ZH Wikipedia",
                    ApiEndpoint = "https://zh.wikipedia.org/w/api.php",
                }
            };
            foreach (var i in newItems)
            {
                i.LastAccessTime = DateTimeOffset.Now;
                i.AccountProfile.GetType().GetProperty("UserName").SetValue(i.AccountProfile, "0.0.0.0");
            }
            WikiSites.AddRange(newItems);
        }

        /// <summary>
        /// Save current wiki session to file.
        /// </summary>
        public void Save(string path)
        {
            if (storage == null) storage = new WikiEditSession();
            // Save settings
            storage.WikiSites = WikiSites.Select(s => s.ToModel()).ToArray();
            storage.SessionCookies = WikiClient.CookieContainer;
            // Persist
            using (var sw = new StreamWriter(path))
                StorageSerializer.Serialize(sw, storage);
        }

        /// <summary>
        /// Load current wiki session from file.
        /// </summary>
        public void Load(string path)
        {
            using (var sw = new StreamReader(path))
            using (var jr = new JsonTextReader(sw))
                storage = StorageSerializer.Deserialize<WikiEditSession>(jr);
            ResetWikiClient();
            WikiClient.CookieContainer = storage.SessionCookies ?? new CookieContainer();
            WikiSites.Clear();
            WikiSites.AddRange(storage.WikiSites.Select(s => new WikiSiteViewModel(s, this)));
        }

        #endregion

        public WikiEditController()
        {
            Clear();
        }
    }
}
