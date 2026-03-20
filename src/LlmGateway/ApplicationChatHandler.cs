using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class ApplicationChatHandler(ChatClientFactory factory, IToolProvider toolProvider) : IRequestHandler<ApplicationChatQuery, ChatResponse>
{
    public async Task<ChatResponse> Handle(ApplicationChatQuery request, CancellationToken cancellationToken)
    {
        var tools = await toolProvider.GetToolsAsync();
        var chatOptions = new ChatOptions { Tools = tools };

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant for an application architecture skeleton. Use available tools to answer brief questions about employees and retained application capabilities."),
            new(ChatRole.System, "Limit answer to 3 sentences. Be brief."),
            new(ChatRole.System, $"Currently logged in user is {request.CurrentUsername}")
        };

        foreach (var history in request.ChatHistory)
        {
            var role = history.Role == "user" ? ChatRole.User : ChatRole.Assistant;
            chatMessages.Add(new ChatMessage(role, history.Content));
        }

        chatMessages.Add(new ChatMessage(ChatRole.User, request.Prompt));

        IChatClient client = await factory.GetChatClient();
        return await client.GetResponseAsync(chatMessages, chatOptions, cancellationToken);
    }
}
