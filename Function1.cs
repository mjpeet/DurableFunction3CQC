using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DurableFunction3CQC.Util;

namespace DurableFunction3CQC
{
    public static class Function1
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


        [Function("Function1_Orchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(Function1));
            logger.LogInformation("Saying hello.");
            var outputs = new List<string>();
            string currentPageUrl = InitialUri;

            // Replace name and input with values relevant for your Durable Functions Activity
            outputs.Add(await context.CallActivityAsync<string>("Function1_SayHello", "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>("Function1_SayHello", "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>("Function1_SayHello", "London"));
            outputs.Add(await context.CallActivityAsync<string>("Function1_SayHello", "London 2x"));
            outputs.Add(await context.CallActivityAsync<string>("Function1_SayHello", "London 3x"));

            outputs.Add(await context.CallActivityAsync<string>("Function1_GetLocations_str", currentPageUrl));

            return outputs;
        }

        [Function("Function1_SayHello")]
        public static string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function1_SayHello");
            logger.LogInformation("Saying hello to {name}.", name);
            return $"Hello {name}!";
        }

        [Function("Function1_GetLocations_str")]
        public static async Task<string> GetLocations_str([ActivityTrigger] string currentPageUri, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function1_GetLocations_str");
            logger.LogInformation("Start GetLocations from {currentPageUri}.", currentPageUri);

            List<Location> allLocations = new List<Location>();

            var request = new HttpRequestMessage(HttpMethod.Get, BaseUri + currentPageUri);

            // get secrets
            string secretKeyAzure = await SecretsHelper.GetSecret(SecretKeyName, KeyVaultName, executionContext);
            logger.LogInformation($"Your secret key is '{secretKeyAzure}'.");
            string secretValueAzure = await SecretsHelper.GetSecret(SecretValueName, KeyVaultName, executionContext);
            logger.LogTrace($"Your secret value is '{secretValueAzure}'.");

            request.Headers.Add(secretKeyAzure, secretValueAzure);


            try
            {
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

                if (apiResponse != null)
                {
                    allLocations.AddRange(apiResponse.Locations);
                    currentPageUri = apiResponse.NextPageUri;
                }
            }
            catch (Exception e)
            {
                logger.LogTrace(e.Message);
            }
            

            return JsonConvert.SerializeObject(allLocations);
        }

        [Function("Function1_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function1_HttpStart");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync("Function1_Orchestrator");

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}
