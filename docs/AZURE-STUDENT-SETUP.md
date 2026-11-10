# Azure with your student account

Use your own student email to activate/sign into [Azure for Students](https://azure.microsoft.com/en-us/free/students). Your university's Azure tenant and subscription permissions determine which resources you can create. Your Azure student identity is separate from the RaceDay Organiser/Participant login.

## Create storage

1. In the Azure portal, confirm the correct directory and student subscription.
2. Create a resource group and a Storage account with Standard performance and LRS redundancy. Select an available nearby region.
3. Keep anonymous blob access disabled. Under Data storage -> Containers, create `raceday-images` with private access.
4. In Access control (IAM), grant your development identity **Storage Blob Data Contributor** at the container or storage-account scope. Storage resource ownership alone does not grant blob data access.
5. For local development, sign in with Azure CLI (`az login`) using that account/subscription, or sign into Visual Studio. The API uses DefaultAzureCredential.
6. Configure only the API terminal:

```powershell
$env:Storage__Provider="Azure"
$env:Storage__ServiceUri="https://YOUR_ACCOUNT.blob.core.windows.net"
$env:Storage__Container="raceday-images"
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project src/RunWay.Api --no-launch-profile --urls http://localhost:5100
```

Do not put your student email password into application settings. RaceDay does not need it.

For hosted Azure apps, enable the API's managed identity and assign it the same Blob Data Contributor role. Configure the service URI and container in API app settings. Role assignment can take time to propagate.

## Alternative: connection string

If your coursework subscription permits storage keys, use an API user secret locally:

```powershell
dotnet user-secrets set "Storage:Provider" "Azure" --project src/RunWay.Api
dotnet user-secrets set "Storage:ConnectionString" "<storage connection string>" --project src/RunWay.Api
```

The placeholder must be replaced locally. Never paste a key into a chat, README, screenshot or Git commit. For Compose, copy `.env.example` to ignored `.env`, set `STORAGE_PROVIDER=Azure` and supply `AZURE_STORAGE_CONNECTION_STRING`. The container does not inherit your host Azure CLI login; use a connection string for the local container demo or a correctly configured workload/managed identity in Azure.

## Verify your Azure integration

With Azure mode selected, upload a new organiser banner and a participant profile photo in the MVC app. Confirm their objects appear in the Azure private container and that the MVC event/profile pages display them after restarting both apps. Record this in your demo. All upload/download calls should travel Web -> API -> Azure. No browser Azure key or public container is needed.

Troubleshooting: 403 usually means the selected identity lacks blob data permission, the wrong tenant/subscription was used, or the storage firewall blocks your network. A missing container produces 404; create it first. Local files are not automatically moved to Azure when you switch providers; upload replacement images.

Sources: [Azure Blob .NET quickstart](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet), [Blob data role assignment](https://learn.microsoft.com/en-us/azure/storage/blobs/assign-azure-role-data-access).
