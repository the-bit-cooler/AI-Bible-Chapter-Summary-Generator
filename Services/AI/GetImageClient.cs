using OpenAI.Images;

namespace ScripturAI.Services;

public partial class AiService
{
  private static readonly ImageClient imageClient = new(model: Environment.GetEnvironmentVariable("OPEN_AI_IMAGE_GENERATOR_NAME"), apiKey: Environment.GetEnvironmentVariable("OPEN_AI_KEY"));

  internal static ImageClient GetImageClient()
  {
    return imageClient;
  }
}
