using UnityEngine;
using Unity.Entities;

namespace Framework.UnityAuthoring.Authoring
{
    public class DamageableAuthoring : MonoBehaviour
    {
        public int Armor = 0;
        [Range(0f, 0.95f)] public float ResistPercent = 0f;

        #if FRAMEWORK_USE_BAKERS
        class Baker : Unity.Entities.Baker<DamageableAuthoring>
        {
            public override void Bake(DamageableAuthoring a)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Framework.Damage.Components.Damageable
                {
                    Armor = a.Armor,
                    ResistPercent = a.ResistPercent
                });
            }
        }
        #endif
    }
}
