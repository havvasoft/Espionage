using UnityEngine;

namespace Espionage.NPC
{
    public sealed class NpcParseResult
    {
        public bool Success => Reply != null;
        public NpcReply Reply { get; }
        public string Error { get; }

        private NpcParseResult(NpcReply reply, string error)
        {
            Reply = reply;
            Error = error ?? string.Empty;
        }

        public static NpcParseResult Ok(NpcReply reply)
            => new NpcParseResult(reply,null);

        public static NpcParseResult Fail(string error)
            => new NpcParseResult(null, error);
    }
}
