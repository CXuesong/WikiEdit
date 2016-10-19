using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiEdit.Models
{
    internal class WikiSite
    {
        public string Name { get; set; }


        public string ApiEndpoint { get; set; }

        public DateTimeOffset LastAccessTime { get; set; }

        #region Site Information Cache

        public string SiteName { get; set; }

        public string UserName { get; set; }

        #endregion

    }
}
