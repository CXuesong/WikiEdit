using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using WikiClientLibrary;
using WikiEdit.Models;

namespace WikiEdit.Spark
{
    internal static class PageInfoBuilder
    {
        /// <summary>
        /// Builds <see cref="PageInfo"/> from a MediaWiki page with its content fetched.
        /// </summary>
        public static PageInfo BuildBasicInfo(Page page, WikitextParser parser)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            var info = new PageInfo
            {
                Title = page.Title,
                LastRevisionId = page.LastRevisionId,
                LastRevisionTime = page.LastRevision?.TimeStamp ?? DateTime.MinValue,
                LastRevisionUser = page.LastRevision?.UserName,
                ContentLength = page.ContentLength,
            };
            if (page.IsRedirect)
            {
                info.Description = page.Content;
            }
            else if (!string.IsNullOrWhiteSpace(page.Content))
            {
                if (page.InferContentModel() == ContentModels.Wikitext)
                {
                    var p = parser.Parse(page.Content);
                    // Search for leading line.
                    var leadingLine = p.EnumChildren()
                        .OfType<Paragraph>()
                        .FirstOrDefault(line => line.Inlines.OfType<PlainText>()
                            .Any(pt => !string.IsNullOrWhiteSpace(pt.Content)));
                    if (leadingLine != null) info.Description = leadingLine.ToString();
                    // Collect template arguments, if any.
                    info.TemplateArguments = p.EnumDescendants()
                        .OfType<ArgumentReference>()
                        .Select(r => new TemplateArgumentInfo(r.Name.ToString().Trim()))
                        .Distinct(TemplateArgumentInfoComparer.Default)
                        .ToArray();
                }
            }
            return info;
        }

        private class TemplateArgumentInfoComparer : IEqualityComparer<TemplateArgumentInfo>
        {
            public static readonly TemplateArgumentInfoComparer Default = new TemplateArgumentInfoComparer();

            /// <inheritdoc />
            public bool Equals(TemplateArgumentInfo x, TemplateArgumentInfo y)
            {
                if (x == null || y == null) return x  == null && y == null;
                return x.Name == y.Name;
            }

            /// <inheritdoc />
            public int GetHashCode(TemplateArgumentInfo obj)
            {
                if (obj == null) return 0;
                return obj.Name.GetHashCode();
            }
        }
    }
}
