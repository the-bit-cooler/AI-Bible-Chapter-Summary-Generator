namespace ScripturAI.Models;

public class KjvBibleBook
{
  public string? book { get; set; }
  public List<KjvBibleBookChapter> chapters { get; set; } = [];
}

public class KjvBibleBookChapter
{
  public string? chapter { get; set; }
  public string? summary { get; set; }
  public List<KjvBibleBookVerse> verses { get; set; } = [];
}

public class KjvBibleBookVerse
{
  public string? verse { get; set; }
  public string? text { get; set; }
}
