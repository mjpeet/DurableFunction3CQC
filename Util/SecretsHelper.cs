using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace DurableFunction3CQC.Util
{
    public static class SecretsHelper
    {
        public static async Task<string> GetSecret(string secretName, string keyVaultName, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("GetSecret");
            logger.LogInformation($"*** Function GetSecret parameters secretName: {secretName}, keyVaultName: {keyVaultName}");


            // key vault secrets
            var kvUri = $"https://{keyVaultName}.vault.azure.net";

            var secretClient = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
            var secretKeyAzure = await secretClient.GetSecretAsync(secretName);

            return secretKeyAzure.Value.Value;
        }
    }
}
