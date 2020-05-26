using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Windows.Forms;
using System.Configuration;
using System.Globalization;

namespace RightmoveSearch.Domain
{
    public class RightmoveService
    {
        public static string[] Keywords
        {
            get
            {
                var rawKeywords = Setting<string>("Keywords");
                var keywords = !string.IsNullOrEmpty(rawKeywords) ? rawKeywords.Split(char.Parse(",")) : null;
                return keywords;
            }
        }

        private static T Setting<T>(string name)
        {
            string value = ConfigurationManager.AppSettings[name];

            if (value == null)
            {
                throw new Exception(String.Format("Could not find setting '{0}',", name));
            }

            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }


        private int PageSize(Enums.SearchTypeEnum searchType)
        {
            switch (searchType)
            {
                case Enums.SearchTypeEnum.OnTheMarket:
                    return 24;
                case Enums.SearchTypeEnum.RightMove:
                    return 25;
                default:
                    return 25;
            }
        }
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.131 Safari/537.36";
        //private const string ResultCountSelector = "searchHeader-resultCount";
        private const string PageCountSelector = "pagination-pageInfo";
        private const string LinkContainterSelector = "propertyCard-link";
        private const string LinkSelector = "/property-for-sale/property-";
        private string daysSinceAdded(int value)
        {
            if(value > 0)
            {
                return value.ToString();
            }
            else
            {
                return string.Empty;
            }
        } 
        //readonly string[] _keywords = { "modernisation", "modernization", "refurbishment", "updating" };
        private Enums.SearchTypeEnum SearchType { get; set;}

        private string ResultCountSelector {
            get
            {
                switch (SearchType)
                {
                    case Enums.SearchTypeEnum.OnTheMarket:
                        return "results-count";
                    case Enums.SearchTypeEnum.RightMove:
                        return "searchHeader-resultCount";
                    default:
                        return string.Empty;
                }
            }
        }

        public SearchResultModel GetOTMProperties(string location, int minPrice, int maxPrice, int maxDaysSinceAdded, int radius)
        {
            SearchType = Enums.SearchTypeEnum.OnTheMarket;
            var otmSearchUrl = string.Format("https://www.onthemarket.com/for-sale/2-bed-property/{4}/?max-bedrooms=&max-price={1}&min-price={0}&new-home-flag=F&prop-types=bungalows&prop-types=houses&retirement=false&shared-ownership=false&view=grid", minPrice, maxPrice, maxDaysSinceAdded, radius, location);
            
            //Load html for the first page of the search results
            var root = LoadHtmlAsynch(0, otmSearchUrl, UserAgent);

            //get the total number of search results
            var resultCount = GetSearchResultCount(root);

            //get a list of all the property links from the search results
            var propertyLinks = GetPropertyLinks(root, otmSearchUrl, Enums.SearchTypeEnum.OnTheMarket);

            var matches = new List<SearchResultModelItem>();
            var searchReturn = new SearchResultModel
            {
                PropertiesFound = resultCount,
                SearchLink = otmSearchUrl
            };

            //load each link and check for our key words
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
                    found = Keywords.Any(keyFeaturesList.InnerText.ToLower().Contains);
                }

                //Now check the description for matches
                var description = propertyPage.Descendants("div").FirstOrDefault(x => x.GetAttributeValue("class", "") == "left overflow-hidden agent-content");

                //the description can be null in some rare cases
                if (!found && description != null)
                {
                    found = Keywords.Any(description.InnerText.ToLower().Contains);
                    if (!found) return;
                }


                //date added can be null in some rare cases
                var dateAdded = propertyPage.Descendants("div").FirstOrDefault(x => x.GetAttributeValue("id", "") == "firstListedDateValue");
                DateTime parsedDateAdded = new DateTime();
                if (dateAdded != null)
                {
                    parsedDateAdded = DateTime.Parse(dateAdded.InnerText);
                    if (!found) return;
                }

