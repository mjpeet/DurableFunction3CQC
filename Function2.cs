using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;

namespace DurableFunction3CQC
{
    public static class Function2
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [Function("Function2_OrchestratorFunction")]
        public static async Task<List<Location>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var locations = await context.CallActivityAsync<List<Location>>("Function2_GetLocationsActivity", null);
            return locations;
        }

        [Function("Function2_GetLocationsActivity")]
        public static async Task<List<Location>> GetLocationsActivity([ActivityTrigger] string name, FunctionContext executionContext)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.service.cqc.org.uk/public/v1/locations?perPage=5");
            request.Headers.Add("Ocp-Apim-Subscription-Key", "7394d5e20ca249ec976a8dca142fe041");

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

            return apiResponse.Locations;
        }

        [Function("Function2_ClientFunction")]
        public static async Task<HttpResponseData> ClientFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
			FunctionContext executionContext,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(Function2));

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}
