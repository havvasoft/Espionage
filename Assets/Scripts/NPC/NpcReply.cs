using UnityEngine;

namespace Espionage.NPC
{
    public sealed class NpcReply
    {
        public NpcDecision Decision { get; }
        public string Reply { get; }

        public NpcReply(NpcDecision decision, string reply)
        {
            Decision = decision;
            Reply = reply;
        }
    }
}
