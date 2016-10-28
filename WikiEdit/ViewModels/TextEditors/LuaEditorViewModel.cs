using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Highlighting;
using WikiEdit.Services;

namespace WikiEdit.ViewModels.TextEditors
{
    public class LuaEditorViewModel : RawTextEditorViewModel
    {
        /// <inheritdoc />
        public LuaEditorViewModel(SettingsService settingsService) : base(settingsService)
        {
            this.HighlightingDefinition = HighlightingManager.Instance.GetDefinition("Lua");
        }
    }
}
