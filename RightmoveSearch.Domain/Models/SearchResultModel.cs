using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RightmoveSearch.Domain
{
    public class SearchResultModel
    {
        public string Link { get; set; }
        public DateTime DateAdded { get; set; }
        public string Address { get; set; }
    }
}