using Framework.Contracts.Intents;
using Unity.Mathematics;

namespace Framework.AI.Policies
{
    public interface IMovementPolicy
    {
        void Resolve(in float2 agent, in float2 target, float attackRange, double now, ref MoveIntent intent);
    }

    public struct DefaultMovementPolicy : IMovementPolicy
    {
        public void Resolve(in float2 agent, in float2 target, float attackRange, double now, ref MoveIntent intent)
        {
            var mode = (AIMoveMode)intent.Mode;
            switch (mode)
            {
                case AIMoveMode.Chase:
                    ResolveChase(agent, target, attackRange, ref intent);
                    break;
                case AIMoveMode.Flee:
                    ResolveFlee(agent, target, attackRange, ref intent);
                    break;
                case AIMoveMode.StrafeLeft:
                case AIMoveMode.StrafeRight:
                    ResolveStrafe(agent, target, attackRange, now, ref intent, mode == AIMoveMode.StrafeLeft ? 1f : -1f);
                    break;
                case AIMoveMode.Backstep:
                    ResolveBackstep(agent, target, attackRange, ref intent);
                    break;
                default:
                    intent.Destination = agent;
                    intent.Active = 0;
                    intent.Speed = 0f;
                    break;
            }
        }

        private static void ResolveChase(in float2 agent, in float2 target, float attackRange, ref MoveIntent intent)
        {
            var toTarget = target - agent;
            var range = math.max(0.5f, attackRange);
            var dir = NormalizeSafe(toTarget, new float2(1f, 0f));
            var desired = agent + dir * math.min(range, math.length(toTarget));
            intent.Destination = desired;
            intent.Active = 1;
        }

        private static void ResolveFlee(in float2 agent, in float2 target, float attackRange, ref MoveIntent intent)
        {
            var away = agent - target;
            var dir = NormalizeSafe(away, new float2(1f, 0f));
            var retreat = math.max(attackRange, 1f);
            intent.Destination = agent + dir * retreat;
            intent.Active = 1;
        }

        private static void ResolveBackstep(in float2 agent, in float2 target, float attackRange, ref MoveIntent intent)
        {
            var dir = NormalizeSafe(agent - target, new float2(1f, 0f));
            intent.Destination = agent + dir * math.max(0.5f, attackRange * 0.5f);
            intent.Active = 1;
        }

        private static void ResolveStrafe(in float2 agent, in float2 target, float attackRange, double now, ref MoveIntent intent, float directionSign)
        {
            var toTarget = target - agent;
            var radius = math.max(0.5f, attackRange);
            var forward = NormalizeSafe(toTarget, new float2(0f, 1f));
            var tangent = new float2(-forward.y, forward.x) * directionSign;
            var phase = (float)math.sin(now * 2.0);
            var offset = tangent * radius * 0.5f * (1f + phase);
            intent.Destination = target - forward * radius + offset;
            intent.Active = 1;
        }

        private static float2 NormalizeSafe(in float2 value, in float2 fallback)
        {
            var lenSq = value.x * value.x + value.y * value.y;
            if (lenSq <= 1e-6f)
                return fallback;
            return value / math.sqrt(lenSq);
        }
    }
}
