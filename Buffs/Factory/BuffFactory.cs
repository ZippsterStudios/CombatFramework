using Unity.Collections;
using Unity.Entities;

namespace Framework.Buffs.Factory
{
    public static class BuffFactory
    {
        // Renamed to avoid clashing with Components.BuffInstance
        public struct PendingBuff : IBufferElementData
        {
            public FixedString64Bytes Id;
            public int Stacks;
            public float TimeRemaining;
        }

        public static void EnsureBuffer(ref EntityManager em, in Entity e)
        {
            if (!em.HasBuffer<PendingBuff>(e)) em.AddBuffer<PendingBuff>(e);
        }

        public static void Apply(ref EntityManager em, in Entity e, in FixedString64Bytes id, float duration, int stacks = 1)
        {
            EnsureBuffer(ref em, e);
            var buf = em.GetBuffer<PendingBuff>(e);
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i].Id.Equals(id))
                {
                    var bi = buf[i];
                    bi.Stacks += stacks;
                    bi.TimeRemaining = duration; // refresh for simplicity
                    buf[i] = bi;
                    return;
                }
            }
            buf.Add(new PendingBuff { Id = id, Stacks = stacks, TimeRemaining = duration });
        }

        public static void Remove(ref EntityManager em, in Entity e, in FixedString64Bytes id)
        {
            if (!em.HasBuffer<PendingBuff>(e)) return;
            var buf = em.GetBuffer<PendingBuff>(e);
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i].Id.Equals(id)) { buf.RemoveAt(i); return; }
            }
        }
    }
}
