using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Highlighting;
using WikiEdit.Services;

namespace WikiEdit.ViewModels.TextEditors
{
    public class JavaScriptEditorViewModel : RawTextEditorViewModel
    {
        /// <inheritdoc />
        public JavaScriptEditorViewModel(SettingsService settingsService) : base(settingsService)
        {
            this.HighlightingDefinition = HighlightingManager.Instance.GetDefinition("JavaScript");
        }
    }
}
