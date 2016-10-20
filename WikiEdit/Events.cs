using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using WikiEdit.ViewModels.Documents;

namespace WikiEdit
{
    /// <summary>
    /// The event is raised when the active document view has been changed.
    /// Note that if all the documents have been closed, the event will be
    /// raised with a <c>null</c> payload.
    /// </summary>
    public class ActiveDocumentChangedEvent : PubSubEvent<DocumentViewModel>
    {
    }
}
