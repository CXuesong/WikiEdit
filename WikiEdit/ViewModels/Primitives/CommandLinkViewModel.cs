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
    /// The ViewModel for a command link.
    /// </summary>
    internal class CommandLinkViewModel : BindableBase
    {
        private string _Text;

        public CommandLinkViewModel(Action commandHandler) : this(null, commandHandler)
        {
        }

        public CommandLinkViewModel(string text, Action commandHandler)
        {
            if (commandHandler == null) throw new ArgumentNullException(nameof(commandHandler));
            _Text = text;
            CommandHandler = commandHandler;
        }

        public string Text
        {
            get { return _Text; }
            set { SetProperty(ref _Text, value); }
        }

        private DelegateCommand _Command;

        public DelegateCommand Command
        {
            get
            {
                if (_Command == null)
                {
                    _Command = new DelegateCommand(CommandHandler);
                }
                return _Command;
            }
        }

        public Action CommandHandler { get; }

    }
}
