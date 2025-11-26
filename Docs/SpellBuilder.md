Spell Builder DSL

Example

Spells.NewSpell("Meteor")
  .SetManaCost(120)
  .SetCastTime(3.5f)
  .UseDOT("BurningGround")
  .UseDirectDamage(150)
  .UseAOEForAll(10f, "Enemy")
  .Register();

Key APIs

- Setters: SetManaCost, SetCooldown, SetCastTime, SetRange, SetTargeting, SetSchool
- Effects: UseDOT, UseHOT, UseBuff, UseDebuff, UseArea, UseDirectDamage, UseDirectHeal
- Modifiers: UseAOE, UseAOEForAll, UseChain, UseCone (applies to next effect)
- Register: Compiles into SpellDefinitionCatalog

Routing

- FeatureRegistry executes effects: Damage, Heal, DOT/HOT, Buffs, AreaEffects
