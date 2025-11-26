using Framework.Pets.Components;
using Framework.Pets.Content;
using Framework.Pets.Drivers;
using Unity.Entities;

namespace Framework.Pets.Policies
{
    public static class PetLimitPolicy
    {
        public static bool TryAcquire(ref EntityManager em, in Entity owner, in PetDefinition def, out Entity toReplace)
        {
            toReplace = Entity.Null;
            int max = def.MaxCountPerOwner;
            if (max < 0)
                return true;

            var index = PetQuery.EnsureIndex(ref em, owner);
            int count = 0;
            int oldestSeq = int.MaxValue;
            Entity oldestPet = Entity.Null;

            for (int i = 0; i < index.Length; i++)
            {
                var entry = index[i];
                if (!entry.PetId.Equals(def.Id))
                    continue;

                count++;
                if (entry.Sequence < oldestSeq)
                {
                    oldestSeq = entry.Sequence;
                    oldestPet = entry.Pet;
                }
            }

            if (count < max)
                return true;

            if ((def.Flags & PetFlags.ReplaceOldestOnLimit) != 0 && oldestPet != Entity.Null)
            {
                toReplace = oldestPet;
                return true;
            }

            toReplace = Entity.Null;
            return false;
        }
    }
}
