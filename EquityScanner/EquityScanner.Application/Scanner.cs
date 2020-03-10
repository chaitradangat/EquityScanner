using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

using EquityScanner.WebQueries;

using Newtonsoft.Json;

using System.Linq;

using System.IO;

namespace EquityScanner.Application
{
    public class Scanner
    {
        List<string> symbolList;

        WebCommands webCommand;

        string savefolderName;

        public Scanner()
        {
            var symbols = ConfigurationManager.AppSettings["SYMBOLS"];

            symbolList = new List<string>(symbols.Split(','));

            symbolList.RemoveAll(x => x == null);

            symbolList.RemoveAll(x => string.IsNullOrWhiteSpace(x));

            webCommand = new WebCommands();
        }

        public List<SymbolData> ScanForStocks()
        {
            List<SymbolData> symbolDatas = new List<SymbolData>();

            SaveDataToDisk();

            var files = Directory.GetFiles(savefolderName, "*.json");

            foreach (var file in files)
            {
                var symbolStringData = File.ReadAllText(file);

                var symbolData = ParseData(symbolStringData);

                symbolData = ConsolidateData(symbolData, 60);

                symbolData.SymbolName = file.Replace(savefolderName, "").Replace("\\", "").Replace(".json", "");

                symbolDatas.Add(symbolData);
            }

            return symbolDatas;
        }

        SymbolData ParseData(string symbolStringData)
        {
            SymbolData symbolData = new SymbolData();

            if (symbolData != null)
            {
                dynamic json = JsonConvert.DeserializeObject(symbolStringData);

                int idx = 0;

                if (json != null && json.chart != null && json.chart.result != null && json.chart.result[0] != null && json.chart.result[0].indicators != null &&
                    json.chart.result[0].indicators.quote[0] != null && json.chart.result[0].indicators.quote[0].high != null)
                {
                    foreach (var item in json.chart.result[0].indicators.quote[0].high)
                    {
                        symbolData.allVolumeData.Add((double?)json.chart.result[0].indicators.quote[0].volume[idx]);

                        symbolData.allPriceData.Add((double?)json.chart.result[0].indicators.quote[0].high[idx]);
                        ++idx;
                    }
                }
            }


            return symbolData;
        }

        SymbolData ConsolidateData(SymbolData symbolData, int Interval)
        {
            symbolData.allVolumeData.RemoveAll(x => x == null);

            symbolData.allPriceData.RemoveAll(x => x == null);

            symbolData.Volume = symbolData.allVolumeData.Sum();

            double? temp = 0;

            int idx = 0;

            foreach (var item in symbolData.allPriceData)
            {
                if (idx == 0)
                {
                    temp = item;

                    symbolData.PriceData.Add(temp);

                    temp = 0;
                }
                else if (idx % Interval == 0)
                {
                    symbolData.PriceData.Add(temp / (Interval - 1));

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
                symbolData.PriceData.Add(temp / ((symbolData.allPriceData.Count % Interval != 0 ? symbolData.allPriceData.Count % Interval : Interval) - 1));
                temp = 0;
            }

            return symbolData;
        }

        public List<SymbolData> AnalyzeContinousFall(List<SymbolData> symbolDatas)
        {
            HashSet<SymbolData> result = new HashSet<SymbolData>();

            foreach (var symbolData in symbolDatas)
            {
                double? temp = 9999999999999999999;

                bool match = true;

                int idx = 0;

                foreach (var price in symbolData.PriceData)
                {
                    if (price < temp)
                    {
                        temp = price;
                    }
                    else
                    {
                        match = false;
                        break;
                    }

                    ++idx;
                }

                if (match)
                {
                    if (symbolData.PriceData.Count > 0 && symbolData.PriceData[0] >= 40 && symbolData.Volume >= 500000 )
                    {
                        result.Add(symbolData);
                    }
                }
            }

            return result.OrderByDescending(x => x.Volume).ToList();
        }

        public List<SymbolData> AnalyzeSlope(List<SymbolData> symbolDatas)
        {
            symbolDatas.RemoveAll(x => x.Volume < 100000);

            symbolDatas.RemoveAll(x => x.PriceData[0] < 500);

            foreach (var symbolData in symbolDatas)
            {
                double? n = symbolData.allPriceData.Count;

                double? sumxy = 0, sumx = 0, sumy = 0, sumx2 = 0;

                for (int i = 0; i < symbolData.allPriceData.Count; i++)
                {
                    sumxy += i * symbolData.allPriceData[i];

                    sumx += i;
                    sumy += symbolData.allPriceData[i];
                    sumx2 += i * i;
                }

                symbolData.Slope = ((sumxy - sumx * sumy / n) / (sumx2 - sumx * sumx / n));
            }

            return symbolDatas.OrderBy(x=>x.Slope).ToList();
        }

        public List<SymbolData> AnalyzeContinousFallNew(List<SymbolData> symbolDatas)
        {
            symbolDatas.RemoveAll(x => x.Volume < 1000000);

            double? temp = 0;

            foreach (var symbolData in symbolDatas)
            {
                foreach (var price in symbolData.allPriceData)
                {
                    if (temp == 0)
                    {
                        temp = price;
                    }
                    else if (price < temp)
                    {
                        symbolData.FallFrequency++;

                        temp = price;
                    }
                    else if (price > temp)
                    {
                        symbolData.RiseFrequency++;

                        temp = price;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            symbolDatas.RemoveAll(x => x.FallFrequency < 200);

            symbolDatas.RemoveAll(x => x.FallFrequency == x.RiseFrequency);

            return
            symbolDatas.OrderByDescending(x => (x.FallFrequency - x.RiseFrequency)).ToList();
        }



        public void SaveDataToDisk()
        {
            savefolderName = DateTime.Today.ToShortDateString() + "_" + "PulledData";

            if (!Directory.Exists(savefolderName))
            {
                Directory.CreateDirectory(savefolderName);
            }

            foreach (var symbol in symbolList)
            {
                if (!File.Exists($@"{savefolderName}\{symbol}.json"))
                {
                    string symbolStringData = webCommand.GetGraphDataForSymbol(symbol);

                    if (symbolStringData != null)
                    {
                        File.WriteAllText($@"{savefolderName}\{symbol}.json", symbolStringData);
                    }
                }
            }
        }
    }

    public class SymbolData
    {
        public string SymbolName;

        internal int Interval;

        internal double? Volume;

        internal List<double?> PriceData;

        internal List<double?> allPriceData;

        internal List<double?> allVolumeData;

        internal double? Slope;

        internal int FallFrequency;

        internal int RiseFrequency;

        public SymbolData()
        {
            Slope = 0;

            SymbolName = "";

            allPriceData = new List<double?>();

            allVolumeData = new List<double?>();

            PriceData = new List<double?>();

            Interval = 0;

            Volume = 0;
        }
    }
}
