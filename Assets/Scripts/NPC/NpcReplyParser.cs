using UnityEngine;
using System;

namespace Espionage.NPC
{
    internal static class NpcReplyParser
    {
        public static NpcParseResult Parse(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return NpcParseResult.Fail("Model returned empty text.");

            NpcReplyDto dto;

            try
            {
                dto = JsonUtility.FromJson<NpcReplyDto>(rawText);
            }
            catch (ArgumentException e)
            {
                return NpcParseResult.Fail($"Not valid JSON: {e.Message}");
            }

            if (dto == null)
                return NpcParseResult.Fail("JSON could not be read.");

            if (!TryParseDecision(dto.decision, out NpcDecision decision))
                return NpcParseResult.Fail($"Unknown decision value: '{dto.decision}'.");

            if (string.IsNullOrWhiteSpace(dto.reply))
                return NpcParseResult.Fail("Reply text is empty.");

            return NpcParseResult.Ok(new NpcReply(decision, dto.reply.Trim()));
        }

        private static bool TryParseDecision(string value, out NpcDecision decision)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "grant": decision = NpcDecision.Grant; return true;
                case "deny": decision = NpcDecision.Deny; return true;
                case "undecided": decision = NpcDecision.Undecided; return true;
                default: decision = NpcDecision.Undecided; return false;
            }
        }
    }
}
