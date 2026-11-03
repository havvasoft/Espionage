using System;
using System.Collections.Generic;

// Wire-format DTOs for Ollama's /api/chat. Field names must match the JSON exactly (JsonUtility).
// ReSharper disable InconsistentNaming
namespace Espionage.LLM
{
    [Serializable]
    internal class OllamaChatRequest
    {
        public string model;
        public List<OllamaMessage> messages = new List<OllamaMessage>();
        public bool stream;
        public OllamaOptions options = new OllamaOptions();
        public string keep_alive = "30m";
    }

    /// <summary>Adds the "format" field. Kept separate because JsonUtility cannot omit null/empty fields.</summary>
    [Serializable]
    internal sealed class OllamaJsonChatRequest : OllamaChatRequest
    {
        public string format = "json";
    }

    [Serializable]
    internal sealed class OllamaOptions
    {
        public float temperature;
        public int num_predict;
    }

    [Serializable]
    internal sealed class OllamaMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    internal sealed class OllamaChatResponse
    {
        public string model;
        public OllamaMessage message = new OllamaMessage();
        public bool done;
        public string done_reason;
        public string error;
    }
}
