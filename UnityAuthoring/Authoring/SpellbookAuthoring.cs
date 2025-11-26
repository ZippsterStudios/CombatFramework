using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace Framework.UnityAuthoring.Authoring
{
    public class SpellbookAuthoring : MonoBehaviour
    {
        [Tooltip("Spell IDs to grant on bake.")]
        public List<string> Spells = new List<string>();

        #if FRAMEWORK_USE_BAKERS
        class Baker : Unity.Entities.Baker<SpellbookAuthoring>
        {
            public override void Bake(SpellbookAuthoring a)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buf = AddBuffer<Framework.Spells.Spellbook.Components.SpellSlot>(entity);
                for (int i = 0; i < a.Spells.Count; i++)
                {
                    var s = a.Spells[i] ?? string.Empty;
                    buf.Add(new Framework.Spells.Spellbook.Components.SpellSlot
                    {
                        SpellId = new FixedString64Bytes(s)
                    });
                }
            }
        }
        #endif
    }
}
