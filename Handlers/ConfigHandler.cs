using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FloppaFlipper.Handlers
{
    public static class ConfigHandler
    {
        public static Config Config;
        
        /// <summary>
        /// Initializes and reads the config.
        /// </summary>
        /// <returns>If the config was read successfully.</returns>
        public static bool Init()
        {
            Config = new Config();

            DeserializeSavedGuildChannelBindings();
            
            return ReadAllSettings();
        }

        private static bool ReadAllSettings()  
        {  
            try  
            {  
                var appSettings = ConfigurationManager.AppSettings;  

                // Check if the config is empty
                if (appSettings.Count == 0)  
                {  
                    Console.WriteLine("[ERROR]: Config is empty!");

                    return false;
                }

                // Loop all of the config entries
                foreach (string key in appSettings.AllKeys)
                {
                    string value = appSettings[key];

                    if (string.IsNullOrEmpty(value))
                    {
                        Console.WriteLine("Skipping an empty config value of " + key);
                        
                        continue;
                    }
                        
                    // Assign the config value to correct variable
                    switch (key)
                    {
                        case nameof(Config.BotToken):
                        {
                            Config.BotToken = value;
                            break;
                        }
                            
                        case nameof(Config.RefreshRate):
                        {
                            Config.RefreshRate = GetIntValue(value);
                            break;
                        }
                            
                        case nameof(Config.ItemNotificationCooldown):
                        {
                            Config.ItemNotificationCooldown = GetIntValue(value);
                            break;
                        }
                            
                        case nameof(Config.MaxSparklineDatasetLength):
                        {
                            Config.MaxSparklineDatasetLength = GetIntValue(value);
                            break;
                        }
                            
                        case nameof(Config.MinTradedVolume):
                        {
                            Config.MinTradedVolume = GetIntValue(value);
                            break;
                        }
                            
                        case nameof(Config.MinBuyPrice):
                        {
                            Config.MinBuyPrice = GetIntValue(value);
                            break;
                        }
                            
                        case nameof(Config.LatestPricesApiEndpoint):
                        {
                            Config.LatestPricesApiEndpoint = value;
                            break;
                        }
                            
                        case nameof(Config._1HourPricesApiEndpoint):
                        {
                            Config._1HourPricesApiEndpoint = value;
                            break;
                        }
                            
                        case nameof(Config._5MinPricesApiEndpoint):
                        {
                            Config._5MinPricesApiEndpoint = value;
                            break;
                        }
                            
                        case nameof(Config._6HourPricesApiEndpoint):
                        {
                            Config._6HourPricesApiEndpoint = value;
                            break;
                        }
                            
                        case nameof(Config._24HourPricesApiEndpoint):
                        {
                            Config._24HourPricesApiEndpoint = value;
                            break;
                        }
                            
                        case nameof(Config.TimeSeriesApiEndpoint):
                        {
                            Config.TimeSeriesApiEndpoint = value;
                            break;
                        }
                            
                        case nameof(Config.MappingApiEndpoint):
                        {
                            Config.MappingApiEndpoint = value;
                            break;
                        }
                            
                        case nameof(Config.IconsApiEndpoint):
                        {
                            Config.IconsApiEndpoint = value;
                            break;
                        }
                            
                        case nameof(Config.WikiApiEndpoint):
                        {
                            Config.WikiApiEndpoint = value;
                            break;
                        }
                            
                        case nameof(Config.PriceInfoPageApiEndpoint):
                        {
                            Config.PriceInfoPageApiEndpoint = value;
                            break;
                        }
                            
                        case nameof(Config.GeTrackerPageApiEndpoint):
                        {
                            Config.GeTrackerPageApiEndpoint = value;
                            break;
                        }

                        case nameof(Config.BlacklistedItemIds):
                        {
                            // Get the blacklist values as string array
                            string[] arr = value.Split(',');
                            
                            // Create the actual blacklist array
                            uint[] values = new uint[arr.Length];
                            
                            // Parse from the string version to the int version
                            for (int i = 0; i < arr.Length; i++)
                            {
                                if (uint.TryParse(arr[i], out uint result))
                                {
                                    values[i] = result;
                                }
                                else
                                {
                                    Console.WriteLine("[ERROR]: BlacklistedItemIds contains an invalid value.");
                                    return false;
                                }
                            }

                            Config.BlacklistedItemIds = values;
                            break;
                        }
                        
                        case nameof(Config.MinPriceChangePercentage):
                        {
                            if (!double.TryParse(value, NumberStyles.Number, CultureInfo.CreateSpecificCulture ("en-US"), out double result))
                            {
                                Console.WriteLine("[ERROR]: MinPriceChangePercentage contains an invalid value: " + value);
                                
                                continue;
                            }

                            Config.MinPriceChangePercentage = result;
                            break;
                        }
                    }
                }
            }  
            catch (ConfigurationErrorsException)  
            {  
                Console.WriteLine("[ERROR]: Error reading the config!");

                return false;
            }
            
            return true;
        }

        public static void AddGuildChannelBinding(string guild, string channel)
        {
            Config.GuildChannelDict ??= new Dictionary<string, string>();

            if (Config.GuildChannelDict.ContainsKey(guild))
            {
                Config.GuildChannelDict[guild] = channel;
            }
            else
            {
                Config.GuildChannelDict.Add(guild, channel);
            }
                
            SerializeSavedGuildChannelBindings();
        }

        public static void RemoveGuildChannelBinding(string guild)
        {
            Config.GuildChannelDict.Remove(guild);
                
            SerializeSavedGuildChannelBindings();
        }

        private static void SerializeSavedGuildChannelBindings()
        {
            XElement xElem = new XElement(
                "guildChannelBindings",
                Config.GuildChannelDict.Select(x => new XElement("guildChannelBinding", new XAttribute("key", x.Key),
                    new XAttribute("value", x.Value)))
            );
            xElem.Save("GuildChannelBindings.txt");
        }

        private static void DeserializeSavedGuildChannelBindings()
        {
            try
            {
                XElement xElem2 = XElement.Load("GuildChannelBindings.txt");
                Dictionary<string, string> newDict = xElem2.Descendants("guildChannelBinding")
                    .ToDictionary(x => (string)x.Attribute("key"), x => (string)x.Attribute("value"));

                Config.GuildChannelDict = newDict;
            }
            catch (Exception)
            {
                Console.WriteLine("[WARNING]: There was an error deserializing the guild/channel bindings. The file may not exist yet.");

                Config.GuildChannelDict ??= new Dictionary<string, string>();
            }
        }

        private static int GetIntValue(string value)
        {
            if (int.TryParse(value, out int intValue))
            {
                return intValue;
            }

            Console.WriteLine("[ERROR]: Could not parse a config entry.");

            return -1;
        }
    }
}