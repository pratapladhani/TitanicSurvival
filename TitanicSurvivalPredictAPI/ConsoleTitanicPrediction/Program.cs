using System.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTitanicPrediction
{
    class Program
    {
        public class SurvivalPrediction
        {
            public bool WillSurvive { get; set; }
            public double SurvivalProbability { get; set; }
        }
        static void Main(string[] args)
        {
            //Console.WriteLine("There is a {0:p2} probability that you will {1}", 0.0188, "");
            MainAsync().Wait();
            Console.ReadLine();
        }

        static async Task MainAsync()
        {
            try
            {
                var a = await InvokeRequestResponse("female", 40);
                Console.WriteLine("There is a {0:p2} probability that you will {1}", a.SurvivalProbability, a.WillSurvive ? "survive" : "not survive");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred: {0}", ex.Message);
            }
            
        }

        public class StringTable
        {
            public string[] ColumnNames { get; set; }
            public string[] ColumnTypes { get; set; }
            public string[,] Values { get; set; }
        }
        private static async Task<SurvivalPrediction> InvokeRequestResponse(string sex, int age)
        {

            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {
                    Inputs = new Dictionary<string, StringTable>()
                    {
                        {
                            "InputRecord",
                            new StringTable()
                            {
                                ColumnNames= new string[]
                                {
                                    "PassengerId",
                                    "Survived",
                                    "Pclass",
                                    "Name",
                                    "Sex",
                                    "Age",
                                    "SibSp",
                                    "Parch",
                                    "Ticket",
                                    "Fare",
                                    "Cabin",
                                    "Embarked"
                                },
                                Values= new string[,]
                                {
                                    {
                                        "0",
                                        "0",
                                        "0",
                                        "value",
                                        sex,
                                        age.ToString(),
                                        "0",
                                        "0",
                                        "value",
                                        "0",
                                        "value",
                                        "value"
                                    }
                                }
                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>()
                    {
                    }
                };
                string apiKey = Properties.Settings.Default.TitanicAPIKey;// Replace this with the API key for the web service
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                string baseAddress = Properties.Settings.Default.AzureMLAPIBaseAddress;
                client.BaseAddress = new Uri(baseAddress);
                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);
                bool willSurvive = false;
                double survivalProbability = 0;
                if (response.IsSuccessStatusCode)
                {
                    string json= await response.Content.ReadAsStringAsync();
                    
                    //Console.WriteLine("Result: {0}", json);
                    
                    JObject results = JObject.Parse(json);
                    if (results["Results"]["PredictedOutput"]["value"]["Values"].HasValues)
                    {
                        var values = from p in results["Results"]["PredictedOutput"]["value"]["Values"].Values()
                                     select p;

                        int counter = 0;
                        
                        foreach (var item in values)
                        {
                            if (counter == 12) //"Scored Labels"

                            {
                                willSurvive = (item.ToString() == "0") ? false : true;
                            }
                            if (counter == 13) //"Scored Probabilities"
                            {
                                survivalProbability = (double)item;
                            }
                            counter++;
                        }
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp, which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                }
                return new SurvivalPrediction() { WillSurvive = willSurvive, SurvivalProbability = survivalProbability };
            }
        }
    }
}
