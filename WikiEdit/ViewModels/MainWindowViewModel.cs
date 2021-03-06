﻿using System;
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
using WikiEdit.Services;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit.ViewModels
{
    internal class MainWindowViewModel : BindableBase
    {
        private readonly WikiEditSessionService sessionService;
        private readonly IChildViewModelService _ChildViewModelService;
        private readonly IViewModelFactory _ViewModelFactory;

        [Dependency]
        public WikiSiteListViewModel WikiSiteListViewModel { get; set; }

        [Dependency]
        public DocumentOutlineViewModel DocumentOutlineViewModel { get; set; }

        [Dependency]
        public SessionInformationViewModel SessionInformationViewModel { get; set; }

        public ObservableCollection<DocumentViewModel> DocumentViewModels => _ChildViewModelService.Documents;

        public MainWindowViewModel(WikiEditSessionService sessionService,
            IChildViewModelService childViewModelService,
            IEventAggregator eventAggregator, IViewModelFactory viewModelFactory)
        {
            if (sessionService == null) throw new ArgumentNullException(nameof(sessionService));
            if (childViewModelService == null) throw new ArgumentNullException(nameof(childViewModelService));
            this.sessionService = sessionService;
            _ChildViewModelService = childViewModelService;
            _ViewModelFactory = viewModelFactory;
            eventAggregator.GetEvent<ActiveDocumentChangedEvent>().Subscribe(OnActiveDocumentChanged);
        }

        #region Wiki Site

        private WikiSiteViewModel _CurrentWikiSite;
        private bool _IsAccountProfileOpen;

        public WikiSiteViewModel CurrentWikiSite
        {
            get { return _CurrentWikiSite; }
            private set
            {
                if (SetProperty(ref _CurrentWikiSite, value))
                {
                    _ShowWikiSiteCommand?.RaiseCanExecuteChanged();
                    _ShowAccountProfileCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsAccountProfileOpen
        {
            get { return _IsAccountProfileOpen; }
            set { SetProperty(ref _IsAccountProfileOpen, value); }
        }


        private DelegateCommand _ShowWikiSiteCommand;

        public DelegateCommand ShowWikiSiteCommand
        {
            get
            {
                if (_ShowWikiSiteCommand == null)
                {
                    _ShowWikiSiteCommand = new DelegateCommand(() =>
                    {
                        if (CurrentWikiSite == null) return;
                        _ChildViewModelService.Documents.ActivateOrCreate(CurrentWikiSite,
                            () => _ViewModelFactory.CreateWikiSiteOverview(CurrentWikiSite));
                    }, () => CurrentWikiSite != null);
                }
                return _ShowWikiSiteCommand;
            }
        }

        private DelegateCommand _ShowAccountProfileCommand;

        public DelegateCommand ShowAccountProfileCommand
        {
            get
            {
                if (_ShowAccountProfileCommand == null)
                {
                    _ShowAccountProfileCommand = new DelegateCommand(() =>
                    {
                        if (CurrentWikiSite == null) IsAccountProfileOpen = false;
                        IsAccountProfileOpen = !IsAccountProfileOpen;
                    }, () => CurrentWikiSite != null);
                }
                return _ShowAccountProfileCommand;
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
                if (sessionService.PromptSave() && _ChildViewModelService.Documents.CloseAll())
                {
                    sessionService.Clear();
                }
            });
            addCommand("Open", () => sessionService.Open());
            addCommand("Save", () => sessionService.Save(false));
            addCommand("SaveAs", () => sessionService.Save(true));
        }

        #endregion

    }
}
