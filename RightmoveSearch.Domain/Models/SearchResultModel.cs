using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RightmoveSearch.Domain
{
    public class SearchResultModel
    {
        public int PropertiesFound { get; set; }
        public IEnumerable<SearchResultModelItem> Results { get; set; }
    }

    public class SearchResultModelItem
    {
        public string Link { get; set; }
        public DateTime DateAdded { get; set; }
        public string Address { get; set; }
    }

    public class ValidLink
    {
        public string Address { get; set; }
        public string Link { get; set; }
    }
}