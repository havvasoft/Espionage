using NUnit.Framework;

namespace Espionage.NPC.Tests
{
    public class NpcGrantVerifierTests
    {
        [Test]
        public void EvidenceReasonAndNoBadIntent_Confirms()
        {
            Assert.IsTrue(NpcGrantVerifier.Evaluate("{\"kanit\":true,\"neden\":true,\"kotu\":false}"));
        }

        [Test]
        public void MissingEvidence_DoesNotConfirm()
        {
            Assert.IsFalse(NpcGrantVerifier.Evaluate("{\"kanit\":false,\"neden\":true,\"kotu\":false}"));
        }

        [Test]
        public void MissingReason_DoesNotConfirm()
        {
            Assert.IsFalse(NpcGrantVerifier.Evaluate("{\"kanit\":true,\"neden\":false,\"kotu\":false}"));
        }

        [Test]
        public void BadIntent_DoesNotConfirm()
        {
            Assert.IsFalse(NpcGrantVerifier.Evaluate("{\"kanit\":true,\"neden\":true,\"kotu\":true}"));
        }

        [Test]
        public void MissingFields_DoNotConfirm()
        {
            Assert.IsFalse(NpcGrantVerifier.Evaluate("{}"));
            Assert.IsFalse(NpcGrantVerifier.Evaluate("{\"kanit\":true}"));
        }

        [Test]
        public void MalformedOrEmptyText_DoesNotConfirm()
        {
            Assert.IsFalse(NpcGrantVerifier.Evaluate("bu json degil"));
            Assert.IsFalse(NpcGrantVerifier.Evaluate(""));
            Assert.IsFalse(NpcGrantVerifier.Evaluate(null));
        }
    }
}