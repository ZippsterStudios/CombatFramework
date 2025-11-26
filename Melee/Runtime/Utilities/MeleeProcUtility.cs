using Framework.Melee.Blobs;
using Framework.Melee.Components;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Melee.Runtime.Utilities
{
    public static class MeleeProcUtility
    {
        public static void AddEquipmentProc(ref EntityManager em,
                                            Entity entity,
                                            in FixedString64Bytes buffId,
                                            BlobAssetReference<MeleeProcTableBlob> procTable,
                                            in FixedString32Bytes sourceItemId = default,
                                            byte stackCount = 1,
                                            bool markAsProcCarrier = true)
        {
            var buffer = em.HasBuffer<EquipmentBuffElement>(entity)
                ? em.GetBuffer<EquipmentBuffElement>(entity)
                : em.AddBuffer<EquipmentBuffElement>(entity);

            buffer.Add(new EquipmentBuffElement
            {
                BuffId = buffId,
                ProcTable = procTable,
                SourceItemId = sourceItemId,
                StackCount = stackCount,
                IsProcCarrier = (byte)(markAsProcCarrier ? 1 : 0)
            });
        }

        public static void RemoveEquipmentProc(ref EntityManager em, Entity entity, in FixedString64Bytes buffId)
        {
            if (!em.HasBuffer<EquipmentBuffElement>(entity))
                return;

            var buffer = em.GetBuffer<EquipmentBuffElement>(entity);
            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                if (buffer[i].BuffId.Equals(buffId))
                    buffer.RemoveAtSwapBack(i);
            }
        }

        public static void AddProcAugment(ref EntityManager em,
                                          Entity entity,
                                          in FixedString64Bytes sourceBuffId,
                                          BlobAssetReference<MeleeProcTableBlob> procTable,
                                          double expireTime = 0d,
                                          byte stackIndex = 0)
        {
            var buffer = em.HasBuffer<ProcAugmentElement>(entity)
                ? em.GetBuffer<ProcAugmentElement>(entity)
                : em.AddBuffer<ProcAugmentElement>(entity);

            buffer.Add(new ProcAugmentElement
            {
                SourceBuffId = sourceBuffId,
                ProcTable = procTable,
                ExpireTime = expireTime,
                StackIndex = stackIndex
            });
        }

        public static void RemoveProcAugment(ref EntityManager em, Entity entity, in FixedString64Bytes sourceBuffId)
        {
            if (!em.HasBuffer<ProcAugmentElement>(entity))
                return;

            var buffer = em.GetBuffer<ProcAugmentElement>(entity);
            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                if (buffer[i].SourceBuffId.Equals(sourceBuffId))
                    buffer.RemoveAtSwapBack(i);
            }
        }
    }
}
