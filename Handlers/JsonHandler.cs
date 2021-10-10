using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FloppaFlipper.Handlers
{
    public static class JsonHandler
    {
        /// <summary>
        /// Fetches and modifies the JSON from the web endpoint provided.
        /// </summary>
        /// <param name="endpoint">Endpoint to load the JSON from.</param>
        /// <param name="id">Item id. Leave to 0 to fetch all items.</param>
        /// <returns>Modified json string as an JSON array</returns>
        public static async Task<string> FetchPriceJson(string endpoint, uint id = 0)
        {
            string finalEndpoint = endpoint;
            if (id != 0) finalEndpoint += id;

            string json = await FetchJsonFromEndpoint(finalEndpoint);

            // If the endpoint is of format {"data":{"ID":{"... ...}},"timestamp":UNIX_TIMESTAMP}
            if (endpoint == ConfigHandler.Config._5MinPricesApiEndpoint ||
                endpoint == ConfigHandler.Config._1HourPricesApiEndpoint ||
                endpoint == ConfigHandler.Config._6HourPricesApiEndpoint ||
                endpoint == ConfigHandler.Config._24HourPricesApiEndpoint)
            {
                // Trim out the timestamp, we don't need it
                json = json[..json.LastIndexOf(',')];
                
                // Re-add the last rbracket
                json = json.Insert(json.Length, "}");

                // Trim out the '{"data":' -part, and the last '}'
                json = json[8..];
                json = json.Remove(json.Length - 1);

                // Convert the json to an array by adding '[' and ']' to the ends of the json.
                json = json.Insert(0, "[");
                json = json.Insert(json.Length, "]");
            }
            // If the endpoint is of format {"data":{"ID":{"... ...}}}
            else if (endpoint == ConfigHandler.Config.LatestPricesApiEndpoint)
            {
                // Trim out the '{"data":' -part, and the last '}'
                json = json[8..];
                json = json.Remove(json.Length - 1);

                // Convert the json to an array by adding '[' and ']' to the ends of the json.
                json = json.Insert(0, "[");
                json = json.Insert(json.Length, "]");
            }
            else if (endpoint == ConfigHandler.Config.TimeSeriesApiEndpoint)
            {
                // Trim out the '{"data":' -part
                json = json[8..];
                
                // Trim out the timestamp, we don't need it
                json = json[..json.LastIndexOf(',')];
            }

            return json;
        }

        /// <summary>
        /// Fetches the unmodified JSON from the web endpoint provided.
        /// </summary>
        /// <param name="endpoint">Endpoint to load the JSON from.</param>
        /// <returns>Full json string</returns>
        public static async Task<string> FetchJsonFromEndpoint(string endpoint)
        {
            // Connect to the item info API...
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.UserAgent = "FloppaFlipper - Discord: Japsu#8887";
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            
            Console.WriteLine("[CONNECTION]: " + response.StatusCode);

            // Get the content as a JSON string
            await using Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            string infoJsonString = await reader.ReadToEndAsync();

            return infoJsonString;
        }
    }
}