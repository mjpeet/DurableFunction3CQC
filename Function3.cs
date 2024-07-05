using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using DurableFunction3CQC.Util;

namespace DurableFunction3CQC
{
    public static class Function3
    {

        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string BaseUri = "https://api.service.cqc.org.uk/public/v1";
        private static readonly string InitialUri = "/locations?page=1&perPage=1000";
        private static string KeyVaultName = System.Environment.GetEnvironmentVariable("KeyVaultName", EnvironmentVariableTarget.Process);
        // "KeyVaultName": "mykeyvlt20240613192800"
        private static string SecretKeyName = System.Environment.GetEnvironmentVariable("SecretKeyName", EnvironmentVariableTarget.Process);
        // "SecretKeyName": "CQCKeyName"
        private static string SecretValueName = System.Environment.GetEnvironmentVariable("SecretValueName", EnvironmentVariableTarget.Process);
        // "SecretValueName": "CQCKeyValue"


        [Function("Function3_OrchestratorFunction")]
        public static async Task<AnalyzeData> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(Function3));

            logger.LogInformation("Running Function3_OrchestratorFunction");
            List<Location> allLocations = new List<Location>();
            string currentPageUrl = InitialUri;

            allLocations = await context.CallActivityAsync<List<Location>>("Function3_ActivityFunction_GetLocations", currentPageUrl);
            AnalyzeData _ret = await context.CallActivityAsync<AnalyzeData>("Function3_ActivityFunction_AnalyzeData", allLocations);

            return _ret;
        }

        [Function("Function3_ActivityFunction_AnalyzeData")]
        public static AnalyzeData AnalyzeData([ActivityTrigger] List<Location> allLocations, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("AnalyzeData");
            logger.LogInformation("AnalyzeData for list of " + allLocations.Count.ToString());

            AnalyzeData ret = new AnalyzeData();
            ret.Total = allLocations.Count;
            List<PartialPostCodeCount> data = new List<PartialPostCodeCount>();

            // Basic analysis: count locations by postalCode prefix
            var prefixCounts = allLocations
                .GroupBy(loc => loc.PostalCode.Substring(0, 2))
                .Select(group => new { Prefix = group.Key, Count = group.Count() })
                .ToList();

            // Print prefix counts
            foreach (var prefixCount in prefixCounts)
            {
                Console.WriteLine($"Prefix: {prefixCount.Prefix}, Count: {prefixCount.Count}");

                var _item = new PartialPostCodeCount();
                _item.PostCodePrefix = prefixCount.Prefix;
                _item.Count = prefixCount.Count.ToString();

                data.Add(_item);
            }

            ret.data = data;

            return ret;
        }

        [Function("Function3_ActivityFunction_SayHello3")]
        public static string SayHello3([ActivityTrigger] string name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello3");
            logger.LogInformation("Saying hello to {name}.", name);
            return $"Hello {name}!";
        }

        [Function("Function3_ActivityFunction_GetLocations")]
        public static async Task<List<Location>> GetLocations_Activity_Function3([ActivityTrigger] string uri, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function3_ActivityFunction_GetLocations");
            logger.LogInformation("Saying hello to {uri}.", uri);

            string currentPageUrl = InitialUri;
            List<Location> locations = new List<Location>();

            // get secrets
            string secretKeyAzure = await SecretsHelper.GetSecret(SecretKeyName, KeyVaultName, executionContext);
            logger.LogInformation($"Your secret key is '{secretKeyAzure}'.");
            string secretValueAzure = await SecretsHelper.GetSecret(SecretValueName, KeyVaultName, executionContext);
            logger.LogTrace($"Your secret value is '{secretValueAzure}'.");

            int i = 0;

            while (!string.IsNullOrEmpty(uri) && i < 2)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, BaseUri + uri);
                request.Headers.Add(secretKeyAzure, secretValueAzure);

                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);


                if (apiResponse != null)
                {
                    locations.AddRange(apiResponse.Locations);
                    currentPageUrl = apiResponse.NextPageUri;
                }

                i++;
            }

            return locations;
        }


        [Function("Function3_ClientFunction")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext,
            ExecutionContext context,
            ILogger log)
        {
            ILogger logger = executionContext.GetLogger("Function3_ClientFunction");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync("Function3_OrchestratorFunction");

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // return http 412 if the 3 envrnment variables are not set
            if(string.IsNullOrEmpty(KeyVaultName) || string.IsNullOrEmpty(SecretKeyName) || string.IsNullOrEmpty(SecretValueName))
            {
                var response = req.CreateResponse(HttpStatusCode.PreconditionFailed);
                return response;

            }

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}
