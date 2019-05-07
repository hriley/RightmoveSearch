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
        public ActionResult Index()
        {
            var vm = new HomeViewModel
            {
                MinPrice = 100000,
                MaxPrice = 300000,
                MaxDaysSinceAdded = 1,
                Radius = 20,
                Result = null
            };

            return View(vm);
        }

        [HttpPost]
        public ActionResult Index(HomeViewModel model)
        {
            Stopwatch sw = Stopwatch.StartNew();
            if (ModelState.IsValid)
            {
                var cacheKey = string.Format("{0}-{1}-{2}-{3}", model.MinPrice, model.MaxPrice, model.MaxDaysSinceAdded, model.Radius);
                if (HttpRuntime.Cache.Get(cacheKey) == null)
                {
                    var service = new RightmoveService();
                    model.Result = service.GetProperties(model.MinPrice, model.MaxPrice, model.MaxDaysSinceAdded, model.Radius);
                    HttpRuntime.Cache.Insert(cacheKey, model.Result, null, DateTime.Now.AddHours(12), Cache.NoSlidingExpiration);
                    
                }
                else
                {
                    model.Result = (SearchResultModel)HttpRuntime.Cache.Get(cacheKey);
                }
            }
            model.Duration = sw.Elapsed.Seconds;
            return View(model);
        }
    }
}