# Azure student setup

1. Sign in to [Azure for Students](https://azure.microsoft.com/en-us/free/students) with your student email and confirm your university directory/subscription.
2. Create a Storage account (Standard/LRS is sufficient for this coursework) and a private container named raceday-images.
3. Assign your account **Storage Blob Data Contributor** for the storage account/container. Azure resource ownership alone does not provide blob data access.
4. Sign in locally with Azure CLI (az login) or Visual Studio. Configure the API terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:Storage__Provider="Azure"
$env:AzureStorage__ServiceUri="https://YOUR_ACCOUNT.blob.core.windows.net"
$env:AzureStorage__ContainerName="raceday-images"
dotnet run --project RunWay.Api --no-launch-profile --urls http://localhost:5001
```

The API uses DefaultAzureCredential when ServiceUri is supplied. In Azure hosting, enable the API's managed identity and give it the same blob data role.

Alternatively, configure AzureStorage:ConnectionString as an API user secret or API environment setting. For Compose, put AZURE_STORAGE_CONNECTION_STRING in an ignored .env. Do not commit credentials or put them in the MVC project. Your host Azure CLI session is not inherited by Docker containers.

## Verify before submission

Upload a NEW organiser event banner and participant profile photo while Azure mode is active. Check the private container in Azure and reload both MVC pages after restarting the apps. This proves Web -> API -> Azure upload/download. Existing local images do not transfer automatically when switching providers.

A 403 usually means an identity/role/firewall problem. Check the selected tenant and allow role assignments time to propagate. Live Azure verification needs your account and has not been performed by this delivery.

Sources: [Blob .NET quickstart](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet), [Blob data roles](https://learn.microsoft.com/en-us/azure/storage/blobs/assign-azure-role-data-access).
