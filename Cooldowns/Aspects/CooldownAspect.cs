using Unity.Entities;
using Unity.Collections;

namespace Framework.Cooldowns.Aspects
{
    public readonly partial struct CooldownAspect : IAspect
    {
        public readonly Entity Entity;
        readonly DynamicBuffer<Framework.Cooldowns.Components.CooldownGroup> _groups;

        public bool IsReady(in FixedString64Bytes groupId, double now)
        {
            for (int i = 0; i < _groups.Length; i++)
                if (_groups[i].GroupId.Equals(groupId))
                    return now >= _groups[i].ReadyTime;
            return true;
        }

        public void Apply(ref EntityManager em, in FixedString64Bytes groupId, double readyTime)
        {
            var groups = em.GetBuffer<Framework.Cooldowns.Components.CooldownGroup>(Entity);
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].GroupId.Equals(groupId))
                {
                    var cg = groups[i];
                    if (readyTime > cg.ReadyTime)
                    {
                        cg.ReadyTime = readyTime;
                        groups[i] = cg;
                    }
                    return;
                }
            }
            groups.Add(new Framework.Cooldowns.Components.CooldownGroup { GroupId = groupId, ReadyTime = readyTime });
        }
    }
}
