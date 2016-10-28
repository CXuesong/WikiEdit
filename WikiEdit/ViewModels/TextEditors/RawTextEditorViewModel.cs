using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Highlighting;
using WikiEdit.Services;

namespace WikiEdit.ViewModels.TextEditors
{
    public class RawTextEditorViewModel : TextEditorViewModel
    {
        /// <inheritdoc />
        public RawTextEditorViewModel(SettingsService settingsService) : base(settingsService)
        {
        }

        private IHighlightingDefinition _HighlightingDefinition;

        public IHighlightingDefinition HighlightingDefinition
        {
            get { return _HighlightingDefinition; }
            set { SetProperty(ref _HighlightingDefinition, value); }
        }
    }
}
