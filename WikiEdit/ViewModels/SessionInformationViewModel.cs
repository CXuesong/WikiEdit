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
using WikiEdit.Models;
using WikiEdit.Services;

namespace WikiEdit.ViewModels
{
    /// <summary>
    /// The Information tab view of the Backstage menu.
    /// </summary>
    internal class SessionInformationViewModel : BindableBase
    {

        public SessionInformationViewModel(WikiEditSessionService sessionService)
        {
            if (sessionService == null) throw new ArgumentNullException(nameof(sessionService));
            SessionService = sessionService;
            PropertyChangedEventManager.AddHandler(sessionService, Controller_PropertyChanged, nameof(sessionService.FileName));
            using (var s = Application.GetResourceStream(GlobalConfigurations.SourceControlVersionUri).Stream)
            using (var r = new StreamReader(s))
                SourceControlInformation = r.ReadToEnd();
        }

        private void Controller_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SessionService.FileName))
            {
                OnPropertyChanged(nameof(SessionFileName));
                _OpenContainingFolderCommand?.RaiseCanExecuteChanged();
            }
        }

        public WikiEditSessionService SessionService { get; }

        public string SessionFileName
            => string.IsNullOrEmpty(SessionService.FileName) ? Tx.T("session:unsaved") : SessionService.FileName;

        private DelegateCommand _OpenContainingFolderCommand;

        public DelegateCommand OpenContainingFolderCommand
        {
            get
            {
                if (_OpenContainingFolderCommand == null)
                {
                    _OpenContainingFolderCommand = new DelegateCommand(() =>
                    {
                        if (string.IsNullOrEmpty(SessionService.FileName)) return;
                        var folder = Path.GetDirectoryName(SessionService.FileName);
                        var fileName = Path.GetFileName(SessionService.FileName);
                        using (var proc = Process.Start("Explorer.exe", $"\"{folder}\" /select \"{fileName}\""))
                        {
                            
                        }
                    },() => !string.IsNullOrEmpty(SessionService.FileName));
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
