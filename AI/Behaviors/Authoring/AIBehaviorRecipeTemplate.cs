using System;
using System.Collections.Generic;
using Framework.AI.Behaviors.Runtime;
using Framework.AI.Components;
using Unity.Collections;
using Unity.Entities;
using RecipeAction = Framework.AI.Behaviors.Runtime.Action;

namespace Framework.AI.Behaviors.Authoring
{
    public sealed class BehaviorRecipeTemplate
    {
        public readonly List<RuleTemplate> Rules = new List<RuleTemplate>(8);
        public ActionTemplate? DefaultAction;

        public BlobAssetReference<AIBehaviorRecipe> Build(in AIAgentBehaviorConfig config)
        {
            var map = BuildConfigMap(in config);
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AIBehaviorRecipe>();
            var rules = builder.Allocate(ref root.Rules, Rules.Count);
            for (int i = 0; i < Rules.Count; i++)
            {
                var template = Rules[i];
                rules[i] = new Rule
                {
                    Condition = template.Condition.Resolve(map),
                    Action = template.Action.Resolve(map),
                    Priority = Rules.Count - i
                };
            }

            if (DefaultAction.HasValue)
            {
                root.HasDefault = 1;
                root.DefaultAction = DefaultAction.Value.Resolve(map);
            }
            else
            {
                root.HasDefault = 0;
                root.DefaultAction = default;
            }

            var asset = builder.CreateBlobAssetReference<AIBehaviorRecipe>(Allocator.Persistent);
            builder.Dispose();
            return asset;
        }

        private static Dictionary<string, float> BuildConfigMap(in AIAgentBehaviorConfig config)
        {
            return new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                ["AttackRange"] = config.AttackRange,
                ["ChaseRange"] = config.ChaseRange,
                ["MoveSpeed"] = config.MoveSpeed,
                ["FleeMoveSpeed"] = config.FleeMoveSpeed,
                ["FleeRetreatDistance"] = config.FleeRetreatDistance,
                ["FleeHealthThresholdPercent"] = config.FleeHealthThresholdPercent,
                ["DecisionIntervalSeconds"] = config.DecisionIntervalSeconds,
                ["PrimarySpellCooldownSeconds"] = config.PrimarySpellCooldownSeconds
            };
        }
    }

    public struct RuleTemplate
    {
        public readonly ConditionTemplate Condition;
        public readonly ActionTemplate Action;

        public RuleTemplate(ConditionTemplate condition, ActionTemplate action)
        {
            Condition = condition;
            Action = action;
        }
    }

    public struct ConditionTemplate
    {
        public readonly byte Flags;
        public readonly byte NotInRange;
        public readonly float RangeValue;
        public readonly string RangeKey;
        public readonly float HealthValue;
        public readonly string HealthKey;

        public ConditionTemplate(byte flags, byte notInRange, float rangeValue, string rangeKey, float healthValue, string healthKey)
        {
            Flags = flags;
            NotInRange = notInRange;
            RangeValue = rangeValue;
            RangeKey = rangeKey;
            HealthValue = healthValue;
            HealthKey = healthKey;
        }

        public Condition Resolve(Dictionary<string, float> configMap)
        {
            return new Condition
            {
                Flags = Flags,
                NotInRange = NotInRange,
                AttackRange = RangeKey != null && configMap.TryGetValue(RangeKey, out var range) ? range : RangeValue,
                HealthBelow = HealthKey != null && configMap.TryGetValue(HealthKey, out var hp) ? hp : HealthValue
            };
        }
    }

    public struct ActionTemplate
    {
        public readonly AIBehaviorActionKind Kind;
        public readonly float SpeedValue;
        public readonly string SpeedKey;
        public readonly float RetreatValue;
        public readonly string RetreatKey;
        public readonly FixedString64Bytes SpellId;

        public ActionTemplate(AIBehaviorActionKind kind, float speedValue, string speedKey, float retreatValue, string retreatKey, FixedString64Bytes spellId)
        {
            Kind = kind;
            SpeedValue = speedValue;
            SpeedKey = speedKey;
            RetreatValue = retreatValue;
            RetreatKey = retreatKey;
            SpellId = spellId;
        }

        public RecipeAction Resolve(Dictionary<string, float> configMap)
        {
            return new RecipeAction
            {
                Kind = (byte)Kind,
                MoveSpeed = SpeedKey != null && configMap.TryGetValue(SpeedKey, out var speed) ? speed : SpeedValue,
                RetreatDistance = RetreatKey != null && configMap.TryGetValue(RetreatKey, out var retreat) ? retreat : RetreatValue,
                SpellId = SpellId
            };
        }
    }
}
