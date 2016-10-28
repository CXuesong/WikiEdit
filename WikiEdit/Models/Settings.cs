using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiEdit.Models
{
    /// <summary>
    /// Model used to persist application settings.
    /// </summary>
    public class Settings
    {
        public TextEditorSettings TextEditor { get; set; }
    }

    public class TextEditorSettings
    {
        /// <summary>
        /// Text editor settings per language.
        /// </summary>
        /// <remarks><c>LanguageSettings[""]</c> means the language defaults.</remarks>
        public IDictionary<string, TextEditorLanguageSettings> LanguageSettings { get; set; }

        public TimeSpan LastRevisionAutoRefetchInterval { get; set; }
    }

    /// <summary>
    /// Text editor settings per language.
    /// </summary>
    /// <remarks><c>null</c> of any property indicates that default settings should be used.</remarks>
    public class TextEditorLanguageSettings
    {
        public string FontFamily { get; set; }

        public float? FontSize { get; set; }

        /// <summary>
        /// The wiki content model names (e.g. wikitext, Scribunto, etc.) that
        /// uses the language.
        /// </summary>
        /// <remarks>This property is ignored for LanguageSettings[""].</remarks>
        public string[] WikiContentModels { get; set; }
    }
}
