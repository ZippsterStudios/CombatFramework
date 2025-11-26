using Unity.Entities;

namespace Framework.Spells.Pipeline.Config
{
    public struct CastGlobalConfig
    {
        public float InterruptChargePercent;
        public float FizzleChargePercent;
        public byte AllowPartialRefundOnPreResolve;
    }

    public struct CastGlobalConfigSingleton : IComponentData
    {
        public BlobAssetReference<CastGlobalConfig> Reference;

        public bool IsCreated => Reference.IsCreated;

        public static CastGlobalConfig DefaultValues => new()
        {
            InterruptChargePercent = 0.5f,
            FizzleChargePercent = 0.1f,
            AllowPartialRefundOnPreResolve = 1
        };
    }
}
