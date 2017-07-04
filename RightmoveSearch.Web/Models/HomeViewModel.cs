using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using RightmoveSearch.Domain;

namespace RightmoveSearch.Web.Models
{
    public class HomeViewModel
    {
        [Required]
        public int MinPrice { get; set; }

        [Required]
        public int MaxPrice { get; set; }

        [Required]
        public int MaxDaysSinceAdded { get; set; }

        [Required]
        public int Radius { get; set; }

        [DefaultValue(null)]
        public IEnumerable<SearchResultModel> Results { get; set; }
    }
}