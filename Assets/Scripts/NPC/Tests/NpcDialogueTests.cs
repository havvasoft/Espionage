using System.Threading;
using System.Threading.Tasks;
using Espionage.LLM;
using NUnit.Framework;

namespace Espionage.NPC.Tests
{
    public class NpcDialogueTests
    {
        private sealed class FakeLlmClient : ILlmClient
        {
            private readonly LlmResult _result;
            public int CallCount { get; private set; }

            public FakeLlmClient(LlmResult result)
            {
                _result = result;
            }

            public Task<LlmResult> SendAsync(LlmRequest request, CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(_result);
            }
        }

        [Test]
        public void ValidJsonFromClient_ReturnsReply()
        {
            var client = new FakeLlmClient(LlmResult.Ok("{\"decision\":\"grant\",\"reply\":\"Gec.\"}", "fake"));
            var dialogue = new NpcDialogue(client);

            var result = dialogue.AskAsync("Merhaba").GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(NpcDecision.Grant, result.Reply.Decision);
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
    }
}