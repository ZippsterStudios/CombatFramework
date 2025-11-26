using Framework.AI.Behaviors.Authoring;
using Framework.AI.Behaviors.Runtime;
using Framework.AI.Components;
using Framework.AI.Policies;
using Framework.AI.Runtime;
using Framework.Contracts.Intents;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace CombatFramework.Tests.AI
{
    public sealed class AIBehaviorTests
    {
        [Test]
        public void DecisionUtility_IdleWithoutCandidates()
        {
            var input = new AIDecisionInput
            {
                CurrentState = AIStateIds.Combat,
                HasTarget = 0,
                TargetVisible = 0,
                HealthPercent = 1f,
                LowHealthThresholdPercent = 0.25f,
                CandidateCount = 0
            };

            Assert.That(AIDecisionUtility.RuleBasedState(in input), Is.EqualTo(AIStateIds.Idle));
        }

        [Test]
        public void DecisionUtility_CombatWithVisibleTarget()
        {
            var input = new AIDecisionInput
            {
                CurrentState = AIStateIds.Idle,
                HasTarget = 1,
                TargetVisible = 1,
                HealthPercent = 0.8f,
                LowHealthThresholdPercent = 0.2f,
                CandidateCount = 2
            };

            Assert.That(AIDecisionUtility.RuleBasedState(in input), Is.EqualTo(AIStateIds.Combat));
        }

        [Test]
        public void DecisionUtility_FleeOnLowHealth()
        {
            var input = new AIDecisionInput
            {
                CurrentState = AIStateIds.Combat,
                HasTarget = 1,
                TargetVisible = 1,
                HealthPercent = 0.05f,
                LowHealthThresholdPercent = 0.25f
            };

            Assert.That(AIDecisionUtility.RuleBasedState(in input), Is.EqualTo(AIStateIds.Flee));
        }

        [Test]
        public void MovementPolicy_ChaseRespectsRange()
        {
            var policy = new DefaultMovementPolicy();
            var intent = new MoveIntent { Mode = (byte)AIMoveMode.Chase, Speed = 5f };
            policy.Resolve(float2.zero, new float2(10f, 0f), 4f, 0d, ref intent);

            Assert.That(math.length(intent.Destination), Is.EqualTo(4f).Within(0.01f));
        }

        [Test]
        public void MovementPolicy_FleeRunsOpposite()
        {
            var policy = new DefaultMovementPolicy();
            var intent = new MoveIntent { Mode = (byte)AIMoveMode.Flee, Speed = 6f };
            var agent = new float2(2f, 0f);
            var target = float2.zero;

            policy.Resolve(agent, target, 5f, 0d, ref intent);
            Assert.That(intent.Destination.x, Is.GreaterThan(agent.x));
            Assert.That(intent.Active, Is.EqualTo((byte)1));
        }

        [Test]
        public void MovementUtility_ClampsToLeash()
        {
            var intent = new MoveIntent { Active = 1, Destination = new float2(20f, 0f) };
            AIMovementUtility.ClampToLeash(1, float2.zero, 5f, 7f, ref intent);
            Assert.That(math.length(intent.Destination), Is.EqualTo(7f).Within(0.01f));
        }

        [Test]
        public void CastUtility_HonorsCooldown()
        {
            var config = AIAgentBehaviorConfig.CreateDefaults();
            var combat = AICombatRuntime.CreateDefault();
            var cast = new CastIntent();
            var spell = config.PrimarySpellId;
            var now = 0d;

            Assert.That(AICastUtility.TryWriteCast(now, spell, new Entity { Index = 1, Version = 1 }, ref combat, in config, ref cast), Is.True);
            Assert.That(cast.Active, Is.EqualTo((byte)1));

            cast.Active = 0;
            Assert.That(AICastUtility.TryWriteCast(now + 0.1d, spell, new Entity { Index = 1, Version = 1 }, ref combat, in config, ref cast), Is.False);
        }

        [Test]
        public void RecipeParser_BuildsArchetype()
        {
            const string text = @"
when has_target & visible & in_range(AttackRange): stop; cast(primary)
when has_target & visible & not in_range(AttackRange): move(chase, MoveSpeed)
default: stop";

            var template = AIBehaviorRecipeTextParser.Parse(text);
            var config = AIAgentBehaviorConfig.CreateDefaults();
            using var recipe = template.Build(in config);

            Assert.That(recipe.IsCreated, Is.True);
            Assert.That(recipe.Value.Rules.Length, Is.GreaterThan(1));
            Assert.That(recipe.Value.Rules[0].Action.Kind, Is.EqualTo((byte)AIBehaviorActionKind.CastPrimary));
        }
    }
}
