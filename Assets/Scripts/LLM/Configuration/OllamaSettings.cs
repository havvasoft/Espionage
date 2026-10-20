using UnityEngine;

namespace Espionage.LLM
{
    /// <summary>Designer-editable asset that produces an <see cref="OllamaConfig"/>.</summary>
    [CreateAssetMenu(fileName = "OllamaSettings", menuName = "Espionage/LLM/Ollama Settings")]
    public sealed class OllamaSettings : ScriptableObject
    {
        [SerializeField] private string baseUrl = OllamaConfig.DefaultBaseUrl;
        [SerializeField] private string model = OllamaConfig.DefaultModel;
        [SerializeField, Min(1)] private int timeoutSeconds = OllamaConfig.DefaultTimeoutSeconds;
        [SerializeField, Min(0f)] private float temperature = OllamaConfig.DefaultTemperature;
        [SerializeField, Min(1)] private int maxTokens = OllamaConfig.DefaultMaxTokens;

        public OllamaConfig ToConfig() => new OllamaConfig(baseUrl, model, timeoutSeconds, temperature, maxTokens);

        public ILlmClient CreateClient() => new OllamaClient(ToConfig());
    }
}
