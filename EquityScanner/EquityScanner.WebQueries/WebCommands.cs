using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Net;

namespace EquityScanner.WebQueries
{
    public class WebCommands
    {
        string GraphURL;


        public WebCommands()
        {
            GraphURL = ConfigurationManager.AppSettings["STOCKGRAPHURL"];
        }

        public string GetGraphDataForSymbol(string symbol,string range="1d")
        {
            try
            {
                string _GraphURL = string.Format(GraphURL, symbol, range);

                _GraphURL = _GraphURL.Replace(" ", "%20");

                WebClient wc = new WebClient();

                var graphData = wc.DownloadString(_GraphURL);

                return graphData;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
