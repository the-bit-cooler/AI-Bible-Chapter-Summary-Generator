using OpenAI.Chat;

namespace ScripturAI.Services;

public partial class AiService
{
  private static readonly ChatClient chatClient = new(model: Environment.GetEnvironmentVariable("OPEN_AI_CHAT_NAME"), apiKey: Environment.GetEnvironmentVariable("OPEN_AI_KEY"));

  internal static ChatClient GetChatClient()
  {
    return chatClient;
  }
}
