using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikiClientLibrary;

namespace WikiEdit.Models
{
    /// <summary>
    /// Basic information of a MediaWiki page.
    /// </summary>
    public class PageInfo
    {
        private static readonly TemplateArgumentInfo[] _EmptyTemplateArguments = {};
        private static readonly string[] _EmptyStrings = { };

        /// <summary>
        /// Page canonical title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Short description of the page.
        /// </summary>
        public string Description { get; set; }

        public IList<string> RedirectPath { get; set; } = _EmptyStrings;

        public int ContentLength { get; set; }

        public int LastRevisionId { get; set; }

        public DateTime LastRevisionTime { get; set; }

        public string LastRevisionUser { get; set; }


        /// <summary>
        /// A list of arguments for template.
        /// </summary>
        public IList<TemplateArgumentInfo> TemplateArguments { get; set; } = _EmptyTemplateArguments;
    }

    /// <summary>
    /// Used to describe a template argument in <see cref="PageInfo"/> .
    /// </summary>
    public class TemplateArgumentInfo
    {
        public TemplateArgumentInfo(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
