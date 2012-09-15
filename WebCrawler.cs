using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace _5412_Project
{
    public class WebCrawler
    {
        #region Public Fields
        public int crawlerID { get; private set; }
        #endregion

        #region Constructor
        public WebCrawler(int id)
        {
            crawlerID = id;
        }
        #endregion

        #region Main Method
        public void Crawl(object URL)
        {
            //Get URL from scheduler
            string url = (string)URL;
            Console.WriteLine("Crawler " + crawlerID + " got the following URL: {0}",url);

            //Check if URL is valid
            if (isURLValid(url))
            {
                //Console.WriteLine("valid URL");
                //Access URL text
                string data = urlText(url);
                //Console.WriteLine("Gets Text");

                //Send (data,date) tuple to Index Scheduler
                IndexScheduler indexScheduler = Program.indexScheduler;
                //Console.WriteLine("Waiting");
                indexScheduler.dataSema.WaitOne();
                //Console.WriteLine("Got it");
                indexScheduler.dataQueue.Enqueue(new Tuple<string, string, DateTime>(url, data, DateTime.Now));
                indexScheduler.dataSema.Release();
                Console.WriteLine("Sent data to Index Scheduler");

                //Put self back into URL scheduler
                URLScheduler urlScheduler = Program.urlScheduler;
                urlScheduler.crawlerSema.WaitOne();
                urlScheduler.crawlerQueue.Enqueue(this);
                urlScheduler.crawlerSema.Release();
            } else {
                Program.database.addInvalidURL();
            }
        }
        #endregion

        #region Internal Helper Functions
        internal string urlText(string url)
        {
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            webRequest.UserAgent = "A C# Web Crawler for Academic Purposes";
            WebResponse webResponse = webRequest.GetResponse();
            Stream stream = webResponse.GetResponseStream();
            StreamReader sReader = new StreamReader(stream);
            return sReader.ReadToEnd();
        }

        internal bool isURLValid(string url)
        {
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                webRequest.Timeout = 5000;
                webRequest.Method = "HEAD";
                HttpWebResponse webResponse = (HttpWebResponse) webRequest.GetResponse();
                int pageStatus = (int)webResponse.StatusCode;
                if ( pageStatus >= 100 && pageStatus < 400) 
                {
                    return true;
                } 
                else 
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(url);
                Console.WriteLine();
                Console.WriteLine(e.ToString());
                return false;
            }
        }
        #endregion
    }
}
