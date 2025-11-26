# Spell Examples

Use these reference snippets to verify DSL/JSON conversions or to sanity-check new features before wiring them into the spell pipeline.

## JSON definition sample

```json
{
  "Id": "spell.arc-beam",
  "School": "Arcane",
  "CastTime": 0.0,
  "Cooldown": 12.0,
  "Range": 35.0,
  "Targeting": "Hostile",
  "Costs": [{ "Resource": "Mana", "Amount": 60 }],
  "Blocks": [
    {
      "Scope": { "Kind": "PrimaryTarget" },
      "Payload": {
        "Kind": "SpawnDot",
        "OverTime": {
          "Id": "arc-beam-dot",
          "MagnitudeOverride": 75,
          "TickIntervalOverride": 1.0,
          "DurationOverride": 4.0
        }
      }
    }
  ]
}
```

## DSL snippet

```
spell fireball {
    school Fire
    manaCost 50
    castTime 2s
    cooldown 6s
    target Hostile
    effect primaryTarget damage {
        amount 320
        variance 10%
        school Fire
    }
    effect radius(target, 4m, enemy) dot {
        id burn
        dps 25
        interval 1s
        duration 6s
    }
}
```

Link these files from gameplay docs rather than duplicating raw JSON inline. That keeps subsystem pages focused on mechanics while this folder tracks content authoring specifics.
