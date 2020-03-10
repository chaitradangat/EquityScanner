using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

using EquityScanner.WebQueries;

using Newtonsoft.Json;

using System.Linq;

namespace EquityScanner.Application
{
    public class Scanner
    {
        List<string> symbolList;

        WebCommands webCommand;

        public Scanner()
        {
            var symbols = ConfigurationManager.AppSettings["SYMBOLS"];

            symbolList = new List<string>(symbols.Split(','));

            symbolList.RemoveAll(x => x == null);

            symbolList.RemoveAll(x => string.IsNullOrWhiteSpace(x));

            webCommand = new WebCommands();
        }

        public List<string> ScanForStocks()
        {
            foreach (var symbol in symbolList)
            {
                string symbolStringData = webCommand.GetGraphDataForSymbol(symbol);

                SymbolData symbolData = ParseData(symbol);

                symbolData = ConsolidateData(symbolData, 5);
            }

            return null;
        }

        SymbolData ParseData(string symbolStringData)
        {
            SymbolData symbolData = new SymbolData();

            if (symbolData != null)
            {
                dynamic json = JsonConvert.DeserializeObject(symbolStringData);

                //json.chart.result[0].indicators.quote[0].high[index];

                //json.chart.result[0].indicators.quote[0].volume[index];

                int idx = 0;

                if (json!=null && json.chart!=null && json.chart.result != null && json.chart.result[0] != null && json.chart.result[0].indicators != null && 
                    json.chart.result[0].indicators.quote[0] != null && json.chart.result[0].indicators.quote[0].high != null)
                {
                    foreach (var item in json.chart.result[0].indicators.quote[0].high)
                    {
                        symbolData.allVolumeData.Add(json.chart.result[0].indicators.quote[0].volume[idx]);

                        symbolData.allPriceData.Add(json.chart.result[0].indicators.quote[0].high[idx]);
                        ++idx;
                    }
                }
            }


            return symbolData;
        }

        SymbolData ConsolidateData(SymbolData symbolData,int Interval)
        {
            symbolData.allVolumeData.RemoveAll(x => x == null);

            symbolData.allPriceData.RemoveAll(x => x == null);

            symbolData.Volume = symbolData.allVolumeData.Sum();

            double? temp = 0;

            int idx = 0;

            foreach (var item in symbolData.allPriceData)
            {
                if (idx % Interval == 0 && idx != 0)
                {
                    symbolData.PriceData.Add(temp);

                    temp = 0;
                }
                else
                {
                    temp += item;
                }
                ++idx;
            }
            if (temp != 0)
            {
                symbolData.PriceData.Add(temp);
                temp = 0;
            }

            return symbolData;
        }

        bool AnalyzeContinousFall(SymbolData symbolData)
        {



            return true;
        }
    }

    class SymbolData
    {
        internal int Interval;

        internal double? Volume;

        internal List<double?> PriceData;

        internal  List<double?> allPriceData;

        internal  List<double?> allVolumeData;

        public SymbolData()
        {
            allPriceData = new List<double?>();

            allVolumeData = new List<double?>();

            PriceData = new List<double?>();

            Interval = 0;

            Volume = 0;
        }
    }
}
