namespace AssistantAi.Models
{
    /// <summary>
    /// One turn of conversation in the shape the chat/completions endpoint expects.
    /// Keeps the API services independent of the SQLite storage types.
    /// </summary>
    public class ChatMessage
    {
        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        /// <summary>"user", "assistant" or "system".</summary>
        public string Role { get; }

        public string Content { get; }
    }
}
