using Azure.Storage.Blobs;
using OpenAI.Images;
using ScripturAI.Models;

namespace ScripturAI.Services;

public partial class AiService
{
  internal static async Task GenerateAiImageSummaryAsync(string? book, KjvBibleBookChapter chapter)
  {
    if (string.IsNullOrEmpty(book)) throw new Exception("Missing book name.");
    if (string.IsNullOrEmpty(chapter.chapter)) throw new Exception($"Missing {book} chapter number.");
    if (string.IsNullOrEmpty(chapter.summary)) throw new Exception($"Missing {book} {chapter.chapter} summary.");

    string prompt = $@"
      Create a detailed, reverent, classical-style image representing the main themes of {book} {chapter} from the Bible.
      Avoid modern elements or text. 
      Use the following summary to guide your composition: 
      {chapter.summary}
    ";

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    GeneratedImage image = await GetImageClient().GenerateImageAsync(
      prompt,
      new ImageGenerationOptions
      {
        Size = GeneratedImageSize.W1536xH1024
      }
    );
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    // 5️⃣ Upload to Azure Storage
    using var stream = new MemoryStream();

    image.ImageBytes.ToStream().CopyTo(stream);
    stream.Position = 0;

    BlobClient blobClient = await DataService.GetBlobClientAsync(book, chapter.chapter, "png");

    await blobClient.UploadAsync(stream, overwrite: true);
  }
}
