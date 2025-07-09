using System.Text;
using tryAGI.OpenAI;

namespace ClubDoorman;

internal static class OpenAiExtensions
{
    public static ChatCompletionRequestSystemMessage AsSystemMessage(this string text) =>
        new() { Content = text };

    public static ChatCompletionRequestUserMessage AsUserMessage(this string text) =>
        new() { Content = text };

    public static ChatCompletionRequestUserMessage AsUserMessage(this byte[] imageBytes, string mimeType = "image/jpeg", 
        ChatCompletionRequestMessageContentPartImageImageUrlDetail detail = ChatCompletionRequestMessageContentPartImageImageUrlDetail.Low) =>
        new()
        {
            Content = new List<ChatCompletionRequestUserMessageContentPart>
            {
                new ChatCompletionRequestMessageContentPartImage
                {
                    ImageUrl = new ChatCompletionRequestMessageContentPartImageImageUrl
                    {
                        Url = $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}",
                        Detail = detail
                    }
                }
            }
        };
} 