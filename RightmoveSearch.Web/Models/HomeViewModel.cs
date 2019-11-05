using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using RightmoveSearch.Domain;
using RightmoveSearch.Domain.Enums;

namespace RightmoveSearch.Web.Models
{
    public class HomeViewModel
    {
        public Dictionary<string, string> Regions { get; set; }

        [Required]
        public string selectedRegion { get; set; }

        [Required]
        public int MinPrice { get; set; }

        [Required]
        public int MaxPrice { get; set; }

        public Dictionary<string, int> MaxDaysSinceAdded { get; set; }

        [Required]
        public int selectedMaxDays { get; set; }

        public Dictionary<string, double> Radius { get; set; }

        [Required]
        public int selectedRadius { get; set; }

        [DefaultValue(null)]
        public SearchResultModel Result { get; set; }

        //public SearchTypeEnum SearchType { get; set;}

        public Dictionary<string, int> SearchTypes { get; set; }

        public SearchTypeEnum selectedSearchType { get; set; }

        public int Duration { get; set; }

        public bool IsError { get; set; }

        public string ErrorMsg { get; set; }
    }
}