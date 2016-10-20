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

        public ObservableCollection<DocumentViewModel> DocumentViewModels => childVmService.DocumentViewModels;

        public MainWindowViewModel(WikiEditController wikiEditController,
            IChildViewModelService childVmService)
        {
            if (wikiEditController == null) throw new ArgumentNullException(nameof(wikiEditController));
            if (childVmService == null) throw new ArgumentNullException(nameof(childVmService));
            this.wikiEditController = wikiEditController;
            this.childVmService = childVmService;
        }

        #region Session Persistence

        public string FileName
        {
            get { return _FileName; }
            set { SetProperty(ref _FileName, value); }
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
            addCommand("Open", () =>
            {
                if (PromptSaveSession())
                {
                    wikiEditController.Clear();
                    FileName = null;
                }
            });
            addCommand("Save", () => SaveSession());
            addCommand("SaveAs", () => SaveSession(true));
        }

        #endregion

    }
}
