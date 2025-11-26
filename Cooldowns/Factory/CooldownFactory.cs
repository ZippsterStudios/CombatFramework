using Unity.Collections;
using Unity.Entities;

namespace Framework.Cooldowns.Factory
{
    public static class CooldownFactory
    {
        public static void ApplyCooldown(ref EntityManager em, in Entity e, in FixedString64Bytes groupId, double readyTime)
        {
            if (!em.HasBuffer<Components.CooldownGroup>(e))
                em.AddBuffer<Components.CooldownGroup>(e);
            var buf = em.GetBuffer<Components.CooldownGroup>(e);
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i].GroupId.Equals(groupId))
                {
                    var cg = buf[i];
                    if (readyTime > cg.ReadyTime) { cg.ReadyTime = readyTime; buf[i] = cg; }
                    return;
                }
            }
            buf.Add(new Components.CooldownGroup { GroupId = groupId, ReadyTime = readyTime });
        }

        public static bool IsReady(in DynamicBuffer<Components.CooldownGroup> buffer, in FixedString64Bytes groupId, double now)
        {
            for (int i = 0; i < buffer.Length; i++)
                if (buffer[i].GroupId.Equals(groupId))
                    return now >= buffer[i].ReadyTime;
            return true;
        }
    }
}

