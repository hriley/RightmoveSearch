using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HtmlAgilityPack;
using System.Diagnostics;
using RightmoveSearch.Domain;
using RightmoveSearch.Web.Models;
using System.Web.Caching;

namespace RightmoveSearch.Web.Controllers
{
    public class HomeController : Controller
    {
        private HomeViewModel SetDDLists(HomeViewModel model)
        {
            var regions = new Dictionary<string, string>();
            regions.Add("Tunbridge Wells", "REGION%5E1366");
            regions.Add("Fareham", "REGION%5E503");
            regions.Add("Southampton", "REGION%5E1231");
            regions.Add("Portsmouth", "REGION%5E1089");
            regions.Add("Truro", "REGION%5E1365");
            regions.Add("Bristol", "REGION%5E219");
            regions.Add("Brighton", "REGION%5E93554");

            var radius = new Dictionary<string, double>();
            radius.Add("This area only", 0);
            radius.Add("1 / 4 mile", 0.25);
            radius.Add("1 mile", 1);
            radius.Add("3 miles", 3);
            radius.Add("5 miles", 5);
            radius.Add("10 miles", 10);
            radius.Add("20 miles", 20);
            radius.Add("30 miles", 30);
            radius.Add("40 miles", 40);

            var maxDays = new Dictionary<string, int>();
            maxDays.Add("1", 1);
            maxDays.Add("3", 3);
            maxDays.Add("7", 7);
            maxDays.Add("14", 14);
            //maxDays.Add("Anytime", 0);

            //var searchTypes = new Dictionary<string, int>();
            //searchTypes.Add("Rightmove", 0);
            //searchTypes.Add("OnTheMarket", 1);

            model.Regions = regions;
            model.Radius = radius;
            model.MaxDaysSinceAdded = maxDays;
            //model.SearchTypes = searchTypes;            

            return model;
        }
        
        public ActionResult Index()
        {
            var vm = new HomeViewModel
            {
                selectedRegion = "REGION%5E1366",
                MinPrice = 150000,
                MaxPrice = 300000,
                selectedMaxDays = 1,
                selectedRadius = 10,
                //selectedSearchType = 0,
                Result = null
            };

            vm = SetDDLists(vm);

            return View(vm);
        }

        [HttpPost]
        public ActionResult Index(HomeViewModel model)
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                if (ModelState.IsValid)
                {

                    //dirty hack to accommodate OnTheMarket
                    //if (model.SearchType == Domain.Enums.SearchTypeEnum.OnTheMarket)
                    //{
                    //    model.selectedRegion = model.OTMRegion;
                    //}

                    var cacheKey = string.Format("{0}-{1}-{2}-{3}-{4}-{5}", model.selectedRegion, model.MinPrice, model.MaxPrice, model.selectedMaxDays, model.selectedRadius, model.selectedSearchType);
                    if (HttpRuntime.Cache.Get(cacheKey) == null)
                    {
                            var service = new RightmoveService();
                            model.Result = service.GetRightmoveProperties(model.selectedRegion, model.MinPrice, model.MaxPrice, model.selectedMaxDays, model.selectedRadius);
                            HttpRuntime.Cache.Insert(cacheKey, model.Result, null, DateTime.Now.AddHours(1), Cache.NoSlidingExpiration);
                    }
                    else
                    {
                        model.Result = (SearchResultModel)HttpRuntime.Cache.Get(cacheKey);
                    }
            }
                
                model.Duration = sw.Elapsed.Seconds;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("ErrorMsg", string.Format("Oops! An error occurred: {0}", ex.Message));
            }

            model = SetDDLists(model);
            return View(model);
        }
    }
}