using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Cooldowns.Drivers
{
    [BurstCompile]
    public static class CooldownDriver
    {
        [BurstCompile]
        public static void SetCooldown(ref EntityManager em, in Entity e, in FixedString64Bytes groupId, double readyTime)
        {
            if (!em.HasBuffer<Components.CooldownGroup>(e))
                em.AddBuffer<Components.CooldownGroup>(e);
            var buf = em.GetBuffer<Components.CooldownGroup>(e);
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i].GroupId.Equals(groupId))
                {
                    var v = buf[i];
                    v.ReadyTime = readyTime;
                    buf[i] = v;
                    return;
                }
            }
            buf.Add(new Components.CooldownGroup { GroupId = groupId, ReadyTime = readyTime });
        }

        [BurstCompile]
        public static bool IsOnCooldown(in EntityManager em, in Entity e, in FixedString64Bytes groupId, double now)
        {
            if (!em.HasBuffer<Components.CooldownGroup>(e)) return false;
            var buf = em.GetBuffer<Components.CooldownGroup>(e);
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i].GroupId.Equals(groupId))
                    return now < buf[i].ReadyTime;
            }
            return false;
        }
    }
}

