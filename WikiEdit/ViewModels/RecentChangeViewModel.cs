using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Prism.Commands;
using Prism.Mvvm;
using Unclassified.TxLib;
using WikiClientLibrary;
using WikiEdit.Services;
using WikiEdit.ViewModels.Primitives;

namespace WikiEdit.ViewModels
{
    /// <summary>
    /// Represents an entry of Recent Changes list.
    /// </summary>
    public class RecentChangeViewModel : BindableBase
    {
        private readonly IViewModelFactory _ViewModelFactory;

        public WikiSiteViewModel WikiSite { get; }

        public RecentChangesEntry RawEntry { get; }

        /// <summary>
        /// So that PropertyGroupDescription works.
        /// </summary>
        public DateTime TimeStamp { get; }

        private bool _NeedPatrol;

        public bool NeedPatrol
        {
            get { return _NeedPatrol; }
            set { SetProperty(ref _NeedPatrol, value); }
        }

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
        /// Target page title.
        /// </summary>
        public string TargetTitle => RawEntry.Title;

        /// <summary>
        /// Revision ids used to show diff.
        /// </summary>
        public Tuple<int, int> DiffRevisionIds { get; }

        public int DeltaContentLength => RawEntry.NewContentLength - RawEntry.OldContentLength;

        /// <summary>
        /// For TextBlock Formatting.
        /// </summary>
        public int DeltaContentLengthSign => Math.Sign(DeltaContentLength);

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
                        if (!NeedPatrol) return;
                        IsBusy = true;
                        Status = Tx.T("please wait");
                        try
                        {
                            await RawEntry.PatrolAsync();
                            NeedPatrol = false;
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


        private DelegateCommand<string> _OpenWikiLinkCommand;

        public DelegateCommand<string> OpenWikiLinkCommand
        {
            get
            {
                if (_OpenWikiLinkCommand == null)
                {
                    _OpenWikiLinkCommand = new DelegateCommand<string>(async param =>
                    {
                        var site = await WikiSite.GetSiteAsync();
                        var title = WikiLink.Parse(site, param);
                        if (title.Namespace.Id == BuiltInNamespaces.Special)
                            await _ViewModelFactory.OpenPageAsync(WikiSite, param);
                        else
                            await _ViewModelFactory.OpenPageEditorAsync(WikiSite, param);
                    });
                }
                return _OpenWikiLinkCommand;
            }
        }


        private DelegateCommand _OpenDiffCommand;

        public DelegateCommand OpenDiffCommand
        {
            get
            {
                // Issue: If we declare OpenDiffCommand as DelegateCommand<Tuple<int,int>>, and
                // pass the revids as Tuple, the first call to CanExecute will pass null to the CanExecuteMethod,
                // regardless of what DiffRevisionIds actually is.
                // <Hyperlink Command="{Binding OpenDiffCommand}" CommandParameter="{Binding DiffRevisionIds}">
                if (DiffRevisionIds != null && _OpenDiffCommand == null)
                {
                    _OpenDiffCommand = new DelegateCommand(() =>
                    {
                        _ViewModelFactory.OpenPageDiffViewModel(WikiSite, DiffRevisionIds.Item1, DiffRevisionIds.Item2);
                    });
                }
                return _OpenDiffCommand;
            }
        }

        #endregion

        public string Summary { get; }

        /// <inheritdoc />
        protected override void OnPropertyChanged(string propertyName = null)
        {
            if (propertyName == nameof(IsBusy))
                _PatrolCommand?.RaiseCanExecuteChanged();
            base.OnPropertyChanged(propertyName);
        }

        internal RecentChangeViewModel(IViewModelFactory viewModelFactory, 
            WikiSiteViewModel wikiSite, RecentChangesEntry model)
        {
            if (viewModelFactory == null) throw new ArgumentNullException(nameof(viewModelFactory));
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            if (model == null) throw new ArgumentNullException(nameof(model));
            _ViewModelFactory = viewModelFactory;
            WikiSite = wikiSite;
            RawEntry = model;
            TimeStamp = model.TimeStamp.ToLocalTime();
            NeedPatrol = RawEntry.PatrolStatus == PatrolStatus.Unpatrolled;
            if (model.OldRevisionId > 0 && model.RevisionId > 0)
            {
                DiffRevisionIds = Tuple.Create(model.OldRevisionId, model.RevisionId);
            }
            var sb = new StringBuilder();
            switch (model.Type)
            {
                case RecentChangesType.Create:
                case RecentChangesType.Edit:
                case RecentChangesType.Move:
                    break;
                case RecentChangesType.Log:
                    sb.Append(Tx.SafeText("logactions:" + model.LogAction));
                    sb.Append(" ");
                    break;
                case RecentChangesType.Categorize:
                    sb.Append("Categorize");
                    sb.Append(" ");
                    break;
                case RecentChangesType.External:
                    sb.Append(Tx.T("rctypes:external"));
                    sb.Append(" ");
                    break;
                default:
                    break;
            }
            if (model.Comment != null)
                sb.Append(model.Comment);
            Summary = sb.ToString();
        }
    }
}
