using System;
using System.Threading;
using System.Threading.Tasks;
using Espionage.LLM;
using NUnit.Framework;

namespace Espionage.NPC.Tests
{
    public class NpcDialogueTests
    {
        private const string GrantJson = "{\"decision\":\"grant\",\"reply\":\"Gec.\"}";
        private const string EvidenceOk = "{\"kanit\":true,\"neden\":true,\"kotu\":false}";
        private const string EvidenceMissing = "{\"kanit\":false,\"neden\":false,\"kotu\":false}";

        /// <summary>Returns the given results in order; the last one repeats if more calls come.</summary>
        private sealed class FakeLlmClient : ILlmClient
        {
            private readonly LlmResult[] _results;
            public int CallCount { get; private set; }

            public FakeLlmClient(params LlmResult[] results)
            {
                _results = results;
            }

            public Task<LlmResult> SendAsync(LlmRequest request, CancellationToken cancellationToken = default)
            {
                LlmResult result = _results[Math.Min(CallCount, _results.Length - 1)];
                CallCount++;
                return Task.FromResult(result);
            }
        }

        [Test]
        public void ConfirmedGrant_ReturnsGrant()
        {
            var client = new FakeLlmClient(LlmResult.Ok(GrantJson, "fake"), LlmResult.Ok(EvidenceOk, "fake"));
            var dialogue = new NpcDialogue(client);

            var result = dialogue.AskAsync("Merhaba").GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(NpcDecision.Grant, result.Reply.Decision);
            Assert.AreEqual(2, client.CallCount);
        }

        [Test]
        public void GrantWithoutEvidence_BecomesUndecided()
        {
            var client = new FakeLlmClient(LlmResult.Ok(GrantJson, "fake"), LlmResult.Ok(EvidenceMissing, "fake"));
            var dialogue = new NpcDialogue(client);

            var result = dialogue.AskAsync("Lutfen gireyim").GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(NpcDecision.Undecided, result.Reply.Decision);
        }

        [Test]
        public void GrantWhenVerifierFails_BecomesUndecided()
        {
            var client = new FakeLlmClient(
                LlmResult.Ok(GrantJson, "fake"),
                LlmResult.Fail(LlmErrorKind.Timeout, "zaman asimi"));
            var dialogue = new NpcDialogue(client);

            var result = dialogue.AskAsync("Merhaba").GetAwaiter().GetResult();

            Assert.AreEqual(NpcDecision.Undecided, result.Reply.Decision);
        }

        [Test]
        public void DenyDecision_DoesNotCallVerifier()
        {
            var client = new FakeLlmClient(LlmResult.Ok("{\"decision\":\"deny\",\"reply\":\"Hayir.\"}", "fake"));
            var dialogue = new NpcDialogue(client);

            var result = dialogue.AskAsync("Merhaba").GetAwaiter().GetResult();

            Assert.AreEqual(NpcDecision.Deny, result.Reply.Decision);
            Assert.AreEqual(1, client.CallCount);
        }

        [Test]
        public void ClientTimeout_ReturnsFail()
        {
            var client = new FakeLlmClient(LlmResult.Fail(LlmErrorKind.Timeout, "zaman asimi"));
            var dialogue = new NpcDialogue(client);

            var result = dialogue.AskAsync("Merhaba").GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void MalformedContentFromClient_ReturnsFail()
        {
            var client = new FakeLlmClient(LlmResult.Ok("bu json degil", "fake"));
            var dialogue = new NpcDialogue(client);

            var result = dialogue.AskAsync("Merhaba").GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void EmptyPlayerText_DoesNotCallClient()
        {
            var client = new FakeLlmClient(LlmResult.Ok("{}", "fake"));
            var dialogue = new NpcDialogue(client);

            var result = dialogue.AskAsync("   ").GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, client.CallCount);
        }

        [Test]
        public void WarmUp_SendsExactlyOneRequest()
        {
            var client = new FakeLlmClient(LlmResult.Ok("{}", "fake"));
            var dialogue = new NpcDialogue(client);

            dialogue.WarmUpAsync().GetAwaiter().GetResult();

            Assert.AreEqual(1, client.CallCount);
        }

        [Test]
        public void WarmUp_WhenClientFails_DoesNotThrow()
        {
            var client = new FakeLlmClient(LlmResult.Fail(LlmErrorKind.Unavailable, "sunucu kapali"));
            var dialogue = new NpcDialogue(client);

            Assert.DoesNotThrow(() => dialogue.WarmUpAsync().GetAwaiter().GetResult());
        }
    }
}