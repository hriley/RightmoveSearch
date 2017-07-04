using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HtmlAgilityPack;

namespace RightmoveSearch.Domain
{
    public class RightmoveService
    {
        private const int PageSize = 50;
        private const string ResultCountSelector = "resultcount";
        private const string LinkContainterSelector = "photo touchsearch-summary-list-item-photo";
        private const string LinkSelector = "/property-for-sale/property-";
        readonly string[] _keywords = { "modernisation", "modernization", "refurbishment", "updating" };

        public IEnumerable<SearchResultModel> GetProperties(int minPrice, int maxPrice, int maxDaysSinceAdded, int radius)
        {
            //var rightmoveUrl = string.Format("http://www.rightmove.co.uk/property-for-sale/Fareham.html?minPrice={0}&maxPrice={1}&maxDaysSinceAdded={2}&savedSearchId=18514941&retirement=false&numberOfPropertiesPerPage=50&radius={3}&newHome=false&partBuyPartRent=false&index=", minPrice, maxPrice, maxDaysSinceAdded, radius);


            var rightmoveUrl = string.Format("http://www.rightmove.co.uk/property-for-sale/find.html?searchType=SALE&locationIdentifier=REGION%5E503&insId=1&radius={3}&minPrice={0}&maxPrice={1}&minBedrooms=&maxBedrooms=&displayPropertyType=houses&maxDaysSinceAdded={2}&_includeSSTC=on&sortByPriceDescending=&primaryDisplayPropertyType=&secondaryDisplayPropertyType=&oldDisplayPropertyType=&oldPrimaryDisplayPropertyType=&newHome=&auction=false&index=", minPrice, maxPrice, maxDaysSinceAdded, radius);


            //Load html for the first page of the search results
            var root = LoadHtml(0, rightmoveUrl);

            //get the number of search results
            int resultCount = 0;
            var firstOrDefault = root.Descendants("span").FirstOrDefault(x => x.GetAttributeValue("id", "") == ResultCountSelector);
            if (firstOrDefault != null)
                int.TryParse(firstOrDefault.InnerText, out resultCount);

            //get the number of pages
            var pageCount = (int)Math.Ceiling((double)resultCount / PageSize);

            //create a list of all property links
            var validLinks = new List<string>();
            for (var i = 0; i <= pageCount - 1; i++)
            {
                var pageIndex = i * PageSize;
                var html = root ?? LoadHtml(pageIndex, rightmoveUrl);
                var results = html.Descendants("a").Where(x => x.GetAttributeValue("class", "") == LinkContainterSelector && x.GetAttributeValue("href", "").Contains(LinkSelector)).Select(y => y.GetAttributeValue("href", "")).Distinct();
                var enumerable = results as IList<string> ?? results.ToList();
                //int count = enumerable.Count();
                validLinks.AddRange(enumerable);
                root = null;
            }

            var matches = new List<SearchResultModel>();
            //load each link and check it for the key words
            foreach (var link in validLinks)
            {
                SearchResultModel searchResult = new SearchResultModel();
                var propertyUrl = string.Format("http://www.rightmove.co.uk{0}", link);
                var propertyPage = LoadHtml(propertyUrl);

                //Check the key features list for matches
                var keyFeaturesList = propertyPage.Descendants("ul").FirstOrDefault(x => x.GetAttributeValue("class", "") == "list-two-col list-style-square");

                //the key features list can be null in some rare cases
                bool found = false;
                if (keyFeaturesList != null)
                {
                    found = _keywords.Any(keyFeaturesList.InnerText.ToLower().Contains);                    
                }

                //Now check the description for matches
                var description = propertyPage.Descendants("div").FirstOrDefault(x => x.GetAttributeValue("class", "") == "left overflow-hidden agent-content");

                //the description can be null in some rare cases
                if (!found && description != null)
                {
                    found = _keywords.Any(description.InnerText.ToLower().Contains);
                    if (!found) continue;
                }


                if (found)
                {
                    searchResult.Link = link;
                    searchResult.DateAdded = DateTime.Parse(propertyPage.Descendants("div").FirstOrDefault(x => x.GetAttributeValue("id", "") == "firstListedDateValue").InnerText);
                    searchResult.Address = propertyPage.Descendants("address").FirstOrDefault(x => x.GetAttributeValue("class", "") == "pad-0 fs-16 grid-25").InnerText;
                    matches.Add(searchResult);
                }
            }

            return matches.OrderByDescending(x => x.DateAdded);
        }


        private HtmlNode LoadHtml(int pageIndex, string rightmoveUrl)
        {
            var url = string.Format("{0}{1}", rightmoveUrl, pageIndex);
            var html = new HtmlDocument();
            try
            {
                html.LoadHtml(new WebClient().DownloadString("www.rightmove.co.uk"));
                return html.DocumentNode;
            }
            catch (Exception ex)
            {
                return null;
            }            
        }

        private HtmlNode LoadHtml(string url)
        {
            var html = new HtmlDocument();
            html.LoadHtml(new WebClient().DownloadString(url));
            return html.DocumentNode;
        }
    }
}
