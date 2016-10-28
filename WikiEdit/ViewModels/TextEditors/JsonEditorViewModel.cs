using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Highlighting;
using WikiEdit.Services;

namespace WikiEdit.ViewModels.TextEditors
{
    public class JsonEditorViewModel : RawTextEditorViewModel
    {
        /// <inheritdoc />
        public JsonEditorViewModel(SettingsService settingsService) : base(settingsService)
        {
            // There seems no built-in language definition for JSON itself.
            this.HighlightingDefinition = HighlightingManager.Instance.GetDefinition("JavaScript");
        }
    }
}
