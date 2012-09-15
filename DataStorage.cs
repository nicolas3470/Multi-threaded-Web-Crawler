using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace _5412_Project
{
    public class DataStorage
    {
        #region Private fields
        private DateTime timeStarted; //Used for auto-caching feature
        private int days; //Re-crawl websites older than this number
        private Dictionary<string, URLData> urlData; //Dictionary of <URL, data> Pairs
        private Semaphore storageSema; //Semaphore protecting data
        private Dictionary<string, List<URLData>> wordMatches; //Dictionary of <word,list of URL matches> Pairs
        private Semaphore invalidSema;
        #endregion

        #region Public fields
        public bool keepAutoCaching { private get; set; }
        public int numInvalidURLs { get; private set; }
        #endregion

        #region Constructor
        //Creates a data storage started at startTime, that re-crawls websites older than d days
        public DataStorage(DateTime startTime, int cacheDays)
        {
            storageSema = new Semaphore(1, 1);
            urlData = new Dictionary<string, URLData>();
            timeStarted = startTime;
            days = cacheDays;
            invalidSema = new Semaphore(1, 1);
        }
        #endregion

        #region Main thread
        //Add old URLs to the scheduler again
        public void autoCache()
        {
            while (keepAutoCaching)
            { 
                storageSema.WaitOne();
                //Re-cache is scheduler is still active
                if (!Program.urlScheduler.keepScheduling)
                {
                    foreach (var entry in urlData)
                    {
                        if (DateTime.Now.Subtract(entry.Value.dateModified).Days > days)
                        {
                            //Re-queue URL
                            URLScheduler scheduler = Program.urlScheduler;
                            scheduler.queueSema.WaitOne();
                            scheduler.urlQueue.Enqueue((string)entry.Key);
                            scheduler.queueSema.Release();
                        }
                    }
                }
                storageSema.Release();
                Thread.Sleep(60000 * 60 * 24); //Sleep for a day
            }
        }
        #endregion

        #region Public Methods
        public void addURLData(URLData newData)
        {
            string url = newData.url;
            storageSema.WaitOne();
            try
            {
                urlData.Add(url, newData);
            }
            catch (ArgumentException)
            {
                //Duplicate Item Found that is older
                URLData oldData = urlData[url];
                if (DateTime.Compare(oldData.dateModified, newData.dateModified) > 0)
                {
                    urlData[url] = newData;;
                }
            }
            storageSema.Release();
        }

        public bool doesDataContain(string url)
        {
            storageSema.WaitOne();
            bool containsURL = urlData.ContainsKey(url);
            storageSema.Release();
            return containsURL;
        }

        public Tuple<string,string,string> dataReports()
        {
            string reportURLMatch = "URL Match Report\n\n";
            string reportWordMatch = "Word Match Report\n\n";
            string overallReport = "Overall Crawl Report\n\n";
            storageSema.WaitOne();
            //Console.WriteLine("Program Grabs Storage Semaphore");

            //Using url matching data structure 
            foreach (URLData value in urlData.Values) 
            {
                reportURLMatch += "URL: " + value.url + "\n";
                reportURLMatch += "Date Modified: " + value.dateModified + "\n";
                reportURLMatch += "Word Frequencies:\n";
                foreach (var wordFrequencyPair in value.wordFrequencies)
                {
                    reportURLMatch += wordFrequencyPair.Key + ": " + wordFrequencyPair.Value + "\n";
                }
                reportURLMatch += "\n";
            }
            //Console.WriteLine("Created URL Match");

            //Using word matching data structure
            createWordMatchStructure();
            //Console.WriteLine("Created Word Match Structure");
            foreach (var wordListPair in wordMatches)
            {
                reportWordMatch += "Word: " + wordListPair.Key + "\n";
                foreach (var urlMatch in wordListPair.Value)
                {
                    reportWordMatch += "URL: " + urlMatch.url + "\n";
                    reportWordMatch += "Frequency: " + urlMatch.wordFrequencies[wordListPair.Key] + "\n";
                }
            }
            //Console.WriteLine("Stored Word Match");

            //Overall Report
            overallReport += "Starting URL: " + Program.startURL + "\n";
            overallReport += "Crawlers created: " + Program.urlScheduler.numCrawlers + "\n";
            overallReport += "Indexers created: " + Program.indexScheduler.numIndexers + "\n";
            overallReport += "Invalid URLs sent to crawlers: " + numInvalidURLs + "\n";
            overallReport += "Web pages crawled and indexed: " + urlData.Count + "\n";

            //Return report
            storageSema.Release();
            Tuple<string,string,string> report = Tuple.Create(reportURLMatch,reportWordMatch,overallReport);
            //Console.WriteLine("Returned Report");
            return report;
        }

        public void addInvalidURL()
        {
            invalidSema.WaitOne();
            numInvalidURLs++;
            invalidSema.Release();
        }
        #endregion

        #region Internal Helper Methods
        internal void createWordMatchStructure()
        {
            wordMatches = new Dictionary<string, List<URLData>>();
            foreach (URLData data in urlData.Values)
            {
                foreach (var word in data.wordFrequencies.Keys)
                {
                    if (wordMatches.ContainsKey(word))
                    {
                        wordMatches[word].Add(data);
                    }
                    else
                    {
                        List<URLData> newWordList = new List<URLData>();
                        newWordList.Add(data);
                        wordMatches.Add(word, newWordList);
                    }
                }
            }
        }
        #endregion
    }
}
