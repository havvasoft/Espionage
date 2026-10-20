using System;
using UnityEngine;

namespace Espionage.LLM
{
    /// <summary>Turns a raw HTTP response from Ollama into an <see cref="LlmResult"/>. Treats the body as untrusted.</summary>
    internal static class OllamaResponseParser
    {
        public static LlmResult Parse(long statusCode, string body, string fallbackModel, float elapsedSeconds)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return statusCode >= 200 && statusCode < 300
                    ? LlmResult.Fail(LlmErrorKind.EmptyResponse, "Ollama returned an empty body.", elapsedSeconds)
                    : LlmResult.Fail(LlmErrorKind.HttpError, $"Ollama returned HTTP {statusCode} with no body.", elapsedSeconds);
            }

            OllamaChatResponse dto;
            try
            {
                dto = JsonUtility.FromJson<OllamaChatResponse>(body);
            }
            catch (Exception e) when (e is ArgumentException || e is FormatException)
            {
                return LlmResult.Fail(LlmErrorKind.MalformedResponse, $"Response is not valid JSON (HTTP {statusCode}): {e.Message}", elapsedSeconds);
            }

            if (dto == null)
                return LlmResult.Fail(LlmErrorKind.MalformedResponse, "Response JSON could not be read.", elapsedSeconds);

            if (!string.IsNullOrEmpty(dto.error))
            {
                bool modelMissing = statusCode == 404
                                    || dto.error.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0;
                return LlmResult.Fail(
                    modelMissing ? LlmErrorKind.ModelNotFound : LlmErrorKind.HttpError,
                    $"Ollama error (HTTP {statusCode}): {dto.error}", elapsedSeconds);
            }

            if (statusCode < 200 || statusCode >= 300)
                return LlmResult.Fail(LlmErrorKind.HttpError, $"Ollama returned HTTP {statusCode}.", elapsedSeconds);

            // Non-streaming replies always have done=true; a missing flag means the payload is not a chat reply.
            if (!dto.done)
                return LlmResult.Fail(LlmErrorKind.MalformedResponse, "Response is missing the expected chat fields.", elapsedSeconds);

            string content = dto.message?.content;
            if (string.IsNullOrWhiteSpace(content))
                return LlmResult.Fail(LlmErrorKind.EmptyResponse, "Model returned empty content.", elapsedSeconds);

            return LlmResult.Ok(content, string.IsNullOrEmpty(dto.model) ? fallbackModel : dto.model, elapsedSeconds);
        }
    }
}
