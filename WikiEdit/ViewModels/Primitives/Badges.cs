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
        private readonly Action _TitleCommandHandler, _DiffCommandHandler, _HistoryCommandHandler;

        public PageTitleViewModel(string title, Action titleCommandHandler, Action diffCommandHandler, Action historyCommandHandler)
        {
            _Title = title;
            _TitleCommandHandler = titleCommandHandler;
            _DiffCommandHandler = diffCommandHandler;
            _HistoryCommandHandler = historyCommandHandler;
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
                    _TitleCommand = new DelegateCommand(() => _TitleCommandHandler(), () => _TitleCommandHandler != null);
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
                    _DiffCommand = new DelegateCommand(() => _DiffCommandHandler(), () => _DiffCommandHandler != null);
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
                    _HistoryCommand = new DelegateCommand(() => _HistoryCommandHandler(), () => _HistoryCommandHandler != null);
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
    public class UserNameViewModel : BindableBase
    {

        private string _UserName;
        private readonly Action _UserNameCommandHandler, _TalkCommandHandler, _ContributionsCommandHandler, _BlockCommandHandler;

        public UserNameViewModel(string userName, Action userNameCommandHandler, Action talkCommandHandler, Action contributionsCommandHandler, Action blockCommandHandler)
        {
            _UserName = userName;
            _UserNameCommandHandler = userNameCommandHandler;
            _TalkCommandHandler = talkCommandHandler;
            _ContributionsCommandHandler = contributionsCommandHandler;
            _BlockCommandHandler = blockCommandHandler;
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
                    _UserNameCommand = new DelegateCommand(() => _UserNameCommandHandler(), () => _UserNameCommandHandler != null);
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
                    _TalkCommand = new DelegateCommand(() => _TalkCommandHandler(), () => _TalkCommandHandler != null);
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
                    _ContributionsCommand = new DelegateCommand(() => _ContributionsCommandHandler(), () => _ContributionsCommandHandler != null);
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
                    _BlockCommand = new DelegateCommand(() => _BlockCommandHandler(), () => _BlockCommandHandler != null);
                }
                return _BlockCommand;
            }
        }



        #endregion
    }
}
