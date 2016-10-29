using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiEdit
{
    internal static class GlobalConfigurations
    {
        public const string TranslationDictionaryFile = "WikiEdit.txd";

        public const string SyntaxHighlighterDefinitionFolder = "SyntaxHighlighters";

        public const string SettingsFile = "settings.json";

        /// <summary>
        /// Uri of the resource file that contains the source control versioning information.
        /// </summary>
        public static readonly Uri SourceControlVersionUri = new Uri("pack://application:,,,/WikiEdit;component/scversion.txt");
    }
}