                if (found)
                {
                    searchResult.Link = link;
                    searchResult.DateAdded = parsedDateAdded;
                    searchResult.Address = propertyAddress;
                    matches.Add(searchResult);
                }
            });

            searchReturn.Results = matches.OrderByDescending(x => x.DateAdded);

            return searchReturn;
        }

        public SearchResultModel GetRightmoveProperties(string region, int minPrice, int maxPrice, int maxDaysSinceAdded, int radius)
        {
            var rightmoveSearchUrl = string.Format("http://www.rightmove.co.uk/property-for-sale/find.html?locationIdentifier={4}&minBedrooms=&minPrice={0}&maxPrice={1}&radius={3}&propertyTypes=detached%2Csemi-detached%2Cterraced%2Cbungalow&primaryDisplayPropertyType=houses&maxDaysSinceAdded={2}&includeSSTC=false&index=", minPrice, maxPrice, daysSinceAdded(maxDaysSinceAdded), radius, region);
            SearchType = Enums.SearchTypeEnum.RightMove;

            //Load html for the first page of the search results
            var root = LoadHtml(0, rightmoveSearchUrl, UserAgent);

            //get the total number of search results
            var resultCount = GetSearchResultCount(root);

            //get a list of all the property links from the search results
            var propertyLinks = GetPropertyLinks(root, rightmoveSearchUrl, Enums.SearchTypeEnum.RightMove);
            
            var matches = new List<SearchResultModelItem>();
            var searchReturn = new SearchResultModel
            {
                PropertiesFound = resultCount,
                SearchLink = rightmoveSearchUrl
            };
            

            //load each link and check for our key words
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
                    found = Keywords.Any(keyFeaturesList.InnerText.ToLower().Contains);
                }

                //Now check the description for matches
                var description = propertyPage.Descendants("div").FirstOrDefault(x => x.GetAttributeValue("class", "") == "left overflow-hidden agent-content");

                //the description can be null in some rare cases
                if (!found && description != null)
                {
                    found = Keywords.Any(description.InnerText.ToLower().Contains);
                    if (!found) return;
                }


                //date added can be null in some rare cases
                var dateAdded = propertyPage.Descendants("div").FirstOrDefault(x => x.GetAttributeValue("id", "") == "firstListedDateValue");
                DateTime parsedDateAdded = new DateTime();
                if (dateAdded != null)
                {
                    parsedDateAdded = DateTime.Parse(dateAdded.InnerText);
                    if (!found) return;
                }

                if (found)
                {
                    searchResult.Link = link;
                    searchResult.DateAdded = parsedDateAdded;
                    searchResult.Address = propertyAddress;
                    matches.Add(searchResult);
                }
            });

            searchReturn.Results = matches.OrderByDescending(x => x.DateAdded);

            return searchReturn;
        }


        private IEnumerable<string> GetPropertyLinks(HtmlNode rootNode, string url, Enums.SearchTypeEnum searchType)
        {
            //get the number of pages
            int resultCount = GetSearchResultCount(rootNode);
            var pageCount = (int)Math.Ceiling((double)resultCount / PageSize(searchType));

            //create a list of all property links
            var propertyLinks = new List<string>();
            for (var i = 0; i <= pageCount - 1; i++)
            {
                var pageIndex = i * PageSize(searchType);
                var html = rootNode ?? LoadHtml(pageIndex, url, UserAgent);
                var results = html.Descendants("a").Where(x => x.GetAttributeValue("class", "").Contains(LinkContainterSelector) && x.GetAttributeValue("href", "").Contains(LinkSelector)).Select(y => y.GetAttributeValue("href", "")).Distinct();
                var enumerable = results as IList<string> ?? results.ToList();
                //int count = enumerable.Count();
                propertyLinks.AddRange(enumerable);
                rootNode = null;
            }

            return propertyLinks;
        }

        private int GetSearchResultCount(HtmlNode rootNode)
        {
            //get the number of search results
            int resultCount = 0;
            var resultCountObj = rootNode.Descendants("span").FirstOrDefault(x => x.GetAttributeValue("class", "") == ResultCountSelector);
            if (resultCountObj != null)
            {
                int.TryParse(resultCountObj.InnerText, out resultCount);
            }
            return resultCount;
        }

        private HtmlNode LoadHtml(int pageIndex, string url, string userAgent)
        {
            var fullUrl = string.Format("{0}{1}", url, pageIndex);
            //var html = new HtmlDocument();
            //try
            //{
                var html = new CustomWebClient().GetPage(fullUrl, userAgent);
                return html.DocumentNode;
            //}
            //catch (Exception ex)
            //{
            //    return null;
            //}            
        }

        private HtmlNode LoadHtml(string rightmoveUrl, string userAgent)
        {
            //var html = new HtmlDocument();
            //html.LoadHtml(new WebClient().DownloadString(url));
            var html = new CustomWebClient().GetPage(rightmoveUrl, userAgent);


            return html.DocumentNode;
        }

        private HtmlNode LoadHtmlAsynch(int pageIndex, string url, string userAgent)
        {
            var fullUrl = string.Format("{0}{1}", url, pageIndex);
            //var html = new HtmlDocument();
            try
            {
                //var html = new CustomWebClient().GetPage(fullUrl, userAgent);
                //return html.DocumentNode;


                var web1 = new HtmlWeb();
                var doc1 = web1.LoadFromBrowser(url, o =>
                {
                    var webBrowser = (WebBrowser)o;

                    var spans = webBrowser.Document.GetElementsByTagName("span");
                    foreach (HtmlElement span in spans)
                    {
                        if (span.GetAttribute("className") == "results-count")
                        {
                            // WAIT until the dynamic text is set
                            return !string.IsNullOrEmpty(span.InnerText);
                        }
                    }
                    return false;
                    
                });

                return doc1.DocumentNode;

            }
            catch (Exception ex)
            {
                return null;
            }

            //string url = "http://html-agility-pack/from-browser";

            
        }
    }
}
