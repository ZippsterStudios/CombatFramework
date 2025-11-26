using Framework.ActionBlock.Config;
using Unity.Entities;
using Unity.Burst;

namespace Framework.ActionBlock.Policies
{
    internal static class ActionBlockConfigAccess
    {
        private struct ConfigTag { }
        private struct HasConfigTag { }

        private static readonly SharedStatic<ActionBlockConfig> _config = SharedStatic<ActionBlockConfig>.GetOrCreate<ConfigTag>();
        private static readonly SharedStatic<byte> _hasConfig = SharedStatic<byte>.GetOrCreate<HasConfigTag>();

        static ActionBlockConfigAccess()
        {
            _config.Data = ActionBlockConfig.Default;
            _hasConfig.Data = 0;
        }

        public static void UpdateConfig(in ActionBlockConfig config)
        {
            _config.Data = config;
            _hasConfig.Data = 1;
        }

        public static void Reset()
        {
            _config.Data = ActionBlockConfig.Default;
            _hasConfig.Data = 0;
        }

        public static ActionBlockConfig Get()
        {
            return _hasConfig.Data == 1 ? _config.Data : ActionBlockConfig.Default;
        }
    }
}

