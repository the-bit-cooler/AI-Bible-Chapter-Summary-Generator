using System.Net.Http.Json;
using ScripturAI.Models;

namespace ScripturAI.Services;

public partial class GitHubService
{
  public static async Task<List<GitHubFileRef>> FetchGitHubFileListAsync(string repo, List<string> includeExtensions, List<string>? excludeFiles = null)
  {
    List<GitHubFileRef> fileList = [];
    try
    {
      using HttpClient httpClient = new();

      // GitHub API requires User-Agent
      httpClient.DefaultRequestHeaders.Add("User-Agent", "ScripturAI");

      var response = await httpClient.GetFromJsonAsync<List<GitHubFileRef>>($"https://api.github.com/repos/{repo}/contents");
      var data = response?.ToList();

      if (data != null)
      {
        if (includeExtensions.Count > 0)
        {
          data = [.. data.Where(f => includeExtensions.Any(ext => f.name?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) == true))];
        }
        if (excludeFiles != null && excludeFiles.Count > 0)
        {
          data = [.. data.Where(f => !excludeFiles.Contains(f.name ?? string.Empty, StringComparer.OrdinalIgnoreCase))];
        }

        fileList = data;
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"{nameof(GitHubService)}.{FetchGitHubFileListAsync}: Error fetching file refs from GitHub: {ex.Message}");
    }

    return fileList;
  }
}
