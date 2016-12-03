using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using WikiClientLibrary.Generators;

namespace WikiEdit.ViewModels.Primitives
{
    internal class RecentChangesFilterViewModel : BindableBase
    {

        private bool? _ShowMinor;

        public bool? ShowMinor
        {
            get { return _ShowMinor; }
            set { SetProperty(ref _ShowMinor, value); }
        }


        private bool? _ShowBots;

        public bool? ShowBots
        {
            get { return _ShowBots; }
            set { SetProperty(ref _ShowBots, value); }
        }

        private bool? _ShowAnonymous;

        public bool? ShowAnonymous
        {
            get { return _ShowAnonymous; }
            set { SetProperty(ref _ShowAnonymous, value); }
        }

        private bool? _ShowPatrolled;

        public bool? ShowPatrolled
        {
            get { return _ShowPatrolled; }
            set { SetProperty(ref _ShowPatrolled, value); }
        }


        private bool? _ShowMyEdits;

        public bool? ShowMyEdits
        {
            get { return _ShowMyEdits; }
            set { SetProperty(ref _ShowMyEdits, value); }
        }

        private static PropertyFilterOption ToFilterOption(bool? value)
        {
            if (value == null) return PropertyFilterOption.Disable;
            if (value == true) return PropertyFilterOption.WithProperty;
            return PropertyFilterOption.WithoutProperty;
        }

        public void ConfigureGenerator(RecentChangesGenerator generator)
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            generator.MinorFilter = ToFilterOption(ShowMinor);
            generator.BotFilter = ToFilterOption(ShowBots);
            generator.AnonymousFilter = ToFilterOption(ShowAnonymous);
            generator.PatrolledFilter = ToFilterOption(ShowPatrolled);
            generator.ExcludedUserName = null;
            generator.UserName = null;
            if (ShowMyEdits == true)
                generator.UserName = generator.Site.UserInfo.Name;
            else if (ShowMyEdits == false)
                generator.ExcludedUserName = generator.Site.UserInfo.Name;
        }
    }
}
