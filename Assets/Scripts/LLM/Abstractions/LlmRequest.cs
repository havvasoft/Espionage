using System;
using System.Collections.Generic;

namespace Espionage.LLM
{
    public sealed class LlmRequest
    {
        public IReadOnlyList<LlmMessage> Messages { get; }

        /// <summary>Asks the provider to constrain output to syntactically valid JSON.</summary>
        public bool ExpectJson { get; }

        public LlmRequest(IReadOnlyList<LlmMessage> messages, bool expectJson = false)
        {
            Messages = messages ?? throw new ArgumentNullException(nameof(messages));
            ExpectJson = expectJson;
        }

        public static LlmRequest FromPrompts(string systemPrompt, string userPrompt, bool expectJson = false)
        {
            var messages = new List<LlmMessage>(2);
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                messages.Add(new LlmMessage(LlmRole.System, systemPrompt));
            messages.Add(new LlmMessage(LlmRole.User, userPrompt));
            return new LlmRequest(messages, expectJson);
        }
    }
}
