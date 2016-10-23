using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Unclassified.TxLib;
using WikiEdit.Controllers;
using WikiEdit.Services;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit.ViewModels
{
    internal class MainWindowViewModel : BindableBase
    {
        private readonly WikiEditController wikiEditController;
        private string _FileName;
        private readonly IChildViewModelService childVmService;

        [Dependency]
        public WikiSiteListViewModel WikiSiteListViewModel { get; set; }

        public ObservableCollection<DocumentViewModel> DocumentViewModels => childVmService.Documents;

        public MainWindowViewModel(WikiEditController wikiEditController,
            IChildViewModelService childVmService,
            IEventAggregator eventAggregator)
        {
            if (wikiEditController == null) throw new ArgumentNullException(nameof(wikiEditController));
            if (childVmService == null) throw new ArgumentNullException(nameof(childVmService));
            this.wikiEditController = wikiEditController;
            this.childVmService = childVmService;
            eventAggregator.GetEvent<ActiveDocumentChangedEvent>().Subscribe(OnActiveDocumentChanged);
        }

        #region Wiki Site

        private WikiSiteViewModel _CurrentWikiSite;
        private bool _IsAccountProfileOpen;

        public WikiSiteViewModel CurrentWikiSite
        {
            get { return _CurrentWikiSite; }
            private set { SetProperty(ref _CurrentWikiSite, value); }
        }

        public bool IsAccountProfileOpen
        {
            get { return _IsAccountProfileOpen; }
            set { SetProperty(ref _IsAccountProfileOpen, value); }
        }

        private DelegateCommand _ShowAccountProfile;

        public DelegateCommand ShowAccountProfile
        {
            get
            {
                if (_ShowAccountProfile == null)
                {
                    _ShowAccountProfile = new DelegateCommand(() =>
                    {
                        if (CurrentWikiSite == null) IsAccountProfileOpen = false;
                        IsAccountProfileOpen = !IsAccountProfileOpen;
                    });
                }
                return _ShowAccountProfile;
            }
        }

        #endregion


        private DocumentViewModel _ActiveDocument;

        public DocumentViewModel ActiveDocument
        {
            get { return _ActiveDocument; }
            private set { SetProperty(ref _ActiveDocument, value); }
        }


        private void OnActiveDocumentChanged(DocumentViewModel activeDocument)
        {
            ActiveDocument = activeDocument;
            // Track the active wiki site.
            CurrentWikiSite = activeDocument?.SiteContext;
        }

        #region Session Persistence

        public string FileName
        {
            get { return _FileName; }
            set { SetProperty(ref _FileName, value); }
        }

        public bool OpenSession()
        {
            if (!PromptSaveSession()) return false;
            var ofd = new OpenFileDialog
            {
                Filter = Tx.T("session file filter"),
            };
            if (ofd.ShowDialog() == true)
            {
                wikiEditController.Load(ofd.FileName);
                FileName = ofd.FileName;
                return true;
            }
            return false;
        }

        public bool PromptSaveSession()
        {
            switch (Utility.Confirm(Tx.T("save session prompt"), true))
            {
                case true:
                    return SaveSession();
                case false:
                    return true;
                default:
                    return false;
            }
        }

        public bool SaveSession(bool saveAs = false)
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
                wikiEditController.Save(fn);
                return true;
            }
            catch (Exception ex)
            {
                Utility.ReportException(ex);
                return false;
            }
        }

        #endregion

        #region Commands

        private Dictionary<string, ICommand> _Commands;

        public Dictionary<string, ICommand> Commands
        {
            get
            {
                if (_Commands == null) BuildCommands();
                return _Commands;
            }
        }

        private void BuildCommands()
        {
            _Commands = new Dictionary<string, ICommand>();
            Action<string, Action> addCommand = (key, act) =>
                    _Commands.Add(key, new DelegateCommand(act));
            Action<string, Action, Func<bool>> addCommandE = (key, act, enabled) =>
                    _Commands.Add(key, new DelegateCommand(act, enabled));
            addCommand("New", () =>
            {
                if (PromptSaveSession())
                {
                    wikiEditController.Clear();
                    FileName = null;
                }
            });
            addCommand("Open", () => OpenSession());
            addCommand("Save", () => SaveSession());
            addCommand("SaveAs", () => SaveSession(true));
        }

        #endregion

    }
}
