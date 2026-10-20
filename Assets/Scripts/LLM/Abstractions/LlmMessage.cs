namespace Espionage.LLM
{
    public readonly struct LlmMessage
    {
        public LlmRole Role { get; }
        public string Content { get; }

        public LlmMessage(LlmRole role, string content)
        {
            Role = role;
            Content = content;
        }
    }
}
