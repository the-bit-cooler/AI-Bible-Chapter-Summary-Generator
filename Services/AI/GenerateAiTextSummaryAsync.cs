using System.ClientModel;
using System.Text;
using Azure.Storage.Blobs;
using OpenAI.Chat;
using ScripturAI.Models;

namespace ScripturAI.Services;

public partial class AiService
{
  internal static async Task GenerateAiTextSummaryAsync(string? book, KjvBibleBookChapter chapter)
  {
    if (string.IsNullOrEmpty(book)) throw new Exception("Missing book name.");
    if (string.IsNullOrEmpty(chapter.chapter)) throw new Exception($"Missing {book} chapter number.");

    var chapterVerses = new UserChatMessage($@"
      All verses from {book} {chapter.chapter} of the Bible: 
      {string.Join("\n", chapter.verses.Select(v => $"{v.verse}: {v.text}"))}
    ");

    List<ChatMessage> messages =
    [
      new SystemChatMessage($@"
        You are a concise and balanced biblical summarizer. 
        You receive a full chapter of Scripture in the King James Version or similar. 
        Your task is to produce a clear, faithful summary of that chapter in modern English. 

        Guidelines:
        - Keep the summary concise: roughly 3-5 paragraphs (or less than a page).
        - Focus on the main narrative or message — not every verse.
        - Preserve theological accuracy and tone.
        - Avoid adding interpretation, speculation, or moral commentary.
        - Do not include verse numbers or headings.
        - If the passage is poetic or prophetic, describe the imagery and central theme succinctly.
        - Output only the summary text — no preamble or extra formatting or version or book and chapter heading.

        Safety and Tone Requirements:
        - Avoid using words or imagery related to violence, war, death, sexual acts, nudity, or self-harm.
        - Instead of literal descriptions of these things, summarize their purpose or outcome (e.g., say “a battle took place” instead of “people were killed”).
        - Use calm, neutral, and reverent language appropriate for all audiences.
        - Do not include or imply explicit, gory, or disturbing details.
      "),
      new UserChatMessage($@"
        Summarize {book} {chapter.chapter} of the Bible. 
        Here are the chapter verses: 
        {string.Join("\n", chapter.verses.Select(v => $"{v.verse}: {v.text}"))}
      ")
    ];

    ClientResult<ChatCompletion> response = await GetChatClient().CompleteChatAsync(messages);

    var chatCompletion = response.Value;

    if (chatCompletion == null)
    {
      throw new Exception($"No response received.");
    }
    else if (chatCompletion.Content == null || chatCompletion.Content.Count == 0)
    {
      throw new Exception($"Finish reason: {chatCompletion.FinishReason}.");
    }
    else
    {
      chapter.summary = string.Join("\n", chatCompletion.Content
        .Where(c => !string.IsNullOrWhiteSpace(c.Text))
        .Select(c => c.Text));

      if (string.IsNullOrWhiteSpace(chapter.summary))
      {
        throw new Exception($"Empty response received.");
      }

      // 5️⃣ Upload to Azure Storage
      using MemoryStream stream = new(Encoding.UTF8.GetBytes(chapter.summary));

      BlobClient blobClient = await DataService.GetBlobClientAsync(book, chapter.chapter, "txt");

      await blobClient.UploadAsync(stream, overwrite: true);
    }
  }
}
