using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Practices.Unity;
using Prism.Events;
using Prism.Unity;
using Unclassified.TxLib;
using WikiEdit.Controllers;
using WikiEdit.Services;
using WikiEdit.ViewModels;
using WikiEdit.ViewModels.Documents;
using WikiEdit.Views;

namespace WikiEdit
{
    internal class Bootstrapper : UnityBootstrapper
    {
        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();
            // DI configuration
            Container.RegisterType<WikiEditController>(new ContainerControlledLifetimeManager());
            Container.RegisterType<MainWindow>(new ContainerControlledLifetimeManager());
            Container.RegisterType<MainWindowViewModel>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IChildViewModelService, ChildViewModelService>(
                new ContainerControlledLifetimeManager());
            Container.RegisterType<SettingsService>(new ContainerControlledLifetimeManager());
        }

        protected override DependencyObject CreateShell()
        {
            // Let's do other initializations here.
            var settings = Container.Resolve<SettingsService>();
            settings.Load();
            Tx.LoadFromXmlFile(GlobalConfigurations.TranslationDictionaryFile);
            LoadSyntaxHighlighters();
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
#if DEBUG
            var weController = Container.Resolve<WikiEditController>();
            weController.FillDemo();
            Container.Resolve<IChildViewModelService>()
                .Documents.Add(Container.Resolve<WikiSiteOverviewViewModel>(
                    new DependencyOverride<WikiSiteViewModel>(weController.WikiSites[0])));
#endif
            Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
            Application.Current.MainWindow = (Window) Shell;
            Application.Current.MainWindow.Show();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Utility.ReportException(e.Exception);
            e.Handled = true;
        }

        /// <summary>
        /// Loads and registers syntax highlighting rules from the folder.
        /// </summary>
        private void LoadSyntaxHighlighters()
        {
            foreach (var fileName in Directory.EnumerateFiles(GlobalConfigurations.SyntaxHighlighterDefinitionFolder, "*.xshd"))
            {
                var highLighterName = Path.GetFileName(fileName);
                try
                {
                    using (var s = File.OpenRead(fileName))
                    using (XmlReader reader = new XmlTextReader(s))
                    {
                        var def = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        highLighterName = def.Name;
                        HighlightingManager.Instance.RegisterHighlighting(def.Name,
                            def.Properties["FileExtensions"].Split('|'), def);
                    }
                }
                catch (Exception ex)
                {
                    Utility.ReportException(ex, Tx.T("errors.syntax highlight definition", "name", highLighterName));
                }
            }

        }
    }
}
