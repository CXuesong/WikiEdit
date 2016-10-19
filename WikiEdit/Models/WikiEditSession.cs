using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WikiEdit.Models
{
    internal class WikiEditSession
    {
        public CookieContainer SessionCookies { get; set; }

        public IList<WikiSite> WikiSites { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }
    }
}
