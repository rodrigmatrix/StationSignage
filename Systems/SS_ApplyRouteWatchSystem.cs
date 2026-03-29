using Game.Common;
using Game.Routes;
using Game.Tools;
using StationSignage.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace StationSignage.Systems
{
    public partial class SS_ApplyRouteWatchSystem : SS_BasicSystem
    {

        private EntityQuery m_TempQuery;

        protected override AllowedPhase UpdatePhase => AllowedPhase.ToolUpdate;

        protected override void OnCreateWithBarrier()
        {
            m_TempQuery = base.GetEntityQuery(
        [
                new EntityQueryDesc
                {
                    All =
                    [
                        ComponentType.ReadOnly<Waypoint>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>()
                    ],
                    Any =
                    [
                    ],
                    None = [
                    ]
                }
            ]);
            RequireForUpdate(m_TempQuery);
        }

        protected override void OnUpdate()
        {
            new MarkDirtyConnectionsOnToolApplyJob
            {
                m_cmdBuffer = Barrier.CreateCommandBuffer().AsParallelWriter(),
                m_EntityType = GetEntityTypeHandle(),
                m_TempType = GetComponentLookup<Temp>(true),
                m_WaypointType = GetComponentTypeHandle<Waypoint>(true),
                m_WaypointConnectionData = GetComponentLookup<Connected>(true),
            }.ScheduleParallel(m_TempQuery, Dependency).Complete();
        }
        [BurstCompile]
        private struct MarkDirtyConnectionsOnToolApplyJob : IJobChunk
        {
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Waypoint> nativeArray = chunk.GetNativeArray(ref m_WaypointType);
                NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    Entity entity = nativeArray2[i];
                    if (m_TempType.TryGetComponent(entity, out var temp))
                    {
                        if (m_WaypointConnectionData.TryGetComponent(entity, out var connected) && m_WaypointIncomingData.HasComponent(connected.m_Connected))
                        {
                            m_cmdBuffer.RemoveComponent<SS_VehicleIncomingData>(unfilteredChunkIndex, connected.m_Connected);
                        }
                        if (m_WaypointConnectionData.TryGetComponent(temp.m_Original, out var connected2) && m_WaypointIncomingData.HasComponent(connected2.m_Connected))
                        {

                            m_cmdBuffer.RemoveComponent<SS_VehicleIncomingData>(unfilteredChunkIndex, connected2.m_Connected);
                        }
                        m_cmdBuffer.AddComponent<SS_WaypointDestinationConnectionsDirtyPre>(unfilteredChunkIndex, entity);
                        m_cmdBuffer.AddComponent<SS_WaypointDestinationConnectionsDirtyPre>(unfilteredChunkIndex, temp.m_Original);
                    }
                }
            }
            public EntityCommandBuffer.ParallelWriter m_cmdBuffer;
            [ReadOnly] public EntityTypeHandle m_EntityType;
            [ReadOnly] public ComponentLookup<Temp> m_TempType;
            [ReadOnly] public ComponentTypeHandle<Waypoint> m_WaypointType;
            [ReadOnly] public ComponentLookup<Connected> m_WaypointConnectionData;
            [ReadOnly] public ComponentLookup<SS_VehicleIncomingData> m_WaypointIncomingData;
        }
    }
}