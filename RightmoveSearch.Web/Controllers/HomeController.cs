using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HtmlAgilityPack;
using System.Net;
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
                MaxPrice = 280000,
                MaxDaysSinceAdded = 1,
                Radius = 20,
                Results = null
            };

            return View(vm);
        }

        [HttpPost]
        public ActionResult Index(HomeViewModel model)
        {
            
            if (ModelState.IsValid)
            {
                var cacheKey = string.Format("{0}-{1}-{2}-{3}", model.MinPrice, model.MaxPrice, model.MaxDaysSinceAdded, model.Radius);
                if (HttpRuntime.Cache.Get(cacheKey) == null)
                {
                    var service = new RightmoveService();
                    model.Results = service.GetProperties(model.MinPrice, model.MaxPrice, model.MaxDaysSinceAdded, model.Radius);
                    HttpRuntime.Cache.Insert(cacheKey, model.Results, null, DateTime.Now.AddHours(12), Cache.NoSlidingExpiration);
                }
                else
                {
                    model.Results = (IEnumerable<SearchResultModel>)HttpRuntime.Cache.Get(cacheKey);
                }
            }

            return View(model);
        }
    }
}