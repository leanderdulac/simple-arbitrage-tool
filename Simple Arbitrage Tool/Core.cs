﻿using Lostics.NCryptoExchange;
using Lostics.NCryptoExchange.CoinsE;
using Lostics.NCryptoExchange.CoinEx;
using Lostics.NCryptoExchange.Cryptsy;
using Lostics.NCryptoExchange.Model;
using Lostics.NCryptoExchange.Vircurex;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lostics.SimpleArbitrageBot;
using Lostics.NCryptoExchange.Bter;
using Lostics.NCryptoExchange.VaultOfSatoshi;

namespace Lostics.SimpleArbitrageTool
{
    public class Core
    {
        public const string COINS_E_CONFIGURATION_FILENAME = "coins_e.conf";
        public const string CRYPTSY_CONFIGURATION_FILENAME = "cryptsy.conf";
        public const string VAULT_OF_SATOSHI_CONFIGURATION_FILENAME = "vault_of_satoshi.conf";
        private const string PROPERTY_PUBLIC_KEY = "public_key";
        private const string PROPERTY_PRIVATE_KEY = "private_key";
        private const string CONFIGURATION_FOLDER = "Simple Arbitrage Tool";

        public static void Main()
        {
            try
            {
                using (BterExchange bter = new BterExchange())
                {
                    using (CoinExExchange coinEx = new CoinExExchange())
                    {
                        PublicPrivateKeyPair cryptsyConfiguration = LoadPublicPrivateKeyPair(CRYPTSY_CONFIGURATION_FILENAME);

                        using (CryptsyExchange cryptsy = new CryptsyExchange()
                            {
                                PublicKey = cryptsyConfiguration.PublicKey,
                                PrivateKey = cryptsyConfiguration.PrivateKey
                            }
                        )
                        {
                            PublicPrivateKeyPair vaultOfSatoshiConfiguration = LoadPublicPrivateKeyPair(VAULT_OF_SATOSHI_CONFIGURATION_FILENAME);

                            using (VoSExchange vaultOfSatoshi = new VoSExchange()
                                {
                                    PublicKey = vaultOfSatoshiConfiguration.PublicKey,
                                    PrivateKey = vaultOfSatoshiConfiguration.PrivateKey
                                }
                            )
                            {
                                using (VircurexExchange vircurex = new VircurexExchange())
                                {
                                    DoAnalysis(new List<IExchange>() {
                                        vaultOfSatoshi,
                                        bter,
                                        coinEx,
                                        cryptsy,
                                        vircurex
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (ExchangeConfigurationException e)
            {
                Console.WriteLine(e.Message);

                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }
        }

        private static void DoAnalysis(List<IExchange> exchanges)
        {
            const int maxCurrencies = 14;
            Dictionary<IExchange, List<Market>> validMarkets
                = MarketAnalyser.GetHighVolumeMarkets(exchanges, "BTC", maxCurrencies);
            MarketMatrix marketMatrix = new MarketMatrix(validMarkets);

            marketMatrix.UpdateAllPrices();
            marketMatrix.AddIndirectExchanges("DOGE", "LTC", "BTC");
            marketMatrix.AddIndirectExchanges("GDC", "LTC", "BTC");
            marketMatrix.AddIndirectExchanges("MEC", "LTC", "BTC");
            marketMatrix.AddIndirectExchanges("NET", "LTC", "BTC");
            marketMatrix.AddIndirectExchanges("PPC", "LTC", "BTC");
            marketMatrix.AddIndirectExchanges("QRK", "LTC", "BTC");
            marketMatrix.AddIndirectExchanges("WDC", "LTC", "BTC");

            // Strip any opportunities less than 1%, as non-viable

            foreach (ArbitrageOpportunity opportunity in marketMatrix
                .GetArbitrageOpportunities()
                .Where(opportunity => opportunity.ProfitPercentage >= 1.0m))
            {
                Console.WriteLine(opportunity.ToString());
            }

            Console.WriteLine("\nDone; press any key to exit");
            Console.ReadKey();
        }

        private static FileInfo GetConfigurationFilePath(string filename)
        {
            string applicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            DirectoryInfo folder = new DirectoryInfo(Path.Combine(applicationDataFolder, CONFIGURATION_FOLDER));

            if (!folder.Exists)
            {
                folder.Create();
            }

            return new FileInfo(Path.Combine(folder.FullName, filename));
        }

        private static Dictionary<string, string> LoadProperties(FileInfo configurationFile)
        {
            if (!configurationFile.Exists)
            {
                throw new ConfigurationMissingException("Missing configuration file \""
                    + configurationFile.FullName + "\".");
            }

            Dictionary<string, string> properties = new Dictionary<string, string>();

            using (StreamReader reader = new StreamReader(new FileStream(configurationFile.FullName, FileMode.Open)))
            {
                string line = reader.ReadLine();

                while (null != line)
                {
                    line = line.Trim();

                    // Ignore comment lines
                    if (!line.StartsWith("#"))
                    {
                        string[] parts = line.Split(new[] { '=' });
                        if (parts.Length >= 2)
                        {
                            string name = parts[0].Trim().ToLower();

                            properties.Add(name, parts[1].Trim());
                        }
                    }

                    line = reader.ReadLine();
                }
            }

            return properties;
        }

        /// <summary>
        /// Loads a public/private key pair from a configuration file. Useful for exchanges such as
        /// Cryptsy and Coins-E, which use this style for authentication.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static PublicPrivateKeyPair LoadPublicPrivateKeyPair(string filename)
        {
            FileInfo configurationFile = GetConfigurationFilePath(filename);
            Dictionary<string, string> properties;
            string publicKey;
            string privateKey;

            try
            {
                properties = LoadProperties(configurationFile);
            }
            catch (ConfigurationMissingException e)
            {
                WriteEmptyKeyPairFile(configurationFile);
                throw e;
            }

            if (!properties.TryGetValue(PROPERTY_PUBLIC_KEY, out publicKey))
            {
                throw new ConfigurationInvalidException("No public key specified in configuration file \""
                    + configurationFile.FullName + "\"; expected key with property name \""
                    + PROPERTY_PUBLIC_KEY + "\".");
            }

            if (!properties.TryGetValue(PROPERTY_PRIVATE_KEY, out privateKey))
            {
                throw new ConfigurationInvalidException("No public key specified in configuration file \""
                    + configurationFile.FullName + "\"; expected key with property name \""
                    + PROPERTY_PRIVATE_KEY + "\".");
            }

            return new PublicPrivateKeyPair() {
                PublicKey = publicKey,
                PrivateKey = privateKey
            };
        }

        /// <summary>
        /// Writes out a blank configuration file at the given path, ready for the user to fill in details of
        /// their public & private key.
        /// </summary>
        /// <param name="configurationFile"></param>
        private static void WriteEmptyKeyPairFile(FileInfo configurationFile)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(configurationFile.FullName, FileMode.CreateNew)))
            {
                writer.WriteLine("# Configuration file for specifying API public & private key.");
                writer.WriteLine(PROPERTY_PUBLIC_KEY + "=");
                writer.WriteLine(PROPERTY_PRIVATE_KEY + "=");
            }
        }

        public class PublicPrivateKeyPair
        {
            public PublicPrivateKeyPair()
            {

            }

            public string PublicKey { get; set; }
            public string PrivateKey { get; set; }
        }
    }
}
