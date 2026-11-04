namespace Espionage.LLM
{
    public enum LlmErrorKind
    {
        None = 0,
        InvalidConfiguration = 1,
        InvalidRequest = 2,
        /// <summary>Server unreachable or connection failed (e.g. Ollama not running).</summary>
        Unavailable = 3,
        Timeout = 4,
        ModelNotFound = 5,
        /// <summary>Non-success HTTP status or an error reported by the server.</summary>
        HttpError = 6,
        EmptyResponse = 7,
        MalformedResponse = 8,
        Cancelled = 9
    }
}
