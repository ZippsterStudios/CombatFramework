using Unity.Collections;
using Unity.Entities;

namespace Framework.Pets.Events
{
    public struct PetEventPayload : IBufferElementData
    {
        public Entity Owner;
        public Entity Pet;
        public FixedString64Bytes PetId;
        public FixedString32Bytes GroupId;
        public FixedString64Bytes Reason;
        public float Value;
    }

    public struct PetSummonBeganEvent : IComponentData { }
    public struct PetSummonBlockedEvent : IComponentData { }
    public struct PetSummonResolvedEvent : IComponentData { }
    public struct PetExpiredEvent : IComponentData { }
    public struct PetDismissedEvent : IComponentData { }
    public struct PetDiedEvent : IComponentData { }
    public struct PetHealthLinkedDamageEvent : IComponentData { }
}
