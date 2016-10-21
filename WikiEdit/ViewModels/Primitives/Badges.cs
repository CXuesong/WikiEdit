using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;

namespace WikiEdit.ViewModels.Primitives
{
    /// <summary>
    /// Represents a badge of page title.
    /// E.g. Title [diff] [hist]
    /// </summary>
    internal class PageTitleViewModel : BindableBase
    {
        private string _Title;
        private readonly Action TitleCommandHandler, DiffCommandHandler, HistoryCommandHandler;

        public PageTitleViewModel(string title, Action titleCommandHandler, Action diffCommandHandler, Action historyCommandHandler)
        {
            _Title = title;
            TitleCommandHandler = titleCommandHandler;
            DiffCommandHandler = diffCommandHandler;
            HistoryCommandHandler = historyCommandHandler;
        }

        /// <summary>
        /// Title of the page.
        /// </summary>
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }

        #region Commands

        private DelegateCommand _TitleCommand;

        public DelegateCommand TitleCommand
        {
            get
            {
                if (_TitleCommand == null)
                {
                    _TitleCommand = new DelegateCommand(() => TitleCommandHandler(), () => TitleCommandHandler != null);
                }
                return _TitleCommand;
            }
        }

        private DelegateCommand _DiffCommand;

        public DelegateCommand DiffCommand
        {
            get
            {
                if (_DiffCommand == null)
                {
                    _DiffCommand = new DelegateCommand(() => DiffCommandHandler(), () => DiffCommandHandler != null);
                }
                return _DiffCommand;
            }
        }

        private DelegateCommand _HistoryCommand;

        public DelegateCommand HistoryCommand
        {
            get
            {
                if (_HistoryCommand == null)
                {
                    _HistoryCommand = new DelegateCommand(() => HistoryCommandHandler(), () => HistoryCommandHandler != null);
                }
                return _HistoryCommand;
            }
        }

        #endregion
      }

    /// <summary>
    /// Represents a badge of user name.
    /// E.g. User [talk] [contribs] [block]
    /// </summary>
    internal class UserNameViewModel : BindableBase
    {

        private string _UserName;
        private readonly Action UserNameCommandHandler, TalkCommandHandler, ContributionsCommandHandler, BlockCommandHandler;

        public UserNameViewModel(string userName, Action userNameCommandHandler, Action talkCommandHandler, Action contributionsCommandHandler, Action blockCommandHandler)
        {
            _UserName = userName;
            UserNameCommandHandler = userNameCommandHandler;
            TalkCommandHandler = talkCommandHandler;
            ContributionsCommandHandler = contributionsCommandHandler;
            BlockCommandHandler = blockCommandHandler;
        }

        /// <summary>
        /// User name.
        /// </summary>
        public string UserName
        {
            get { return _UserName; }
            set { SetProperty(ref _UserName, value); }
        }

        #region Commands

        private DelegateCommand _UserNameCommand;

        public DelegateCommand UserNameCommand
        {
            get
            {
                if (_UserNameCommand == null)
                {
                    _UserNameCommand = new DelegateCommand(() => UserNameCommandHandler(), () => UserNameCommandHandler != null);
                }
                return _UserNameCommand;
            }
        }

        private DelegateCommand _TalkCommand;

        public DelegateCommand TalkCommand
        {
            get
            {
                if (_TalkCommand == null)
                {
                    _TalkCommand = new DelegateCommand(() => TalkCommandHandler(), () => TalkCommandHandler != null);
                }
                return _TalkCommand;
            }
        }

        private DelegateCommand _ContributionsCommand;

        public DelegateCommand ContributionsCommand
        {
            get
            {
                if (_ContributionsCommand == null)
                {
                    _ContributionsCommand = new DelegateCommand(() => ContributionsCommandHandler(), () => ContributionsCommandHandler != null);
                }
                return _ContributionsCommand;
            }
        }

        private DelegateCommand _BlockCommand;

        public DelegateCommand BlockCommand
        {
            get
            {
                if (_BlockCommand == null)
                {
                    _BlockCommand = new DelegateCommand(() => BlockCommandHandler(), () => BlockCommandHandler != null);
                }
                return _BlockCommand;
            }
        }



        #endregion
    }
}
