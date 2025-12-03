using Framework.Core.Base;
using Unity.Entities;
using Framework.Spells.Pipeline.Systems;
using Framework.Spells.Sustain;

namespace Framework.Spells.Runtime
{
    public struct SpellSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterManagedSystemInGroups<SpellPipelineSystemGroup>(world);
            SystemRegistration.RegisterISystemInGroups<CastPipelineBootstrapSystem>(world);
            SystemRegistration.RegisterISystemInGroups<CastPlanBuilderSystem>(world);
            SystemRegistration.RegisterISystemInGroups<CastPipelineRunnerSystem>(world);
            SystemRegistration.RegisterISystemInGroups<ValidateSpellStageSystem>(world);
            SystemRegistration.RegisterISystemInGroups<AffordSpellStageSystem>(world);
            SystemRegistration.RegisterISystemInGroups<SpendSpellStageSystem>(world);
            SystemRegistration.RegisterISystemInGroups<WindupSpellStageSystem>(world);
            SystemRegistration.RegisterISystemInGroups<InterruptSpellStageSystem>(world);
            SystemRegistration.RegisterISystemInGroups<FizzleSpellStageSystem>(world);
            SystemRegistration.RegisterISystemInGroups<ApplySpellStageSystem>(world);
            SystemRegistration.RegisterISystemInGroups<CleanupSpellStageSystem>(world);
            SystemRegistration.RegisterISystemInGroups<SustainedSpellDrainSystem>(world);
            SystemRegistration.RegisterISystemInGroups<TemporalImprint.Systems.TemporalImprintRecordingSystem>(world);
            SystemRegistration.RegisterISystemInGroups<TemporalImprint.Systems.TemporalImprintReplaySystem>(world);
            SystemRegistration.RegisterISystemInGroups<TemporalImprint.Systems.TemporalEchoDamageSystem>(world);
            SystemRegistration.RegisterISystemInGroups<TemporalImprint.Systems.TemporalImprintSuppressionSystem>(world);
        }
    }
}
