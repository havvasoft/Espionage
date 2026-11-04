using System;

namespace Espionage.LLM
{
    /// <summary>Immutable, validated connection settings for a local Ollama server.</summary>
    public sealed class OllamaConfig
    {
        public const string DefaultBaseUrl = "http://localhost:11434";
        public const string DefaultModel = "llama3.2";
        public const int DefaultTimeoutSeconds = 30;
        public const float DefaultTemperature = 0.7f;
        public const int DefaultMaxTokens = 256;

        public string BaseUrl { get; }
        public string Model { get; }
        public int TimeoutSeconds { get; }
        public float Temperature { get; }
        public int MaxTokens { get; }

        public string ChatUrl => BaseUrl + "/api/chat";

        public OllamaConfig(
            string baseUrl = DefaultBaseUrl,
            string model = DefaultModel,
            int timeoutSeconds = DefaultTimeoutSeconds,
            float temperature = DefaultTemperature,
            int maxTokens = DefaultMaxTokens)
        {
            BaseUrl = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
            Model = (model ?? string.Empty).Trim();
            TimeoutSeconds = timeoutSeconds;
            Temperature = temperature;
            MaxTokens = maxTokens;
        }

        public static OllamaConfig Default => new OllamaConfig();

        public bool TryValidate(out string error)
        {
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                error = $"Ollama base URL '{BaseUrl}' is not a valid http(s) URL.";
                return false;
            }
            if (string.IsNullOrEmpty(Model))
            {
                error = "Ollama model name is empty.";
                return false;
            }
            if (TimeoutSeconds <= 0)
            {
                error = "Timeout must be greater than zero seconds.";
                return false;
            }
            if (Temperature < 0f || float.IsNaN(Temperature))
            {
                error = "Temperature must be zero or greater.";
                return false;
            }
            if (MaxTokens <= 0)
            {
                error = "Max tokens must be greater than zero.";
                return false;
            }
            error = null;
            return true;
        }
    }
}
