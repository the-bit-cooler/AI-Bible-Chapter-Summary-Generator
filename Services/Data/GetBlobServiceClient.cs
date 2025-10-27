using Azure.Storage.Blobs;

namespace ScripturAI.Services;

public partial class DataService
{
  static readonly BlobServiceClient storageClient = new(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"));
}
