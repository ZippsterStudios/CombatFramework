using Framework.Spells.Content;
using Framework.Spells.Factory;
using Unity.Collections;

namespace Framework.Pets.Content
{
    public static class PetSampleContent
    {
        private static bool _registered;

        public static void RegisterDefaults()
        {
            if (_registered)
                return;

            _registered = true;

            RegisterPets();
            RegisterSpells();
        }

        private static void RegisterPets()
        {
            PetBuilder.NewPet("wolf")
                .Category("summons", 1)
                .Stats(320, 0, 6f)
                .Flags(PetFlags.None)
                .Limit(1, "solo")
                .Leash(25f, 2.5f)
                .Recipe("pet_follow")
                .Register();

            PetBuilder.NewPet("imp")
                .Category("summons", 1)
                .Stats(120, 80, 5.5f)
                .Flags(PetFlags.None)
                .Limit(3, "imps")
                .Leash(22f, 2f)
                .Duration(30f)
                .Recipe("pet_follow")
                .Register();

            PetBuilder.NewPet("drone_swarm")
                .Category("summons", 2)
                .Stats(60, 0, 7f)
                .Flags(PetFlags.Swarm | PetFlags.LeashTeleport | PetFlags.ReplaceOldestOnLimit)
                .Limit(12, "drones")
                .Leash(18f, 1.5f)
                .Duration(20f)
                .Recipe("pet_patrol")
                .Register();
        }

        private static void RegisterSpells()
        {
            SpellBuilder.NewSpell("summon_wolf")
                .SetTargeting(SpellTargeting.Self)
                .AddEffect(TargetScope.Single(TargetScopeKind.Caster), new EffectPayload
                {
                    Kind = EffectPayloadKind.SummonPet,
                    Summon = new SummonPayload
                    {
                        PetId = (FixedString64Bytes)"wolf",
                        Count = 1,
                        SpawnRadius = 1.5f
                    }
                })
                .Register();

            SpellBuilder.NewSpell("summon_imps")
                .SetTargeting(SpellTargeting.Self)
                .AddEffect(TargetScope.Single(TargetScopeKind.Caster), new EffectPayload
                {
                    Kind = EffectPayloadKind.SummonPet,
                    Summon = new SummonPayload
                    {
                        PetId = (FixedString64Bytes)"imp",
                        Count = 3,
                        SpawnRadius = 2.5f
                    }
                })
                .Register();

            SpellBuilder.NewSpell("deploy_drones")
                .SetTargeting(SpellTargeting.Self)
                .AddEffect(TargetScope.Single(TargetScopeKind.Caster), new EffectPayload
                {
                    Kind = EffectPayloadKind.SummonPet,
                    Summon = new SummonPayload
                    {
                        PetId = (FixedString64Bytes)"drone_swarm",
                        Count = 6,
                        SpawnRadius = 3.5f
                    }
                })
                .Register();
        }
    }
}
