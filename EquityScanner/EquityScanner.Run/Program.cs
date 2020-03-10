using System;

using System.Collections.Generic;

using System.Linq;

using EquityScanner.Application;



namespace EquityScanner.Run
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Scanner scanner = new Scanner();

            var symbolDatas = scanner.ScanForStocks();

            var continousFall = scanner.AnalyzeContinousFallNew(symbolDatas);

            foreach (var item in continousFall)
            {
                Console.WriteLine(item.SymbolName);
            }


            Console.ReadLine();
        }
    }
}
