using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ScripturAI.Services;

public partial class DataService
{
  internal static async Task<BlobContainerClient> GetBlobContainerAsync()
  {
    BlobContainerClient containerClient = storageClient.GetBlobContainerClient(Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_CONTAINER_NAME"));

    await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

    return containerClient;
  }
}
