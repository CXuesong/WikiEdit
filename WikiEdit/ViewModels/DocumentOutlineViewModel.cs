using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using Prism.Mvvm;
using WikiEdit.Services;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit.ViewModels
{
    public class DocumentOutlineViewModel : BindableBase
    {
        private readonly IChildViewModelService _ChildViewModelService;

        public DocumentOutlineViewModel(IEventAggregator eventAggregator,
            IChildViewModelService childViewModelService)
        {
            if (eventAggregator == null) throw new ArgumentNullException(nameof(eventAggregator));
            if (childViewModelService == null) throw new ArgumentNullException(nameof(childViewModelService));
            _ChildViewModelService = childViewModelService;
            eventAggregator.GetEvent<ActiveDocumentChangedEvent>().Subscribe(OnActiveDocumentChanged);
        }

        private void OnActiveDocumentChanged(DocumentViewModel doc)
        {
            ActiveDocument = doc;
        }

        private DocumentViewModel _ActiveDocument;

        public DocumentViewModel ActiveDocument
        {
            get { return _ActiveDocument; }
            set { SetProperty(ref _ActiveDocument, value); }
        }

    }
}
