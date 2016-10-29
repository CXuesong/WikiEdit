using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using Unclassified.TxLib;
using WikiEdit.Controllers;
using WikiEdit.Models;

namespace WikiEdit.ViewModels
{
    /// <summary>
    /// The Information tab view of the Backstage menu.
    /// </summary>
    internal class SessionInformationViewModel : BindableBase
    {

        public SessionInformationViewModel(WikiEditController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            Controller = controller;
            PropertyChangedEventManager.AddHandler(controller, Controller_PropertyChanged, nameof(controller.FileName));
            using (var s = Application.GetResourceStream(GlobalConfigurations.SourceControlVersionUri).Stream)
            using (var r = new StreamReader(s))
                SourceControlInformation = r.ReadToEnd();
        }

        private void Controller_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Controller.FileName))
            {
                OnPropertyChanged(nameof(SessionFileName));
                _OpenContainingFolderCommand?.RaiseCanExecuteChanged();
            }
        }

        public WikiEditController Controller { get; }

        public string SessionFileName
            => string.IsNullOrEmpty(Controller.FileName) ? Tx.T("session:unsaved") : Controller.FileName;

        private DelegateCommand _OpenContainingFolderCommand;

        public DelegateCommand OpenContainingFolderCommand
        {
            get
            {
                if (_OpenContainingFolderCommand == null)
                {
                    _OpenContainingFolderCommand = new DelegateCommand(() =>
                    {
                        if (string.IsNullOrEmpty(Controller.FileName)) return;
                        var folder = Path.GetDirectoryName(Controller.FileName);
                        var fileName = Path.GetFileName(Controller.FileName);
                        using (var proc = Process.Start("Explorer.exe", $"\"{folder}\" /select \"{fileName}\""))
                        {
                            
                        }
                    },() => !string.IsNullOrEmpty(Controller.FileName));
                }
                return _OpenContainingFolderCommand;
            }
        }

        #region Application Information

        public string Version => Utility.ProductVersion.ToString();

        public string SourceControlInformation { get; }

        private DelegateCommand _OpenGitHubCommand;

        public DelegateCommand OpenGitHubCommand
        {
            get
            {
                if (_OpenGitHubCommand == null)
                {
                    _OpenGitHubCommand = new DelegateCommand(() =>
                    {
                        Utility.OpenUrl("http://github.com/CXuesong/WikiEdit");
                    });
                }
                return _OpenGitHubCommand;
            }
        }


        #endregion
    }
}
