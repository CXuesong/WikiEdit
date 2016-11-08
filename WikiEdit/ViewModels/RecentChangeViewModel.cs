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

        public FlowDocument DetailDocument { get; } = new FlowDocument();

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
            var detailp = new Paragraph();
            DetailDocument.Blocks.Add(detailp);
            switch (model.Type)
            {
                case RecentChangesType.Create:
                case RecentChangesType.Edit:
                case RecentChangesType.Move:
                    break;
                case RecentChangesType.Log:
                    detailp.Inlines.Add(Tx.SafeText("logactions:" + model.LogAction));
                    detailp.Inlines.Add(" ");
                    break;
                case RecentChangesType.Categorize:
                    detailp.Inlines.Add("Categorize");
                    detailp.Inlines.Add(" ");
                    break;
                case RecentChangesType.External:
                    detailp.Inlines.Add(Tx.T("rctypes:external"));
                    detailp.Inlines.Add(" ");
                    break;
                default:
                    break;
            }
            if (model.Title != null)
            {
                detailp.Inlines.Add(NewHyperlink(model.Title, () =>
                {
                    _ViewModelFactory.OpenPageEditorAsync(wikiSite, model.Title);
                }));
                if (model.OldRevisionId > 0)
                {
                    detailp.Inlines.Add(" ");
                    detailp.Inlines.Add(NewHyperlink(Tx.T("badges:diff"), () =>
                    {
                        _ViewModelFactory.OpenPageDiffViewModel(wikiSite, model.OldRevisionId, model.RevisionId);
                    }));
                    detailp.Inlines.Add(" ");
                    var deltaLengthRun = new Run(Tx.DataSize(Math.Abs(model.NewContentLength - model.OldContentLength)));
                    if (model.NewContentLength > model.OldContentLength)
                    {
                        deltaLengthRun.Foreground = Brushes.Green;
                        deltaLengthRun.FontWeight = FontWeights.Bold;
                        deltaLengthRun.Text = "+" + deltaLengthRun.Text;
                    } else if (model.NewContentLength < model.OldContentLength)
                    {
                        deltaLengthRun.Foreground = Brushes.Red;
                        deltaLengthRun.FontWeight = FontWeights.Bold;
                        deltaLengthRun.Text = "-" + deltaLengthRun.Text;
                    }
                    detailp.Inlines.Add(deltaLengthRun);
                }
            }
            if (model.UserName != null)
            {
                detailp.Inlines.Add(" ");
                detailp.Inlines.Add(NewHyperlink(model.UserName,
                    () => _ViewModelFactory.OpenPageAsync(wikiSite, "User:" + model.UserName)));
                detailp.Inlines.Add(" (");
                detailp.Inlines.Add(NewHyperlink(Tx.T("badges:talk"),
                    () => _ViewModelFactory.OpenPageAsync(wikiSite, "User_talk:" + model.UserName)));
                detailp.Inlines.Add(" ");
                detailp.Inlines.Add(NewHyperlink(Tx.T("badges:contribs"),
                    () => _ViewModelFactory.OpenPageAsync(wikiSite, "Special:Contributions/" + model.UserName)));
                detailp.Inlines.Add(")");
            }
            if (model.Comment != null)
            {
                detailp.Inlines.Add(" ");
                detailp.Inlines.Add(model.Comment);
            }
        }
    }
}
