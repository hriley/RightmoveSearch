using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RightmoveSearch.Domain
{
    public class CustomWebClient
    {
        //The cookies will be here.
        private CookieContainer cookies = new CookieContainer();

        //In case you need to clear the cookies
        public void ClearCookies()
        {
            cookies = new CookieContainer();
        }

        public HtmlDocument GetPage(string url, string userAgent)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";

            if(!string.IsNullOrEmpty(userAgent))
                request.UserAgent = userAgent;
                        

            //This is the important part.
            request.CookieContainer = cookies;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var stream = response.GetResponseStream();

            //When you get the response from the website, the cookies will be stored
            //automatically in "_cookies".

            using (var reader = new StreamReader(stream))
            {
                string html = reader.ReadToEnd();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                return doc;
            }
        }
    }
}
