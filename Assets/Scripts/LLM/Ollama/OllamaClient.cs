using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Espionage.LLM
{
    /// <summary>
    /// <see cref="ILlmClient"/> backed by a locally running Ollama server (/api/chat, non-streaming).
    /// Call from the Unity main thread; the await does not block the game loop.
    /// </summary>
    public sealed class OllamaClient : ILlmClient
    {
        private readonly OllamaConfig _config;
        private readonly IHttpTransport _transport;
        private readonly string _configError;

        public OllamaClient(OllamaConfig config) : this(config, new UnityWebRequestTransport())
        {
        }

        internal OllamaClient(OllamaConfig config, IHttpTransport transport)
        {
            _config = config ?? OllamaConfig.Default;
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _config.TryValidate(out _configError);
        }

        public async Task<LlmResult> SendAsync(LlmRequest request, CancellationToken cancellationToken = default)
        {
            if (_configError != null)
                return LlmResult.Fail(LlmErrorKind.InvalidConfiguration, _configError);

            if (!TryBuildBody(request, out string json, out string requestError))
                return LlmResult.Fail(LlmErrorKind.InvalidRequest, requestError);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var http = await _transport.PostJsonAsync(_config.ChatUrl, json, _config.TimeoutSeconds, cancellationToken);
                float elapsed = (float)stopwatch.Elapsed.TotalSeconds;

                switch (http.Outcome)
                {
                    case HttpOutcome.Completed:
                        return OllamaResponseParser.Parse(http.StatusCode, http.Body, _config.Model, elapsed);
                    case HttpOutcome.Timeout:
                        return LlmResult.Fail(LlmErrorKind.Timeout, $"No response from Ollama within {_config.TimeoutSeconds}s.", elapsed);
                    case HttpOutcome.Cancelled:
                        return LlmResult.Fail(LlmErrorKind.Cancelled, "Request was cancelled.", elapsed);
                    default:
                        return LlmResult.Fail(LlmErrorKind.Unavailable,
                            $"Cannot reach Ollama at {_config.BaseUrl}: {http.Error}. Is 'ollama serve' running?", elapsed);
                }
            }
            catch (Exception e)
            {
                // The contract is "never throw": surface anything unexpected as a failed result.
                return LlmResult.Fail(LlmErrorKind.Unavailable, $"Unexpected LLM client failure: {e.Message}",
                    (float)stopwatch.Elapsed.TotalSeconds);
            }
        }

        private bool TryBuildBody(LlmRequest request, out string json, out string error)
        {
            json = null;
            if (request == null || request.Messages == null || request.Messages.Count == 0)
            {
                error = "Request has no messages.";
                return false;
            }

            OllamaChatRequest dto = request.ExpectJson ? new OllamaJsonChatRequest() : new OllamaChatRequest();
            dto.model = _config.Model;
            dto.stream = false;
            dto.options.temperature = _config.Temperature;
            dto.options.num_predict = _config.MaxTokens;

            bool hasContent = false;
            foreach (var message in request.Messages)
            {
                if (string.IsNullOrWhiteSpace(message.Content))
                    continue;
                hasContent = true;
                dto.messages.Add(new OllamaMessage { role = RoleName(message.Role), content = message.Content });
            }

            if (!hasContent)
            {
                error = "All request messages are empty.";
                return false;
            }

            // Pass as object so JsonUtility serializes the runtime type (includes "format" when present).
            json = JsonUtility.ToJson((object)dto);
            error = null;
            return true;
        }

        private static string RoleName(LlmRole role)
        {
            switch (role)
            {
                case LlmRole.System: return "system";
                case LlmRole.Assistant: return "assistant";
                default: return "user";
            }
        }
    }
}
