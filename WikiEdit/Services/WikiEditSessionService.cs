using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Prism.Events;
using Prism.Mvvm;
using Unclassified.TxLib;
using WikiClientLibrary;
using WikiClientLibrary.Client;
using WikiEdit.Models;
using WikiEdit.ViewModels;

namespace WikiEdit.Services
{
    /// <summary>
    /// A running wiki editing session.
    /// </summary>
    internal class WikiEditSessionService : BindableBase
    {
        private readonly IEventAggregator _EventAggregator;
        private readonly IChildViewModelService _ChildViewModelService;


        public WikiClient WikiClient { get; private set; }

        public ObservableCollection<WikiSiteViewModel> WikiSites { get; } = new ObservableCollection<WikiSiteViewModel>();

        internal async Task<Site> CreateSiteAsync(string apiEndpoint)
        {
            if (apiEndpoint == null) throw new ArgumentNullException(nameof(apiEndpoint));
            var site = await Site.CreateAsync(WikiClient, apiEndpoint);
            return site;
        }

        private void ResetWikiClient()
        {
            WikiClient = new WikiClient
            {
                ClientUserAgent = $"WikiEdit/1.0 ({Environment.OSVersion};.NET CLR {Environment.Version})",
                Logger = TraceLogger.Default,
            };
        }

        #region Session Persistence

        private WikiEditSession storage = new WikiEditSession();

        private static readonly JsonSerializer StorageSerializer = new JsonSerializer
        {
            Converters = {new CookieContainerJsonConverter()},
            // TraceWriter = new DiagnosticsTraceWriter() {LevelFilter = TraceLevel.Verbose},
        };

        /// <summary>
        /// Clear current wiki session.
        /// </summary>
        public void Clear()
        {
            WikiSites.Clear();
            FileName = null;
            ResetWikiClient();
        }

        /// <summary>
        /// Populates the controller with demo settings.
        /// </summary>
        public void FillDemo()
        {
            var newItems = new[]
            {
                new WikiSiteViewModel(_EventAggregator, this)
                {
                    Name = "Test2 Wikipedia",
                    ApiEndpoint = "https://test2.wikipedia.org/w/api.php",
                },
                new WikiSiteViewModel(_EventAggregator, this)
                {
                    Name = "EN Wikipedia",
                    ApiEndpoint = "https://en.wikipedia.org/w/api.php",
                },
                new WikiSiteViewModel(_EventAggregator, this)
                {
                    Name = "FR Wikipedia",
                    ApiEndpoint = "https://fr.wikipedia.org/w/api.php",
                },
                new WikiSiteViewModel(_EventAggregator, this)
                {
                    Name = "JA Wikipedia",
                    ApiEndpoint = "https://ja.wikipedia.org/w/api.php",
                },
                new WikiSiteViewModel(_EventAggregator, this)
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
            storage.WikiSites = WikiSites.Select(s => s.GetModel()).ToArray();
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
            WikiSites.AddRange(storage.WikiSites.Select(s => new WikiSiteViewModel(_EventAggregator, this, s)));
        }

        #endregion

        #region Session Persistence - Storage

        private string _FileName;

        public string FileName
        {
            get { return _FileName; }
            set { SetProperty(ref _FileName, value); }
        }

        public bool Open()
        {
            if (!PromptSave()) return false;
            if (!_ChildViewModelService.Documents.CloseAll()) return false;
            var ofd = new OpenFileDialog
            {
                Filter = Tx.T("session file filter"),
            };
            if (ofd.ShowDialog() == true)
            {
                Load(ofd.FileName);
                FileName = ofd.FileName;
                return true;
            }
            return false;
        }

        public bool PromptSave()
        {
            switch (Utility.Confirm(Tx.T("save session prompt"), true))
            {
                case true:
                    return Save(false);
                case false:
                    return true;
                default:
                    return false;
            }
        }

        public bool Save(bool saveAs)
        {
            var fn = FileName;
            if (saveAs || fn == null)
            {
                var sfd = new SaveFileDialog
                {
                    Filter = Tx.T("session file filter"),
                };
                if (sfd.ShowDialog() == true)
                    fn = sfd.FileName;
                else
                    return false;
            }
            try
            {
                Save(fn);
                return true;
            }
            catch (Exception ex)
            {
                Utility.ReportException(ex);
                return false;
            }
        }

        #endregion

        public WikiEditSessionService(IEventAggregator eventAggregator,
            IChildViewModelService childViewModelService)
        {
            if (eventAggregator == null) throw new ArgumentNullException(nameof(eventAggregator));
            _EventAggregator = eventAggregator;
            _ChildViewModelService = childViewModelService;
            Clear();
        }
    }
}
