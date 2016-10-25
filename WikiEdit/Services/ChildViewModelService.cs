using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Prism.Events;
using Prism.Mvvm;
using WikiClientLibrary;
using WikiEdit.ViewModels;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit.Services
{
    public interface IChildViewModelService
    {
        DocumentViewModelCollection Documents { get; }

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
            if (eventAggregator == null) throw new ArgumentNullException(nameof(eventAggregator));
            Documents = new DocumentViewModelCollection(this);
            activeDocumentChangedEvent = eventAggregator.GetEvent<ActiveDocumentChangedEvent>();
        }

        public DocumentViewModelCollection Documents { get; }

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

    public class DocumentViewModelCollection : ObservableCollection<DocumentViewModel>
    {
        private readonly ChildViewModelService owner;

        internal DocumentViewModelCollection(ChildViewModelService owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            this.owner = owner;
        }

        /// <summary>
        /// Adds a document into the collection and activates it.
        /// </summary>
        public void AddAndActivate(DocumentViewModel newItem)
        {
            if (newItem == null) throw new ArgumentNullException(nameof(newItem));
            Add(newItem);
            newItem.IsActive = true;
        }

        /// <summary>
        /// Get an item with the specified <see cref="DocumentViewModel.DocumentContext"/>,
        /// or create and add a new item.
        /// </summary>
        public T ActivateOrCreate<T>(object contextSource, Func<T> vmFactory)
            where T : DocumentViewModel
        {
            if (contextSource == null) throw new ArgumentNullException(nameof(contextSource));
            if (vmFactory == null) throw new ArgumentNullException(nameof(vmFactory));
            var vm = this.OfType<T>().FirstOrDefault(vm1 => vm1.DocumentContext == contextSource);
            if (vm == null)
            {
                vm = vmFactory();
                Add(vm);
            }
            vm.IsActive = true;
            return vm;
        }

        /// <summary>
        /// Tries to close all documents that satisfy the predicate.
        /// </summary>
        /// <param name="predicate">A predicate, or <n>null</n> to close all the documents.</param>
        public bool CloseAll(Predicate<DocumentViewModel> predicate)
        {
            var docs = Items.ToArray();
            foreach (var d in docs)
            {
                if (predicate == null || predicate(d))
                {
                    if (!d.Close()) return false;
                }
            }
            Debug.Assert(predicate != null || Items.Count == 0);
            return true;
        }

        /// <summary>
        /// Tries to close all the documents.
        /// </summary>
        public bool CloseAll()
        {
            return CloseAll(null);
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
