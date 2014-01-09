﻿using Lostics.NCryptoExchange;
using Lostics.NCryptoExchange.CoinsE;
using Lostics.NCryptoExchange.Cryptsy;
using Lostics.NCryptoExchange.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lostics.SimpleArbitrageBot
{
    public class Core
    {
        public const string COINS_E_CONFIGURATION_FILENAME = "coins_e.conf";
        public const string CRYPTSY_CONFIGURATION_FILENAME = "cryptsy.conf";

        public static void Main()
        {
            using (CryptsyExchange cryptsy = CryptsyExchange.GetExchange(FindCryptsyConfigurationFile()))
            {
                using (CoinsEExchange coinsE = CoinsEExchange.GetExchange(FindCoinsEConfigurationFile()))
                {
                    DoTrading(new List<AbstractExchange>() {
                        cryptsy,
                        coinsE
                    });
                }
            }
        }

        private static void DoTrading(List<AbstractExchange> exchanges)
        {
            Dictionary<AbstractExchange, List<Market>> validMarkets = MarketAnalyser.GetHighVolumeMarkets(exchanges);

            foreach (AbstractExchange exchange in exchanges)
            {
                Console.WriteLine("High volume currencies on "
                    + exchange.Label + ": ");
                foreach (Market market in validMarkets[exchange])
                {
                    Console.WriteLine(market.ToString());
                }
            }

            Console.WriteLine("\nDone");
            Console.ReadKey();
        }

        private static FileInfo FindCoinsEConfigurationFile()
        {
            return new FileInfo(Path.Combine(GetConfigurationDirectory().FullName, COINS_E_CONFIGURATION_FILENAME));
        }

        private static DirectoryInfo GetConfigurationDirectory()
        {
            return new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent;
        }

        private static FileInfo FindCryptsyConfigurationFile()
        {
            return new FileInfo(Path.Combine(GetConfigurationDirectory().FullName, CRYPTSY_CONFIGURATION_FILENAME));
        }
    }
}