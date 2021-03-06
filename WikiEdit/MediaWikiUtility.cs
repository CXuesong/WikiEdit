﻿using System;
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
                    return ContentModels.JavaScript;
                if (title.Title.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                    return ContentModels.Css;
            }
            if (title.Namespace.CanonicalName == "Module")
                return ContentModels.Scribunto;
            return ContentModels.Wikitext;
        }

        public static string InferContentModel(this Page page)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            if (page.ContentModel != null) return page.ContentModel;
            // For older MediaWiki sites...
            return InferContentModelFromTitle(WikiLink.Parse(page.Site, page.Title));
        }

        /// <summary>
        /// Perferms simple wikilink title normalization, without the reliance on Site instance.
        /// Used internally in the application.
        /// </summary>
        public static string SimpleNormalizeWikitext(string title)
        {
            if (title == null) throw new ArgumentNullException(nameof(title));
            title = title.Replace("_", " ");
            title = title.Trim();
            if (title == "") return title;
            if (char.IsLower(title[0])) title = char.ToUpper(title[0]) + title.Substring(1);
            return title;
        }
    }
}
