using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _5412_Project
{
    public class URLData
    {
        #region Public fields
        public string url { get; private set; }
        public DateTime dateModified { get; set; }
        public Dictionary<string, int> wordFrequencies { get; private set; }
        #endregion

        #region Constructor
        public URLData(string URL, DateTime modifyDate, Dictionary<string, int> wordCounts)
        {
            url = URL;
            dateModified = modifyDate;
            wordFrequencies = wordCounts;
        }
        #endregion
    }
}
