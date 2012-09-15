using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace _5412_Project
{
    public class URLScheduler
    {
        #region Private Fields
        private int maxCrawlers;
        private int crawlsToPerform;
        private int crawlsScheduled;
        #endregion

        #region Public Fields
        public Semaphore queueSema { get; private set; }
        public Semaphore crawlerSema { get; private set; }
        public Queue<string> urlQueue {get; private set;}
        public Queue<WebCrawler> crawlerQueue { get; private set; }
        public bool keepScheduling { get; set; }
        public int numCrawlers { get; private set; }
        #endregion

        #region Constructors
        //Default constructor starts crawling at a web directory
        public URLScheduler(int maxCrawl, int numCrawls, string startURL)
        {
            urlQueue = new Queue<string>();
            queueSema = new Semaphore(1, 1);
            crawlerQueue = new Queue<WebCrawler>();
            crawlerSema = new Semaphore(1, 1);
            keepScheduling = true;
            maxCrawlers = maxCrawl;
            crawlsToPerform = numCrawls;
            urlQueue.Enqueue(startURL);
            numCrawlers = 1;
        }

        //Choose starting URL for crawl
        public URLScheduler(int maxCrawl,string url)
        {
            urlQueue = new Queue<string>();
            queueSema = new Semaphore(1, 1);
            crawlerQueue = new Queue<WebCrawler>();
            crawlerSema = new Semaphore(1, 1);
            keepScheduling = true;
            maxCrawlers = maxCrawl;
            urlQueue.Enqueue(url);
        }
        #endregion

        #region Main Thread
        public void schedule()
        {
            //Schedule a URL
            while (keepScheduling && crawlsScheduled < crawlsToPerform)
            {
                queueSema.WaitOne(); 
                if (urlQueue.Count != 0)
                {
                    crawlerSema.WaitOne();
                    if (crawlerQueue.Count != 0)
                    {
                        Console.WriteLine("Using available crawler");
                        //Use an available crawler
                        WebCrawler nextCrawler = crawlerQueue.Dequeue();
                        string nextURL = urlQueue.Dequeue();
                        new Thread(nextCrawler.Crawl).Start(nextURL);
                        crawlsScheduled++;
                    }
                    else if (numCrawlers <= maxCrawlers)
                    {
                        //Console.WriteLine("Create a crawler");
                        //Create a new crawler
                        WebCrawler newCrawler = new WebCrawler(numCrawlers++);
                        string nextURL = urlQueue.Dequeue();
                        new Thread(newCrawler.Crawl).Start(nextURL);
                        crawlsScheduled++;
                        //Console.WriteLine("Scheduled new crawler");
                    } // Otherwise, there are no available resources
                    crawlerSema.Release();
                }
                queueSema.Release();
                //Console.WriteLine("URL scheduler done");
            }

            //Stop Scheduling, clear crawler queue
            crawlerSema.WaitOne();
            crawlerQueue.Clear();
            crawlerSema.Release();
        }
        #endregion
    }
}
