using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
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
        }

        protected override DependencyObject CreateShell()
        {
            Tx.LoadFromXmlFile("WikiEdit.txd");
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
#if DEBUG
            var weController = Container.Resolve<WikiEditController>();
            weController.FillDemo();
            Container.Resolve<IChildViewModelService>()
                .DocumentViewModels.Add(new WikiSiteOverviewViewModel(Container.Resolve<IEventAggregator>(),
                    weController.WikiSites[0]));
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
    }
}
