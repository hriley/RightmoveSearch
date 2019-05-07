using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace RightmoveSearch.Domain
{
    public class RightmoveService
    {
        private const int PageSize = 25;
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.131 Safari/537.36";
        private const string ResultCountSelector = "searchHeader-resultCount";
        private const string PageCountSelector = "pagination-pageInfo";
        private const string LinkContainterSelector = "propertyCard-link";
        private const string LinkSelector = "/property-for-sale/property-";
        readonly string[] _keywords = { "modernisation", "modernization", "refurbishment", "updating" };

        public SearchResultModel GetProperties(int minPrice, int maxPrice, int maxDaysSinceAdded, int radius)
        {
            //var rightmoveUrl = string.Format("http://www.rightmove.co.uk/property-for-sale/Fareham.html?minPrice={0}&maxPrice={1}&maxDaysSinceAdded={2}&savedSearchId=18514941&retirement=false&numberOfPropertiesPerPage=50&radius={3}&newHome=false&partBuyPartRent=false&index=", minPrice, maxPrice, maxDaysSinceAdded, radius);
            //var rightmoveUrl = string.Format("http://www.rightmove.co.uk/property-for-sale/find.html?searchType=SALE&locationIdentifier=REGION%5E503&insId=1&radius={3}&minPrice={0}&maxPrice={1}&minBedrooms=&maxBedrooms=&displayPropertyType=houses&maxDaysSinceAdded={2}&_includeSSTC=on&sortByPriceDescending=&primaryDisplayPropertyType=&secondaryDisplayPropertyType=&oldDisplayPropertyType=&oldPrimaryDisplayPropertyType=&newHome=&auction=false&index=", minPrice, maxPrice, maxDaysSinceAdded, radius);

            var rightmoveUrl = string.Format("http://www.rightmove.co.uk/property-for-sale/find.html?locationIdentifier=REGION%5E1366&minBedrooms=&minPrice={0}&maxPrice={1}&radius={3}&propertyTypes=detached%2Csemi-detached%2Cterraced&primaryDisplayPropertyType=houses&maxDaysSinceAdded={2}&includeSSTC=false&index=", minPrice, maxPrice, maxDaysSinceAdded, radius);

            //Load html for the first page of the search results
            var root = LoadHtml(0, rightmoveUrl, UserAgent);

            //get the number of search results
            int resultCount = 0;
            var resultCountObj = root.Descendants("span").FirstOrDefault(x => x.GetAttributeValue("class", "") == ResultCountSelector);
            if (resultCountObj != null)
            {   
                int.TryParse(resultCountObj.InnerText, out resultCount);
            }

            //get the number of pages
            var pageCount = (int)Math.Ceiling((double)resultCount / PageSize);

            //create a list of all property links
            var propertyLinks = new List<string>();
            for (var i = 0; i <= pageCount - 1; i++)
            {
                var pageIndex = i * PageSize;
                var html = root ?? LoadHtml(pageIndex, rightmoveUrl, UserAgent);
                var results = html.Descendants("a").Where(x => x.GetAttributeValue("class", "").Contains(LinkContainterSelector) && x.GetAttributeValue("href", "").Contains(LinkSelector)).Select(y => y.GetAttributeValue("href", "")).Distinct();
                var enumerable = results as IList<string> ?? results.ToList();
                //int count = enumerable.Count();
                propertyLinks.AddRange(enumerable);
                root = null;
            }

            var matches = new List<SearchResultModelItem>();
            SearchResultModel searchReturn = new SearchResultModel();
            searchReturn.PropertiesFound = resultCount;

            //load each link and check it for the key words
            var degreeOfParallelism = Environment.ProcessorCount;
            var tasks = new Task[degreeOfParallelism];

            Parallel.ForEach(propertyLinks, link => {
                SearchResultModelItem searchResult = new SearchResultModelItem();
                var propertyUrl = string.Format("http://www.rightmove.co.uk{0}", link);
                var propertyPage = LoadHtml(propertyUrl, UserAgent);
                var propertyAddress = propertyPage.Descendants("address").FirstOrDefault(x => x.GetAttributeValue("class", "") == "pad-0 fs-16 grid-25").InnerText;

                //check if it's a duplicate
                if (matches.Select(m => m.Address).Contains(propertyAddress))
                {
                    return;
                }

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
                    if (!found) return;
                }

                if (found)
                {
                    searchResult.Link = link;
                    searchResult.DateAdded = DateTime.Parse(propertyPage.Descendants("div").FirstOrDefault(x => x.GetAttributeValue("id", "") == "firstListedDateValue").InnerText);
                    searchResult.Address = propertyAddress;
                    matches.Add(searchResult);
                }
            });

            searchReturn.Results = matches.OrderByDescending(x => x.DateAdded);

            return searchReturn;
        }


        private HtmlNode LoadHtml(int pageIndex, string rightmoveUrl, string userAgent)
        {
            var url = string.Format("{0}{1}", rightmoveUrl, pageIndex);
            //var html = new HtmlDocument();
            try
            {
                var html = new CustomWebClient().GetPage(url, userAgent);
                return html.DocumentNode;
            }
            catch (Exception ex)
            {
                return null;
            }            
        }

        private HtmlNode LoadHtml(string url, string userAgent)
        {
            //var html = new HtmlDocument();
            //html.LoadHtml(new WebClient().DownloadString(url));
            var html = new CustomWebClient().GetPage(url, userAgent);


            return html.DocumentNode;
        }
    }
}
