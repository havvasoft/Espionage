using System;
using System.Threading;
using System.Threading.Tasks;
using Espionage.LLM;

namespace Espionage.NPC
{
    public sealed class NpcDialogue
    {
        private readonly ILlmClient _client;

        public NpcDialogue(ILlmClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task WarmUpAsync(CancellationToken cancellationToken = default)
        {
            await _client.SendAsync(LlmRequest.FromPrompts(null, "Merhaba"), cancellationToken);
        }
        
        public async Task<NpcParseResult> AskAsync(string playerText, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(playerText))
                return NpcParseResult.Fail("Player text is empty.");

            LlmResult llmResult = await _client.SendAsync(NpcPromptBuilder.Build(playerText), cancellationToken);

            if (!llmResult.Success)
                return NpcParseResult.Fail($"LLM call failed ({llmResult.ErrorKind}): {llmResult.ErrorMessage}");

            return NpcReplyParser.Parse(llmResult.Content);
        }
    }
}