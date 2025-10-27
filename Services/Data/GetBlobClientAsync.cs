using Azure.Storage.Blobs;

namespace ScripturAI.Services;

public partial class DataService
{
  internal static async Task<BlobClient> GetBlobClientAsync(string book, string chapter, string fileExt)
  {
    return (await GetBlobContainerAsync()).GetBlobClient($"summary/{book}/{chapter}.{fileExt}".Replace(" ", string.Empty));
  }
}
