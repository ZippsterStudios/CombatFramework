using System;
using System.Globalization;
using Framework.AI.Behaviors.Runtime;
using Framework.Contracts.Intents;
using Unity.Collections;

namespace Framework.AI.Behaviors.Authoring
{
    public static class AIBehaviorRecipeTextParser
    {
        public static BehaviorRecipeTemplate Parse(string text)
        {
            var template = new BehaviorRecipeTemplate();
            if (string.IsNullOrWhiteSpace(text))
                return template;

            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = StripComment(rawLine);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("default", StringComparison.OrdinalIgnoreCase))
                {
                    template.DefaultAction = ParseActionList(GetPayload(line));
                    continue;
                }

                if (!line.StartsWith("when ", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Expected 'when' but found '{line}'.");

                var colon = line.IndexOf(':');
                if (colon < 0)
                    throw new InvalidOperationException("Missing ':' in rule line.");

                var condition = ParseCondition(line.Substring(5, colon - 5).Trim());
                var action = ParseActionList(line[(colon + 1)..].Trim());
                template.Rules.Add(new RuleTemplate(condition, action));
            }

            return template;
        }

        private static string StripComment(string line)
        {
            var hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);
            return line.Trim();
        }

        private static string GetPayload(string line)
        {
            var colon = line.IndexOf(':');
            if (colon < 0)
                throw new InvalidOperationException("Missing ':' in directive.");
            return line[(colon + 1)..].Trim();
        }

        private static ConditionTemplate ParseCondition(string text)
        {
            byte flags = 0;
            byte notInRange = 0;
            float rangeValue = 0f;
            string rangeKey = null;
            float healthValue = 0f;
            string healthKey = null;

            foreach (var tokenRaw in text.Split('&'))
            {
                var token = tokenRaw.Trim();
                if (token.Length == 0)
                    continue;

                if (token.Equals("has_target", StringComparison.OrdinalIgnoreCase))
                {
                    flags |= AIBehaviorConditionFlags.HasTarget;
                    continue;
                }

                if (token.Equals("visible", StringComparison.OrdinalIgnoreCase))
                {
                    flags |= AIBehaviorConditionFlags.TargetVisible;
                    continue;
                }

                if (token.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
                {
                    notInRange = 1;
                    ParseRange(token[4..], ref flags, ref rangeValue, ref rangeKey);
                    continue;
                }

                if (token.StartsWith("in_range", StringComparison.OrdinalIgnoreCase) || token.StartsWith("range", StringComparison.OrdinalIgnoreCase))
                {
                    notInRange = 0;
                    ParseRange(token, ref flags, ref rangeValue, ref rangeKey);
                    continue;
                }

                if (token.StartsWith("health_below", StringComparison.OrdinalIgnoreCase))
                {
                    ParseScalar(token, ref flags, ref healthValue, ref healthKey);
                    continue;
                }

                throw new InvalidOperationException($"Unknown condition token '{token}'.");
            }

            return new ConditionTemplate(flags, notInRange, rangeValue, rangeKey, healthValue, healthKey);
        }

        private static ActionTemplate ParseActionList(string text)
        {
            ActionTemplate action = default;
            bool hasAction = false;

            foreach (var part in text.Split(';'))
            {
                var token = part.Trim();
                if (token.Length == 0)
                    continue;

                if (token.Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    action = new ActionTemplate(AIBehaviorActionKind.Stop, 0f, null, 0f, null, default);
                    hasAction = true;
                    continue;
                }

                if (token.StartsWith("move", StringComparison.OrdinalIgnoreCase))
                {
                    action = ParseMove(token);
                    hasAction = true;
                    continue;
                }

                if (token.StartsWith("cast", StringComparison.OrdinalIgnoreCase))
                {
                    action = ParseCast(token);
                    hasAction = true;
                    continue;
                }

                throw new InvalidOperationException($"Unknown action token '{token}'.");
            }

            if (!hasAction)
                throw new InvalidOperationException("Rule must declare at least one action.");

            return action;
        }

        private static ActionTemplate ParseMove(string token)
        {
            var inner = ExtractArgs(token);
            var args = inner.Split(',');
            if (args.Length == 0)
                throw new InvalidOperationException("move() requires at least a mode.");

            var mode = args[0].Trim();
            var speed = args.Length > 1 ? args[1].Trim() : string.Empty;
            var retreat = args.Length > 2 ? args[2].Trim() : string.Empty;

            var kind = mode.Equals("flee", StringComparison.OrdinalIgnoreCase)
                ? AIBehaviorActionKind.MoveFlee
                : AIBehaviorActionKind.MoveChase;

            var speedValue = ParseValue(speed, out var speedKey);
            var retreatValue = ParseValue(retreat, out var retreatKey);

            return new ActionTemplate(kind, speedValue, speedKey, retreatValue, retreatKey, default);
        }

        private static ActionTemplate ParseCast(string token)
        {
            var inner = ExtractArgs(token).Trim();
            if (inner.Equals("primary", StringComparison.OrdinalIgnoreCase))
                return new ActionTemplate(AIBehaviorActionKind.CastPrimary, 0f, null, 0f, null, default);

            if (inner.StartsWith("id=", StringComparison.OrdinalIgnoreCase))
                inner = inner[3..].Trim();

            inner = inner.Trim('"', ' ', '\'');
            if (inner.Length == 0)
                throw new InvalidOperationException("cast() requires a spell id.");

            var spell = new FixedString64Bytes(inner);
            return new ActionTemplate(AIBehaviorActionKind.CastId, 0f, null, 0f, null, spell);
        }

        private static void ParseRange(string token, ref byte flags, ref float rangeValue, ref string rangeKey)
        {
            flags |= AIBehaviorConditionFlags.UseRange;
            var inner = ExtractArgs(token);
            rangeValue = ParseValue(inner, out rangeKey);
        }

        private static void ParseScalar(string token, ref byte flags, ref float value, ref string key)
        {
            flags |= AIBehaviorConditionFlags.UseHealthBelow;
            value = ParseValue(ExtractArgs(token), out key);
        }

        private static float ParseValue(string token, out string key)
        {
            key = null;
            if (string.IsNullOrWhiteSpace(token))
                return 0f;

            if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return value;

            key = token.Trim();
            return 0f;
        }

        private static string ExtractArgs(string token)
        {
            var l = token.IndexOf('(');
            var r = token.LastIndexOf(')');
            if (l < 0 || r < 0 || r <= l)
                throw new InvalidOperationException($"Malformed expression '{token}'.");
            return token.Substring(l + 1, r - l - 1);
        }
    }
}
