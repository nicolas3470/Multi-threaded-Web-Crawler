using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data;
using Isis;


namespace AzureConsole
{
    public class SmallGroup : Group
    {
        #region Properties

        /// <summary>
        /// Group name
        /// </summary>
        public string name;
        private int rank;
        public int acknowledged = 0;
        private Dictionary<string, string> acknowledgedict = new Dictionary<string, string>();

        /// <summary>
        /// Timeout and EOLMarker for query
        /// </summary>
        public static Isis.Timeout myTO = new Isis.Timeout(1000, Isis.Timeout.TO_NULLREPLY);
        public static EOLMarker myEOL = new EOLMarker();

        #endregion

        #region Small scale handlers

        /// <summary>
        /// Query types
        /// </summary>
        public static int COUNT = 0;
        public static int ALLRECEIVE = 1;
        public static int PAPERQUERY = 2;
        public static int CHECKPOINT = 9;
        public static int END = 10;

        /// <summary>
        /// Query handlers
        /// </summary>
        /// <param name="sa">Search keywords</param>
        /// <param name="id">Query id</param>
        //public delegate void myDels(string sa);
        public delegate void myDels(int id, string sa);

        public delegate void mainDel();

        #endregion

        #region Constructor

        public SmallGroup(string name)//, int size, int rank, mainDel mainCall)
            : base(name)
        {
            this.name = name;

            this.Handlers[COUNT] += (myDels)delegate(int id, string keywords)
            {
                try
                {
                    Console.WriteLine("Ready to acknowledge the master");
                    //P2PSend(Program.MasterAddr, ScholarSmall.ALLRECEIVE, rank, System.Text.Encoding.Default.GetBytes(endtime));
                    this.Reply(System.Environment.ProcessorCount);                    
                }
                catch (Exception e)
                {
                    throw new Exception("ScholarSmall paperbkw " + e.Message);
                }
            };
        }

        #endregion
    }
}
