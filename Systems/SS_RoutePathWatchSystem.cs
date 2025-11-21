using Game.Common;
using Game.Pathfind;
using Game.Routes;
using StationSignage.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Entities;

namespace StationSignage.Systems
{
    public partial class SS_RoutePathWatchSystem : SystemBase
    {
        private EntityQuery m_PathReadyQuery;
        private ModificationBarrier1 m_ModificationBarrier1;
        protected override void OnCreate()
        {
            m_ModificationBarrier1 = World.GetOrCreateSystemManaged<ModificationBarrier1>();
            m_PathReadyQuery = GetEntityQuery(new EntityQueryDesc[] {
             new(){
                 All =[
                     ComponentType.ReadOnly<Event>()
                 ],
                 Any =[
                    ComponentType.ReadOnly<PathUpdated>(),
                    ComponentType.ReadOnly<PathTargetMoved>(),
                 ]
            }
            });
        }

        protected override void OnUpdate()
        {
            if (m_PathReadyQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var job = new RoutePathReadyJob();
            job.m_OwnerLookup = GetComponentLookup<Owner>(true);
            job.m_routeLookup = GetComponentLookup<Route>(true);
            job.m_cmdBuffer = m_ModificationBarrier1.CreateCommandBuffer().AsParallelWriter();
            job.m_connectedLookup = GetComponentLookup<Connected>(true);
            job.m_entityTypeHandle = GetEntityTypeHandle();
            job.m_PathUpdatedLookup = GetComponentLookup<PathUpdated>(true);
            job.m_PathTargetMovedLookup = GetComponentLookup<PathTargetMoved>(true);
            job.ScheduleParallel(m_PathReadyQuery, Dependency).Complete();
        }
        [BurstCompile]
        private struct RoutePathReadyJob : IJobChunk
        {
            public EntityTypeHandle m_entityTypeHandle;
            public ComponentLookup<PathUpdated> m_PathUpdatedLookup;
            public ComponentLookup<PathTargetMoved> m_PathTargetMovedLookup;
            public ComponentLookup<Owner> m_OwnerLookup;
            public ComponentLookup<Route> m_routeLookup;
            public ComponentLookup<Connected> m_connectedLookup;

            public EntityCommandBuffer.ParallelWriter m_cmdBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {

                var entities = chunk.GetNativeArray(m_entityTypeHandle);
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    if (m_PathUpdatedLookup.TryGetComponent(entity, out var pathUpdated)
                        && m_OwnerLookup.TryGetComponent(pathUpdated.m_Owner, out var owner)
                        && m_routeLookup.HasComponent(owner.m_Owner))
                    {
                        m_cmdBuffer.AddComponent<SS_DirtyTransportLine>(unfilteredChunkIndex, owner.m_Owner);
                        if (m_connectedLookup.TryGetComponent(pathUpdated.m_Owner, out var connected))
                        {
                            m_cmdBuffer.AddComponent<SS_WaypointDestinationConnectionsDirtyPre>(unfilteredChunkIndex, pathUpdated.m_Owner);
                            m_cmdBuffer.RemoveComponent<SS_VehicleIncomingData>(unfilteredChunkIndex, connected.m_Connected);
                        }
                    }
                    if (m_PathTargetMovedLookup.TryGetComponent(entity, out var pathMoved)
                        && m_OwnerLookup.TryGetComponent(pathMoved.m_Target, out owner)
                        && m_routeLookup.HasComponent(owner.m_Owner))
                    {
                        m_cmdBuffer.AddComponent<SS_DirtyTransportLine>(unfilteredChunkIndex, owner.m_Owner);
                        if (m_connectedLookup.TryGetComponent(pathMoved.m_Target, out var connected))
                        {
                            m_cmdBuffer.AddComponent<SS_WaypointDestinationConnectionsDirtyPre>(unfilteredChunkIndex, pathMoved.m_Target);
                            m_cmdBuffer.RemoveComponent<SS_VehicleIncomingData>(unfilteredChunkIndex, connected.m_Connected);
                        }
                    }
                }
            }
        }
    }
}