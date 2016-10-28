using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikiClientLibrary;

namespace WikiEdit
{
    internal static class MediaWikiUtility
    {
        /// <summary>
        /// Infers MediaWiki content model name from the title of the page.
        /// This is for MediaWiki version lower than 1.21 .
        /// </summary>
        public static string InferContentModelFromTitle(WikiLink title)
        {
            if (title == null) throw new ArgumentNullException(nameof(title));
            if (title.Namespace.Id == BuiltInNamespaces.MediaWiki || title.Namespace.Id == BuiltInNamespaces.User)
            {
                if (title.Title.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                    return "javascript";
                if (title.Title.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                    return "css";
            }
            if (title.Namespace.CanonicalName == "Module")
                return "Scribunto";
            return "wikitext";
        }
    }
}
