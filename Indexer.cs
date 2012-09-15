using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Net;

namespace _5412_Project
{
    public class Indexer
    {
        #region Private Fields
        private static String[] unwantedExtensions = {".doc",".rtf",".txt",".pdf",".xls",".xpi",".rss",".atom",".opml",".vcard",".exe",".dmg",".app",".pps",".ical",".jpg",".gif",".png",".bmp",".svg",".eps",".swf",".fla",".css",".mp3",".wav",".ogg",".wma",".m4a",".zip",".rar",".gzip",".bzip",".ace",".ttf",".mov",".wmv",".mp4",".avi",".mpg",".phps",".torrent.",".ico"};
        #endregion

        #region Public Fields
        public int indexerID { get; private set; }
        #endregion

        #region Constructor
        public Indexer(int id)
        {
            indexerID = id;
        }
        #endregion

        #region Main Method
        public void Index(object urlDataDateTuple)
        {
            //Get Data from Indexer
            Tuple <string,string,DateTime> urlDataDate = (Tuple <string,string,DateTime>)urlDataDateTuple;
            Console.WriteLine("Indexer " + indexerID + " got the following URL: {0}",urlDataDate.Item1);

            //Process Data
            Tuple<Dictionary<string, int>, List<string>> frequenciesAndURLs = getFrequenciesAndURLs(urlDataDate.Item1, urlDataDate.Item2);

            //Send info to database
            URLData newData = new URLData(urlDataDate.Item1, urlDataDate.Item3, frequenciesAndURLs.Item1);
            Program.database.addURLData(newData);
            //Console.WriteLine("Sent info into database");

            //Send new URLs to Scheduler if they haven't been crawled
            URLScheduler scheduler = Program.urlScheduler;
            scheduler.queueSema.WaitOne();
            foreach (var newURL in frequenciesAndURLs.Item2) 
            {
                scheduler.urlQueue.Enqueue(newURL);
            }
            scheduler.queueSema.Release();
            //Console.WriteLine("Sent new URL's to URL scheduler");

            //Put self back into Index Scheduler
            IndexScheduler indexScheduler = Program.indexScheduler;
            indexScheduler.indexerSema.WaitOne();
            indexScheduler.indexerQueue.Enqueue(this);
            indexScheduler.indexerSema.Release();

            //Debugging
            if (Program.debugMode)
            {
                Console.WriteLine(frequenciesAndURLs.Item2.Count + " URLs added to scheduler");
            }
        }
        #endregion

        #region Internal Helper Functions
        internal Tuple<Dictionary<string, int>,List<string>> getFrequenciesAndURLs(string url, string data)
        {
            Dictionary<string, int> frequencies = dataTextFrequencies(data);
            List<string> urls = getURLS(url, data);
            var freqURLTuple = Tuple.Create(frequencies, urls);
            return freqURLTuple;
        }

        internal List<string> getURLS(string originalURL, string data)
        {
            var urlList = new List<string>();
            MatchCollection urls = Regex.Matches(data, "href=\"[a-zA-Z./:&\\d_-]+\"");
            string currentURL;
            foreach (Match url in urls)
            {
                currentURL = url.Value.Replace("href=\"", "");
                currentURL = currentURL.Substring(0, currentURL.IndexOf("\""));
                if (currentURL.Length < 4 || !currentURL.Substring(0, 3).Equals("http"))
                {
                    currentURL = originalURL + currentURL;
                }
                if (!unwantedExtensions.Any(currentURL.Contains))
                {
                    urlList.Add(currentURL);
                }
            }
            return urlList;
        }

        internal Dictionary<string,int> dataTextFrequencies(string data) {
            var frequencies = new Dictionary<string, int>();
            
            //Get text only
            //WebBrowser webBrowser = new WebBrowser();
            //webBrowser.DocumentText = "";
            //HtmlDocument htmlDoc = webBrowser.Document;
            //htmlDoc.OpenNew(false);
            //htmlDoc.Write(data);
            //string text = htmlDoc.Body.InnerText;
            string text = pageTextOnly(data);

            //Remove punctuation from text
            text = Regex.Replace(text, @"[\p{P}+]", "",RegexOptions.Singleline);
            text = Regex.Replace(text, @"\|", "", RegexOptions.Singleline);
            text = Regex.Replace(text, @"\s+", " ",RegexOptions.Singleline);

            //Get Frequencies
            string[] words = text.ToLower().Split(' ');
            foreach (string word in words)
            {
                if (!word.Equals(""))
                {
                    if (frequencies.ContainsKey(word))
                    {
                        frequencies[word]++;
                    }
                    else
                    {
                        frequencies[word] = 1;
                    }
                }
            }
            return frequencies;
        }

        //Open Source code (NOT orginally written by me)
        internal string ResolveRelativePaths(string relativeUrl, string originatingUrl)
        {
            string resolvedUrl = String.Empty;

            string[] relativeUrlArray = relativeUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string[] originatingUrlElements = originatingUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int indexOfFirstNonRelativePathElement = 0;
            for (int i = 0; i <= relativeUrlArray.Length - 1; i++)
            {
                if (relativeUrlArray[i] != "..")
                {
                    indexOfFirstNonRelativePathElement = i;
                    break;
                }
            }

            int countOfOriginatingUrlElementsToUse = originatingUrlElements.Length - indexOfFirstNonRelativePathElement - 1;
            for (int i = 0; i <= countOfOriginatingUrlElementsToUse - 1; i++)
            {
                if (originatingUrlElements[i] == "http:" || originatingUrlElements[i] == "https:")
                    resolvedUrl += originatingUrlElements[i] + "//";
                else
                    resolvedUrl += originatingUrlElements[i] + "/";
            }

            for (int i = 0; i <= relativeUrlArray.Length - 1; i++)
            {
                if (i >= indexOfFirstNonRelativePathElement)
                {
                    resolvedUrl += relativeUrlArray[i];

                    if (i < relativeUrlArray.Length - 1)
                        resolvedUrl += "/";
                }
            }

            return resolvedUrl;
        }

        public static string pageTextOnly(string data)
        {
            //Console.WriteLine(data);
            //Remove CSS styles
            string text = Regex.Replace(data, "<style(.| )*?>*</style>", " ", RegexOptions.Singleline);
            //Console.WriteLine(text);
            //Remove script blocks
            text = Regex.Replace(text, "<script(.| )*?>*</script>", " ", RegexOptions.Singleline);
            //Console.WriteLine(text);
            //Remove all ifmages
            text = Regex.Replace(text, "<img(.| )*?/>", " ", RegexOptions.Singleline);
            //Console.WriteLine(text);
            //Remove all HTML tags, leaving on the text inside.
            text = Regex.Replace(text, "<(.| )*?>", " ", RegexOptions.Singleline);
            //Console.WriteLine(text);
            return text;
        }
        #endregion
    }
}
