using Framework.Spells.Content;
using Unity.Entities;

namespace Framework.Spells.Runtime
{
    internal static class StatOperationApplier
    {
        public static void Apply(ref EntityManager em, in Entity target, in StatOperationBlob op)
        {
            if (!em.Exists(target) || !em.HasComponent<Framework.Stats.Components.StatValue>(target))
                return;

            var stat = em.GetComponentData<Framework.Stats.Components.StatValue>(target);
            switch (op.Operation)
            {
                case StatOperationKind.Add:
                    stat.Additive += op.Value;
                    break;
                case StatOperationKind.Multiply:
                    stat.Multiplier += op.Value;
                    break;
                case StatOperationKind.Set:
                    stat.Value = op.Value;
                    break;
            }
            em.SetComponentData(target, stat);
        }
    }
}
