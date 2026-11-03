using NUnit.Framework;

namespace Espionage.NPC.Tests
{
    public class NpcReplyParserTests
    {
        [Test]
        public void ValidGrant_ReturnGrantDecision()
        {
            var result = NpcReplyParser.Parse("{\"decision\":\"grant\",\"reply\":\"Gecebilirsin.\"}");

            Assert.IsTrue(result.Success);
            Assert.AreEqual(NpcDecision.Grant, result.Reply.Decision);
            Assert.AreEqual("Gecebilirsin.", result.Reply.Reply);
        }

        [Test]
        public void UnknownDecision_Fails()
        {
            var result = NpcReplyParser.Parse("{\"decision\":\"maybe\",\"reply\":\"Hmm.\"}");

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void MalformedJson_Fails()
        {
            var result = NpcReplyParser.Parse("bu json değil");

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void EmptyText_Fails()
        {
            var result = NpcReplyParser.Parse("");

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void GrantWithEmptyReply_Fails()
        {
            var result = NpcReplyParser.Parse("{\"decision\":\"grant\",\"reply\":\"\"}");

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void MissingDecision_Fails()
        {
            var result = NpcReplyParser.Parse("{\"reply\":\"Merhaba.\"}");

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void DecisionWithCaseAndSpaces_IsNormalized()
        {
            var result = NpcReplyParser.Parse("{\"decision\":\" GRANT \",\"reply\":\"Tamam.\"}");

            Assert.IsTrue(result.Success);
            Assert.AreEqual(NpcDecision.Grant, result.Reply.Decision);
        }


    }
}