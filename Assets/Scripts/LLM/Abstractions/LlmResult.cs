namespace Espionage.LLM
{
    /// <summary>
    /// Outcome of an LLM call. <see cref="Content"/> is untrusted text and must be validated by the caller.
    /// </summary>
    public sealed class LlmResult
    {
        public bool Success => ErrorKind == LlmErrorKind.None;
        public string Content { get; }
        public LlmErrorKind ErrorKind { get; }
        public string ErrorMessage { get; }
        public string Model { get; }
        public float ElapsedSeconds { get; }

        private LlmResult(string content, LlmErrorKind errorKind, string errorMessage, string model, float elapsedSeconds)
        {
            Content = content ?? string.Empty;
            ErrorKind = errorKind;
            ErrorMessage = errorMessage ?? string.Empty;
            Model = model ?? string.Empty;
            ElapsedSeconds = elapsedSeconds;
        }

        public static LlmResult Ok(string content, string model, float elapsedSeconds = 0f)
            => new LlmResult(content, LlmErrorKind.None, null, model, elapsedSeconds);

        public static LlmResult Fail(LlmErrorKind kind, string message, float elapsedSeconds = 0f)
            => new LlmResult(null, kind, message, null, elapsedSeconds);

        public override string ToString()
            => Success ? $"Ok({Model}, {ElapsedSeconds:0.00}s)" : $"{ErrorKind}: {ErrorMessage}";
    }
}
