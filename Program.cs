using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace _5412_Project 
{
    public class Program
    {
        #region Debugging Fields
        private static int maxCrawlers = 10;
        private static int maxIndexers = 10;
        private static int autoCacheDays = 3;
        private static bool enableAutoCache = false;
        private static int numCrawlsToPerform = 30;
        #endregion

        #region Public Fields
        public static string searchString {private get; set;}
        public static bool searchMade {private get; set;}
        public static DataStorage database { get; private set; }
        public static URLScheduler urlScheduler { get; private set; }
        public static IndexScheduler indexScheduler { get; private set; }
        public static bool debugMode = true;
        public static string startURL = "http://dmoz.org/";
        #endregion

        #region UI Helper Functions
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        #endregion

        #region Main Program Thread
        static void Main(string[] args) 
        {
            Console.Title = "5412 Crawler";


            /*Parts needed
             * 1. Storage - Stores all the program data
             * 2. URL Scheduler - assigns URLs to web crawlers (and creates crawlers)
             * 3. Index Scheduler - assigns workers to index content (and creates workers)
             * 4. Web crawlers - Get data from page, and return content to Index scheduler
             * 5. Index workers - Process data from the web crawl, and add new links to URL scheduler
             * 6. User Interface - Simple interface that takes string and seaches storage for it
             * */

            //Set-up storage
            database = new DataStorage(DateTime.Now, autoCacheDays);
            if (enableAutoCache)
            {
                new Thread(database.autoCache).Start();
            }

            //Set up URL scheduler
            urlScheduler = new URLScheduler(maxCrawlers, numCrawlsToPerform, startURL);
            new Thread(urlScheduler.schedule).Start();

            //Set up Index Scheduler
            indexScheduler = new IndexScheduler(maxIndexers);
            new Thread(indexScheduler.index).Start();

            //Launch Interface
            //Console.WriteLine("Type 's' to enter search mode or 'q' to quit");
            //char key = Console.ReadKey().KeyChar;
            //while (key != 's' && key != 'q') 
            //{
            //    Console.WriteLine();
            //    Console.WriteLine("'s' and 'q' are the only accepted keys");
            //    key = Console.ReadKey().KeyChar;
            //}
            //Console.WriteLine();
            //if (key == 's') 
            //{
            //    IntPtr hWnd = FindWindow(null, Console.Title);
            //    if (hWnd != IntPtr.Zero)
            //    {
            //        ShowWindow(hWnd, 0);
            //    }
            //    SearchWindow ui = new SearchWindow();
            //    ui.ShowDialog();
            //    ShowWindow(hWnd, 1);
            //    SetForegroundWindow(hWnd);
            //}

            //if (searchString != "" && searchMade)
            //{
            //    Console.WriteLine("Search string was {0}", searchString);
            //}

            //Debugging
            if (debugMode)
            {
                Console.WriteLine("Type 'q' to quit crawling");
                char key = Console.ReadKey().KeyChar;
                while (key != 'q')
                {
                    Console.WriteLine("");
                    Console.WriteLine("'q' is the only accepted key");
                    key = Console.ReadKey().KeyChar;
                }
                Console.WriteLine("");
                stopCrawling();
                //Console.WriteLine("Stopped Crawling");
                createReport();
                //Console.WriteLine("Created Report");
            }

            //string test = "<link rel=\"stylesheet\" type=\"text/css\" href=\"http://o.aolcdn.com/os/dmoz/editors/css/dmoznew.jpg\">";
            //MatchCollection urls = Regex.Matches(test, "href=\"[a-zA-Z./:&\\d_-]+\"");
            //string url;
            //foreach (Match match in urls)
            //{
            //    url = match.Value.Replace("href=\"", "");
            //    url = url.Substring(0, url.IndexOf("\""));
            //    Console.WriteLine(url);
            //    if (unwantedExtensions.Any(url.Contains)) Console.WriteLine("BAD");
            //    //if (url.EndsWith(".css")) Console.WriteLine("BAD");
            //}

            //string currentData = "http://dmoz.org/|n|i|c|c|";
            //string[] urlAndData = Regex.Split(currentData, @"\|n\|i\|c\|c\|");
            //string currentURL = urlAndData[0];
            //currentData = urlAndData[1];
            //Console.WriteLine("\n\nURL:\n" + currentURL + "\n\n");
            //Console.WriteLine("\n\nData:\n" + currentData + "\n\n");

            //Reached end of Code
            Console.WriteLine("End of Program. Type Any Key to Exit.");

            //Press any key to exit
            Console.ReadKey();
        }
        #endregion

        #region Internal Helper Methods
        internal static void stopCrawling()
        {
            urlScheduler.keepScheduling = false;
            database.keepAutoCaching = false;
            indexScheduler.keepIndexing = false;
        }

        internal static void createReport()
        {
            var report = database.dataReports();
            //Console.WriteLine("Got Database Reports");
            //Console.WriteLine("Report 1: " + report.Item1);
            //Console.WriteLine("Report 2: " + report.Item2);
            File.WriteAllText("C:\\Users\\Nicolas\\Desktop\\Nick\\5412 Project\\URL Report.txt",report.Item1);
            File.WriteAllText("C:\\Users\\Nicolas\\Desktop\\Nick\\5412 Project\\Word Report.txt", report.Item2);
            File.WriteAllText("C:\\Users\\Nicolas\\Desktop\\Nick\\5412 Project\\Overall Report.txt", report.Item3);
        }
        #endregion
    }
}
