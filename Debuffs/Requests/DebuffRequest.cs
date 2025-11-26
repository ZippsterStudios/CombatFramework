using Framework.Debuffs.Content;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Debuffs.Requests
{
    public struct DebuffRequest : IBufferElementData
    {
        public Entity Target;
        public Entity Source;
        public FixedString64Bytes DebuffId;
        public int AddStacks;
        public float DurationOverride;
        public DebuffFlags ExtraFlags;
    }
}
