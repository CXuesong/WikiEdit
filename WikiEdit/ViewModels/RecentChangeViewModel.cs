using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Unclassified.TxLib;
using WikiClientLibrary;
using WikiEdit.Services;
using WikiEdit.ViewModels.Primitives;

namespace WikiEdit.ViewModels
{
    public class RecentChangeViewModel : BindableBase
    {
        private readonly IViewModelFactory _ViewModelFactory;

        public RecentChangesEntry RawEntry { get; }

        public bool NeedPatrol => RawEntry.PatrolStatus == PatrolStatus.Unpatrolled;

        private bool _IsBusy;

        public bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                if (SetProperty(ref _IsBusy, value))
                _PatrolCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _Status;

        public string Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }

        public bool IsNew => (RawEntry.Flags & RevisionFlags.Create) == RevisionFlags.Create;

        public bool IsMinor => (RawEntry.Flags & RevisionFlags.Minor) == RevisionFlags.Minor;

        public bool IsBot => (RawEntry.Flags & RevisionFlags.Minor) == RevisionFlags.Bot;

        /// <summary>
        /// The view model for the target page or log type of the recent change.
        /// </summary>
        public BindableBase TargetBadge { get; }

        public UserNameViewModel UserNameBadge { get; }

        #region Commands

        private DelegateCommand _PatrolCommand;

        public DelegateCommand PatrolCommand
        {
            get
            {
                if (_PatrolCommand == null)
                {
                    _PatrolCommand = new DelegateCommand(async () =>
                    {
                        if (IsBusy) return;
                        if (RawEntry.PatrolStatus != PatrolStatus.Unpatrolled) return;
                        Status = Tx.T("please wait");
                        try
                        {
                            await RawEntry.PatrolAsync();
                            OnPropertyChanged(nameof(NeedPatrol));
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
                    }, () => !IsBusy);
                }
                return _PatrolCommand;
            }
        }

        #endregion

        internal RecentChangeViewModel(IViewModelFactory viewModelFactory, 
            WikiSiteViewModel wikiSite, RecentChangesEntry model)
        {
            if (viewModelFactory == null) throw new ArgumentNullException(nameof(viewModelFactory));
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            if (model == null) throw new ArgumentNullException(nameof(model));
            _ViewModelFactory = viewModelFactory;
            RawEntry = model;
            switch (model.Type)
            {
                case RecentChangesType.Create:
                case RecentChangesType.Edit:
                case RecentChangesType.Move:
                    TargetBadge = new PageTitleViewModel(model.Title, () =>
                    {
                        _ViewModelFactory.OpenPageEditorAsync(wikiSite, model.Title);
                    }, null, null);
                    break;
                case RecentChangesType.Log:
                    TargetBadge = new CommandLinkViewModel(model.LogAction, () => { });
                    break;
                case RecentChangesType.Categorize:
                    break;
                case RecentChangesType.External:
                    break;
                default:
                    break;
            }
            if (model.UserName != null)
            {
                UserNameBadge = new UserNameViewModel(model.UserName, null, null, null, null);
            }
        }
    }
}
