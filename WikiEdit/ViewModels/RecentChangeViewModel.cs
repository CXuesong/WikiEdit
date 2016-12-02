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

        public RecentChangesEntry RawEntry { get; }

        /// <summary>
        /// So that PropertyGroupDescription works.
        /// </summary>
        public DateTime TimeStamp { get; }

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
        /// Target page title.
        /// </summary>
        public string TargetTitle => RawEntry.Title;

        public int DeltaContentLength => RawEntry.NewContentLength - RawEntry.OldContentLength;

        /// <summary>
        /// For TextBlock Formatting.
        /// </summary>
        public int DeltaContentLengthSign => Math.Sign(DeltaContentLength);

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

        public string Summary { get; }

        private static Hyperlink NewHyperlink(string text, Action onClick)
        {
            var link = new Hyperlink(new Run(text));
            link.Click += (_, e) => onClick();
            return link;
        }

        private static Hyperlink NewHyperlink(string text, ICommand command)
        {
            var link = new Hyperlink(new Run(text)) {Command = command};
            return link;
        }

        internal RecentChangeViewModel(IViewModelFactory viewModelFactory, 
            WikiSiteViewModel wikiSite, RecentChangesEntry model)
        {
            if (viewModelFactory == null) throw new ArgumentNullException(nameof(viewModelFactory));
            if (wikiSite == null) throw new ArgumentNullException(nameof(wikiSite));
            if (model == null) throw new ArgumentNullException(nameof(model));
            _ViewModelFactory = viewModelFactory;
            RawEntry = model;
            TimeStamp = model.TimeStamp.ToLocalTime();
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
