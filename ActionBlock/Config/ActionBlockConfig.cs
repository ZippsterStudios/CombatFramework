using Unity.Entities;

namespace Framework.ActionBlock.Config
{
    public struct ActionBlockConfig : IComponentData
    {
        public bool BlocksRespectDead;
        public bool BlocksRespectCrowdControl;
        public bool BlocksRespectCustomRules;

        public static ActionBlockConfig Default => new ActionBlockConfig
        {
            BlocksRespectDead = true,
            BlocksRespectCrowdControl = true,
            BlocksRespectCustomRules = true
        };
    }
}

