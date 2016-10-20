using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit.Services
{
    internal interface IChildViewModelService
    {
        DocumentViewModelCollection DocumentViewModels { get; }
    }

    /// <summary>
    /// Manages a collection of document views.
    /// </summary>
    internal class ChildViewModelService : IChildViewModelService
    {
        public DocumentViewModelCollection DocumentViewModels { get; } = new DocumentViewModelCollection();
    }

    internal class DocumentViewModelCollection : ObservableCollection<DocumentViewModel>
    {
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
            // Remove the document from the collection when closed.
            item.Closed += Item_Closed;
            base.InsertItem(index, item);
        }

        private void Item_Closed(object sender, EventArgs e)
        {
            var result = Remove((DocumentViewModel) sender);
            Debug.Assert(result);
        }
    }
}
