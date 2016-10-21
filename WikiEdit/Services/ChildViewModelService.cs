using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using Prism.Mvvm;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit.Services
{
    internal interface IChildViewModelService
    {
        DocumentViewModelCollection DocumentViewModels { get; }

        DocumentViewModel ActiveDocument { get; set; }
    }

    /// <summary>
    /// Manages a collection of document views.
    /// </summary>
    internal class ChildViewModelService : BindableBase, IChildViewModelService
    {
        private readonly ActiveDocumentChangedEvent activeDocumentChangedEvent;

        public ChildViewModelService(IEventAggregator eventAggregator)
        {
            DocumentViewModels = new DocumentViewModelCollection(this);
            activeDocumentChangedEvent = eventAggregator.GetEvent<ActiveDocumentChangedEvent>();
        }

        public DocumentViewModelCollection DocumentViewModels { get; }


        private DocumentViewModel _ActiveDocument;

        public DocumentViewModel ActiveDocument
        {
            get { return _ActiveDocument; }
            set
            {
                if (SetProperty(ref _ActiveDocument, value))
                {
                    if (value != null)
                        value.IsActive = true;
                    activeDocumentChangedEvent.Publish(value);
                }
            }
        }

    }

    internal class DocumentViewModelCollection : ObservableCollection<DocumentViewModel>
    {
        private readonly ChildViewModelService owner;

        public DocumentViewModelCollection(ChildViewModelService owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            this.owner = owner;
        }

        /// <summary>
        /// Get an item with the specified <see cref="DocumentViewModel.ContentSource"/>,
        /// or create and add a new item.
        /// </summary>
        public T GetOrCreate<T>(object contextSource, Func<T> vmFactory)
            where T : DocumentViewModel
        {
            if (contextSource == null) throw new ArgumentNullException(nameof(contextSource));
            if (vmFactory == null) throw new ArgumentNullException(nameof(vmFactory));
            var vm = this.OfType<T>().FirstOrDefault(vm1 => vm1.ContentSource == contextSource);
            if (vm == null)
            {
                vm = vmFactory();
                Add(vm);
            }
            return vm;
        }

        protected override void InsertItem(int index, DocumentViewModel item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            item.Activated += Item_Activated;
            // Remove the document from the collection when closed.
            item.Closed += Item_Closed;
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            if (Count == 0) owner.ActiveDocument = null;
        }

        private void Item_Activated(object sender, EventArgs e)
        {
            owner.ActiveDocument = (DocumentViewModel) sender;
        }

        private void Item_Closed(object sender, EventArgs e)
        {
            var result = Remove((DocumentViewModel) sender);
            Debug.Assert(result);
        }
    }
}
