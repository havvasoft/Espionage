using System;
using System.Threading;
using System.Threading.Tasks;
using Espionage.LLM;

namespace Espionage.NPC
{
    public sealed class NpcDialogue
    {
        private const string NotConfirmedReply = "Önce yetkini ve odaya neden girmek istediğini kanıtlaman gerekir.";

        private readonly ILlmClient _client;
        private readonly NpcGrantVerifier _verifier;

        public NpcDialogue(ILlmClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _verifier = new NpcGrantVerifier(_client);
        }

        /// <summary>Sends a tiny request so the model is loaded before the player talks to the NPC. The result is ignored.</summary>
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

            NpcParseResult parsed = NpcReplyParser.Parse(llmResult.Content);

            if (!parsed.Success || parsed.Reply.Decision != NpcDecision.Grant)
                return parsed;

            // A Grant opens the door, so it needs a second, independent confirmation. Fails closed.
            bool confirmed = await _verifier.ConfirmAsync(playerText, cancellationToken);
            if (confirmed)
                return parsed;

            return NpcParseResult.Ok(new NpcReply(NpcDecision.Undecided, NotConfirmedReply));
        }
    }
}