using Colossal.Entities;
using Game.Common;
using Game.Objects;
using Unity.Entities;

namespace StationSignage.Utils
{
    public static class EntityUtils
    {
        public static Entity FindTopOwnership(Entity buildingEntity, ref ComponentLookup<Owner> ownerLookup, ref ComponentLookup<Attached> attachedLookup)
        {
        startOwnershipCheck:
            while (ownerLookup.TryGetComponent(buildingEntity, out var parentOwner) && parentOwner.m_Owner != Entity.Null)
            {
                buildingEntity = parentOwner.m_Owner;
            }
            while (attachedLookup.TryGetComponent(buildingEntity, out var parentAttached) && parentAttached.m_Parent != Entity.Null)
            {
                buildingEntity = parentAttached.m_Parent;
                goto startOwnershipCheck;
            }

            return buildingEntity;
        }
        public static Entity FindTopOwnership(Entity buildingEntity, EntityManager em)
        {
        startOwnershipCheck:
            while (em.TryGetComponent<Owner>(buildingEntity, out var parentOwner) && parentOwner.m_Owner != Entity.Null)
            {
                buildingEntity = parentOwner.m_Owner;
            }
            while (em.TryGetComponent<Attached>(buildingEntity, out var parentAttached) && parentAttached.m_Parent != Entity.Null)
            {
                buildingEntity = parentAttached.m_Parent;
                goto startOwnershipCheck;
            }

            return buildingEntity;
        }
    }
}
