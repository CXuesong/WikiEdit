using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikiEdit.Services;

namespace WikiEdit.ViewModels.TextEditors
{
    public class RawTextEditorViewModel : TextEditorViewModel
    {
        /// <inheritdoc />
        public RawTextEditorViewModel(SettingsService settingsService) : base(settingsService)
        {
        }
    }
}
