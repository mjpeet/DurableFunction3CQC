# DurableFunction3CQC

An Azure Durable Functions (.NET 8, isolated worker) app that pulls care provider
location data from the [CQC (Care Quality Commission) public API](https://api.service.cqc.org.uk/public/v1)
and runs basic analysis over it.

## What it does

- Calls the CQC `/locations` endpoint, paging through results.
- Reads the API key name/value from **Azure Key Vault** (via `DefaultAzureCredential`)
  rather than storing secrets in config.
- Aggregates locations by postcode prefix and returns a summary (total count +
  per-prefix counts).

## Functions

| Function | Trigger | Purpose |
|---|---|---|
| `Function1_HttpStart` / `Function1_Orchestrator` | HTTP (anonymous) | Sample/demo orchestration: says hello a few times, then fetches one page of CQC locations. |
| `Function3_ClientFunction` / `Function3_OrchestratorFunction` | HTTP (function key) | Fetches CQC locations (a couple of pages) and analyzes them, grouping by postcode prefix via `AnalyzeData`. |

`Function2.cs` exists in the project but is excluded from compilation (see the
`.csproj`).

## Configuration

The app needs these environment variables / app settings (see `local.settings.json`
for local development):

- `KeyVaultName` — name of the Azure Key Vault holding the CQC API credentials.
- `SecretKeyName` — name of the Key Vault secret containing the HTTP header name for the CQC API key.
- `SecretValueName` — name of the Key Vault secret containing the CQC API key value.

Locally, authentication to Key Vault uses `DefaultAzureCredential`, so you'll need
to be signed in (e.g. via Azure CLI) with access to the vault.

## Running locally

```bash
func start
```

Requires the [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
and the [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) storage emulator
(or a real Azure Storage connection string) for the Durable Task backend.
