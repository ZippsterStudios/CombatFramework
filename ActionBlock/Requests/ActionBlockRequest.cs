using Framework.ActionBlock.Components;
using Unity.Entities;

namespace Framework.ActionBlock.Requests
{
    public struct ActionBlockRequest : IBufferElementData
    {
        public Entity Target;
        public ActionKind Kind;
        public bool Add;
    }
}

