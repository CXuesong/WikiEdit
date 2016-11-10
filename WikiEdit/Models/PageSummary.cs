using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiEdit.Models
{
    /// <summary>
    /// Basic information of a MediaWiki page.
    /// </summary>
    public class PageSummary
    {
        /// <summary>
        /// Page title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Short description of the page.
        /// </summary>
        public string Description { get; set; }

        public IList<string> RedirectPath { get; set; }

        public int ContentLength { get; set; }

        public int LastRevisionId { get; set; }

        public DateTime LastRevisionTime { get; set; }

        public string LastRevisionUser { get; set; }
    }
}
