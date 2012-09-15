using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace _5412_Project
{
    public class IndexScheduler
    {
        #region Private Fields
        private int maxIndexers;
        #endregion

        #region Public Fields
        public Semaphore dataSema { get; private set; }
        public Semaphore indexerSema { get; private set; }
        public Queue<Tuple<string,string,DateTime>> dataQueue { get; private set; }
        public Queue<Indexer> indexerQueue { get; private set; }
        public bool keepIndexing { get; set; }
        public int numIndexers { get; private set; }
        #endregion

        #region Constructor
        public IndexScheduler(int maxIndex)
        {
            dataQueue = new Queue<Tuple<string,string,DateTime>>();
            dataSema = new Semaphore(1, 1);
            indexerQueue = new Queue<Indexer>();
            indexerSema = new Semaphore(1, 1);
            keepIndexing = true;
            maxIndexers = maxIndex;
            numIndexers = 1;
        }
        #endregion

        #region Main Thread
        public void index()
        {
            //Index Data
            while (keepIndexing)
            {
                dataSema.WaitOne();
                if (dataQueue.Count != 0)
                {
                    //Console.WriteLine("Need an indexer");
                    indexerSema.WaitOne();
                    if (indexerQueue.Count != 0)
                    {
                        Console.WriteLine("Using available Indexer");
                        //Use an available indexer
                        Indexer nextIndexer = indexerQueue.Dequeue();
                        Tuple<string,string,DateTime> nextData = dataQueue.Dequeue();
                        new Thread(nextIndexer.Index).Start(nextData);
                    }
                    else if (numIndexers < maxIndexers)
                    {
                        Console.WriteLine("Creating new indexer");
                        //Create a new Indexer
                        Indexer newIndexer = new Indexer(numIndexers++);
                        Tuple<string,string,DateTime> nextData = dataQueue.Dequeue();
                        new Thread(newIndexer.Index).Start(nextData);
                    } // Otherwise, there are no available resources
                    indexerSema.Release();
                }
                dataSema.Release();
            }

            //Stop Scheduling, clear indexer queue
            indexerSema.WaitOne();
            indexerQueue.Clear();
            indexerSema.Release();
        }
        #endregion
    }
}
