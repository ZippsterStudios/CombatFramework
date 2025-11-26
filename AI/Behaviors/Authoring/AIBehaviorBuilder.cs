using System;
using System.Collections.Generic;
using Framework.AI.Behaviors.Runtime;
using Framework.AI.Components;
using Unity.Collections;
using Unity.Entities;
using RecipeAction = Framework.AI.Behaviors.Runtime.Action;

namespace Framework.AI.Behaviors.Authoring
{
    public sealed class AIBehaviorBuilder
    {
        private readonly List<RuleDescriptor> _rules = new List<RuleDescriptor>(4);
        private ActionDescriptor? _defaultAction;

        public static AIBehaviorBuilder New(string name) => new AIBehaviorBuilder();

        public AIBehaviorBuilder Rule(string name, Action<ConditionBuilder> condition, Action<ActionBuilder> action)
        {
            var cond = new ConditionBuilder();
            condition?.Invoke(cond);
            var act = new ActionBuilder();
            action?.Invoke(act);
            _rules.Add(new RuleDescriptor(cond.Build(), act.Build()));
            return this;
        }

        public AIBehaviorBuilder Default(Action<ActionBuilder> action)
        {
            var builder = new ActionBuilder();
            action?.Invoke(builder);
            _defaultAction = builder.Build();
            return this;
        }

        public void BuildBlob(in AIAgentBehaviorConfig config, out BlobAssetReference<AIBehaviorRecipe> recipe)
        {
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var root = ref blobBuilder.ConstructRoot<AIBehaviorRecipe>();
            var rules = blobBuilder.Allocate(ref root.Rules, _rules.Count);
            for (int i = 0; i < _rules.Count; i++)
            {
                var descriptor = _rules[i];
                rules[i] = new Rule
                {
                    Condition = descriptor.Condition.Resolve(in config),
                    Action = descriptor.Action.Resolve(in config),
                    Priority = _rules.Count - i
                };
            }

            if (_defaultAction.HasValue)
            {
                root.HasDefault = 1;
                root.DefaultAction = _defaultAction.Value.Resolve(in config);
            }
            else
            {
                root.HasDefault = 0;
                root.DefaultAction = default;
            }

            recipe = blobBuilder.CreateBlobAssetReference<AIBehaviorRecipe>(Allocator.Persistent);
            blobBuilder.Dispose();
        }

        internal struct RuleDescriptor
        {
            public readonly ConditionDescriptor Condition;
            public readonly ActionDescriptor Action;

            public RuleDescriptor(ConditionDescriptor condition, ActionDescriptor action)
            {
                Condition = condition;
                Action = action;
            }
        }

        public sealed class ConditionBuilder
        {
            private byte _flags;
            private byte _notInRange;
            private Func<AIAgentBehaviorConfig, float> _rangeResolver;
            private Func<AIAgentBehaviorConfig, float> _healthResolver;

            public ConditionBuilder HasTarget() { _flags |= AIBehaviorConditionFlags.HasTarget; return this; }
            public ConditionBuilder Visible() { _flags |= AIBehaviorConditionFlags.TargetVisible; return this; }
            public ConditionBuilder InRange(Func<AIAgentBehaviorConfig, float> resolver) { _flags |= AIBehaviorConditionFlags.UseRange; _notInRange = 0; _rangeResolver = resolver; return this; }
            public ConditionBuilder NotInRange(Func<AIAgentBehaviorConfig, float> resolver) { _flags |= AIBehaviorConditionFlags.UseRange; _notInRange = 1; _rangeResolver = resolver; return this; }
            public ConditionBuilder HealthBelow(Func<AIAgentBehaviorConfig, float> resolver) { _flags |= AIBehaviorConditionFlags.UseHealthBelow; _healthResolver = resolver; return this; }
            internal ConditionDescriptor Build() => new ConditionDescriptor { Flags = _flags, NotInRange = _notInRange, RangeResolver = _rangeResolver, HealthResolver = _healthResolver };
        }

        public sealed class ActionBuilder
        {
            private AIBehaviorActionKind _kind = AIBehaviorActionKind.None;
            private Func<AIAgentBehaviorConfig, float> _speedResolver;
            private Func<AIAgentBehaviorConfig, float> _retreatResolver;
            private FixedString64Bytes _spellId;

            public ActionBuilder MoveChase(Func<AIAgentBehaviorConfig, float> speedResolver) { _kind = AIBehaviorActionKind.MoveChase; _speedResolver = speedResolver; return this; }
            public ActionBuilder MoveFlee(Func<AIAgentBehaviorConfig, float> speedResolver, Func<AIAgentBehaviorConfig, float> retreatResolver) { _kind = AIBehaviorActionKind.MoveFlee; _speedResolver = speedResolver; _retreatResolver = retreatResolver; return this; }
            public ActionBuilder Stop() { _kind = AIBehaviorActionKind.Stop; return this; }
            public ActionBuilder CastPrimary() { _kind = AIBehaviorActionKind.CastPrimary; return this; }
            public ActionBuilder CastId(string spellId) { _kind = AIBehaviorActionKind.CastId; _spellId = spellId; return this; }
            internal ActionDescriptor Build() => new ActionDescriptor { Kind = _kind, SpeedResolver = _speedResolver, RetreatResolver = _retreatResolver, SpellId = _spellId };
        }

        internal struct ConditionDescriptor
        {
            public byte Flags;
            public byte NotInRange;
            public Func<AIAgentBehaviorConfig, float> RangeResolver;
            public Func<AIAgentBehaviorConfig, float> HealthResolver;

            public Condition Resolve(in AIAgentBehaviorConfig config)
            {
                return new Condition
                {
                    Flags = Flags,
                    NotInRange = NotInRange,
                    AttackRange = RangeResolver != null ? RangeResolver(config) : 0f,
                    HealthBelow = HealthResolver != null ? HealthResolver(config) : 0f
                };
            }
        }

        internal struct ActionDescriptor
        {
            public AIBehaviorActionKind Kind;
            public Func<AIAgentBehaviorConfig, float> SpeedResolver;
            public Func<AIAgentBehaviorConfig, float> RetreatResolver;
            public FixedString64Bytes SpellId;

            public RecipeAction Resolve(in AIAgentBehaviorConfig config)
            {
                return new RecipeAction
                {
                    Kind = (byte)Kind,
                    MoveSpeed = SpeedResolver != null ? SpeedResolver(config) : 0f,
                    RetreatDistance = RetreatResolver != null ? RetreatResolver(config) : 0f,
                    SpellId = SpellId
                };
            }
        }
    }
}
