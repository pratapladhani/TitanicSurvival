using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using TitanicSurvivalPredictAPI.Models;

namespace TitanicSurvivalPredictAPI.Controllers
{
    public class PredictionController : ApiController
    {
        public class StringTable
        {
            public string[] ColumnNames { get; set; }
            public string[,] Values { get; set; }
        }

        

        [HttpGet]
        [ResponseType(typeof(SurvivalPrediction))]
        [SwaggerResponse(HttpStatusCode.OK,
            Description = "OK",
            Type = typeof(SurvivalPrediction))]
        [SwaggerResponse(HttpStatusCode.InternalServerError, 
            Description ="Internal Server Error")]        
        public async Task<HttpResponseMessage> PredictSurvival(Sex sex, int age)
        {
            try
            {
                var result = await InvokeServiceAPI(sex, age);
                return Request.CreateResponse<SurvivalPrediction>(HttpStatusCode.OK, result);
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<SurvivalPrediction> InvokeServiceAPI(Sex sex, int age)
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
                                        sex.ToString().ToLower(),
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
                const string apiKey = "CRBQ+DAzdrTLKltqKprAAIf5E60jarfCah1fdP25lK/aMmLGbKBV6rihlJzL/IvxML/M1nEDcmjsWDVBK+ucBQ=="; // Replace this with the API key for the web service
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                client.BaseAddress = new Uri("https://ussouthcentral.services.azureml.net/workspaces/21c8cb7960ca4fbb8584012370c65427/services/2b26e2941a1c4cc0bcd7bba3fcafc0e4/execute?api-version=2.0&details=true");

                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);
                bool willSurvive = false;
                double survivalProbability = 0;
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

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
                    //Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp, which are useful for debugging the failure
                    //Console.WriteLine(response.Headers.ToString());

                    //string responseContent = await response.Content.ReadAsStringAsync();
                    throw new HttpResponseException(response);
                    //Console.WriteLine(responseContent);
                }
                return new SurvivalPrediction() { Age=age, Sex=sex.ToString(), WillSurvive = willSurvive, SurvivalProbability = survivalProbability };
            }
        }
    }
}
