using System.Net.Http.Json;
using ScripturAI.Models;

namespace ScripturAI.Services;

public partial class GitHubService
{
  public static async Task<KjvBibleBook> LoadKjvBibleBookJsonAsync(string? downloadUrl, string? fileName)
  {
    if (string.IsNullOrEmpty(downloadUrl) || string.IsNullOrEmpty(fileName))
    {
      throw new ArgumentException($"{nameof(GitHubService)}.{LoadKjvBibleBookJsonAsync}: Download URL or file name is null or empty.");
    }

    using HttpClient httpClient = new();

    // GitHub API requires User-Agent
    httpClient.DefaultRequestHeaders.Add("User-Agent", "ScripturAI");

    var book = await httpClient.GetFromJsonAsync<KjvBibleBook>(downloadUrl);
    if (book == null || string.IsNullOrEmpty(book.book) || book.chapters == null)
    {
      throw new ArgumentException($"{nameof(GitHubService)}.{LoadKjvBibleBookJsonAsync}: Failed to parse {fileName}.");
    }

    return book;
  }
}
