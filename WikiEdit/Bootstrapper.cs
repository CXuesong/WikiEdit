using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Practices.Unity;
using Prism.Unity;
using Unclassified.TxLib;
using WikiEdit.Controllers;
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
            var weController = new WikiEditController();
            Container.RegisterInstance(weController);
            Container.RegisterType<MainWindow>(new ContainerControlledLifetimeManager());
            Container.RegisterType<MainWindowViewModel>(new ContainerControlledLifetimeManager());
#if DEBUG
            weController.FillDemo();
            var vm = Container.Resolve<MainWindowViewModel>();
            vm.DocumentViewModels.Add(new WikiSiteOverviewViewModel(weController.WikiSites[0]));
#endif
        }

        protected override DependencyObject CreateShell()
        {
            Tx.LoadFromXmlFile("WikiEdit.txd");
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            Application.Current.MainWindow = (Window) Shell;
            Application.Current.MainWindow.Show();
        }
    }
}
