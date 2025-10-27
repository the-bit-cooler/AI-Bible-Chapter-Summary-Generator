using System.Text.Json;
using ScripturAI.Models;
using ScripturAI.Services;

string task = args.Length > 0 ? args[0].ToLower() : string.Empty;
switch (task)
{
  case "summarize":
    await SummarizeBibleChapters();
    break;

  default:
    Console.WriteLine("Pass a valid argument.");
    return;
}

static async Task SummarizeBibleChapters()
{
  // Fetch list of book JSON files from GitHub repo aruljohn/Bible-kjv (while excluding Books.json)
  List<GitHubFileRef> githubFileList = await GitHubService.FetchGitHubFileListAsync("aruljohn/Bible-kjv", [".json"], ["Books.json"]);

  if (githubFileList.Count == 0)
  {
    Console.WriteLine("No files found to process.");
    return;
  }
  if (githubFileList.Count != 66)
  {
    Console.WriteLine($"Warning: Expected 66 books in KJV, but found {githubFileList.Count}.");
    return;
  }

  // Load or initialize processed books tracker
  const string processedFilePath = "processed_books.json";
  List<string> processedBooks = LoadProcessedBooks(processedFilePath);
  const string progressFilePath = "chapter_progress.json";
  Dictionary<string, BookProgress> bookProgress = LoadBookProgress(progressFilePath);

  // Process each file one by one
  const int maxRetries = 3;

  foreach (var file in githubFileList)
  {
    if (string.IsNullOrEmpty(file.name))
    {
      Console.WriteLine("Skipping a file with no name.");
      continue;
    }

    string bookName = Path.GetFileNameWithoutExtension(file.name);
    if (processedBooks.Contains(bookName, StringComparer.OrdinalIgnoreCase))
    {
      Console.WriteLine($"Skipping {bookName} as it has already been processed.");
      continue;
    }

    // Load book with retry
    KjvBibleBook? book = null;
    bool loadSuccess = await RetryAsync(async () =>
    {
      book = await GitHubService.LoadKjvBibleBookJsonAsync(file.download_url, file.name);
    }, maxRetries);

    if (!loadSuccess || book == null || book.chapters.Count == 0)
    {
      Console.Error.WriteLine($"Failed to load {bookName} after {maxRetries} retries.");
      return;
    }

    // Process chapters with retry
    bool finishedBook = true;
    int chapterStart = 0;

    if (bookProgress.TryGetValue(bookName, out var progress))
    {
      chapterStart = progress.LastChapterIndexFinished + 1; // resume after last completed chapter
    }

    Console.WriteLine($"Starting to summarize from {bookName} {chapterStart + 1}.");

    for (int i = chapterStart; i < book.chapters.Count; i++)
    {
      KjvBibleBookChapter chapter = book.chapters[i];
      bool summarySucceeded = await RetryAsync(() => AiService.GenerateAiTextSummaryAsync(book.book, chapter), maxRetries);
      if (!summarySucceeded)
      {
        Console.Error.WriteLine($"{nameof(AiService)}.{nameof(AiService.GenerateAiTextSummaryAsync)}: Failed to text summarize {bookName} {chapter.chapter} after {maxRetries} retries.");
        finishedBook = false;
        break; // Stop processing this chapter to avoid partial uploads; manual intervention needed
      }
      summarySucceeded = await RetryAsync(() => AiService.GenerateAiImageSummaryAsync(book.book, chapter), maxRetries);
      if (!summarySucceeded)
      {
        Console.Error.WriteLine($"{nameof(AiService)}.{nameof(AiService.GenerateAiImageSummaryAsync)}: Failed to image summarize {bookName} {chapter.chapter} after {maxRetries} retries.");
        finishedBook = false;
        break; // Stop processing this book to avoid partial uploads; manual intervention needed
      }

      Console.WriteLine($"Completed summary for {bookName} {chapter.chapter}.");

      // ✅ Save progress after each successful batch
      bookProgress[bookName] = new BookProgress { LastChapterIndexFinished = i };
      SaveBookProgress(progressFilePath, bookProgress);
    }

    if (finishedBook)
    {
      processedBooks.Add(bookName);
      SaveProcessedBooks(processedFilePath, processedBooks);

      // ✅ Remove book from progress file (cleanup)
      if (bookProgress.ContainsKey(bookName))
      {
        bookProgress.Remove(bookName);
        SaveBookProgress(progressFilePath, bookProgress);
      }

      Console.WriteLine($"Completed processing {bookName}.");
    }
    else
    {
      Console.Error.WriteLine($"Partial failure in {bookName}; not marking as processed.");
    }

    // ✅ Eject after a book finishes as this process can take an hour or longer per book
    return;
  }
}

static List<string> LoadProcessedBooks(string filePath)
{
  if (File.Exists(filePath))
  {
    try
    {
      string json = File.ReadAllText(filePath);
      return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error loading processed books: {ex.Message}. Starting fresh.");
    }
  }
  return new List<string>();
}

static void SaveProcessedBooks(string filePath, List<string> processedBooks)
{
  try
  {
    string json = JsonSerializer.Serialize(processedBooks);
    File.WriteAllText(filePath, json);
  }
  catch (Exception ex)
  {
    Console.WriteLine($"Error saving processed books: {ex.Message}.");
  }
}

static Dictionary<string, BookProgress> LoadBookProgress(string filePath)
{
  if (File.Exists(filePath))
  {
    try
    {
      string json = File.ReadAllText(filePath);
      return JsonSerializer.Deserialize<Dictionary<string, BookProgress>>(json)
        ?? new Dictionary<string, BookProgress>();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error loading book progress: {ex.Message}. Starting fresh.");
    }
  }
  return new Dictionary<string, BookProgress>();
}

static void SaveBookProgress(string filePath, Dictionary<string, BookProgress> progress)
{
  try
  {
    string json = JsonSerializer.Serialize(progress, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(filePath, json);
  }
  catch (Exception ex)
  {
    Console.WriteLine($"Error saving book progress: {ex.Message}.");
  }
}

static async Task<bool> RetryAsync(Func<Task> action, int maxRetries)
{
  for (int attempt = 1; attempt <= maxRetries; attempt++)
  {
    try
    {
      await action();
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
      if (attempt == maxRetries)
      {
        return false;
      }
      await Task.Delay(1000 * attempt); // Exponential backoff in ms
    }
  }
  return false;
}
