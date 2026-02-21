using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Routes;
using Game.SceneFlow;
using Game.Tools;
using StationSignage.BridgeWE;
using StationSignage.Components;
using StationSignage.Utils;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace StationSignage.Systems
{
    public partial class SS_WaypointConnectionsSystem : GameSystemBase, IDefaultSerializable
    {
        private ModificationBarrier1 m_ModificationBarrier1;
        private EntityQuery m_PathReadyQuery;
        private EntityQuery m_unmappedOrPreDirtyStops;
        private EntityQuery m_dirtyRoutes;
        private EntityQuery m_dirtyStops;
        private ushort modificationIdx;
        private static SS_WaypointConnectionsSystem Instance;
        public static ushort ConnectionsVersion => Instance.modificationIdx;

        internal void IncreaseVersion() => modificationIdx++;

        private const uint CURRENT_VERSION = 0;
        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                throw new System.Exception($"Unsupported version {version} for {nameof(SS_WaypointConnectionsSystem)}.");
            }
            reader.Read(out modificationIdx);
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 4;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(modificationIdx);
        }

        public void SetDefaults(Context context)
        {
            modificationIdx = 0;
        }

        protected override void OnCreate()
        {
            Instance = this;
            m_ModificationBarrier1 = World.GetOrCreateSystemManaged<ModificationBarrier1>();
            m_PathReadyQuery = GetEntityQuery([
                ComponentType.ReadOnly<Event>(),
                ComponentType.ReadOnly<PathUpdated>()
            ]);
            m_unmappedOrPreDirtyStops = GetEntityQuery(new EntityQueryDesc[]
            {
                new()
                {
                    All = [
                        ComponentType.ReadOnly<Waypoint>(),
                        ComponentType.ReadOnly<Connected>()
                    ],
                    None = [
                        ComponentType.ReadOnly<SS_WaypointDestinationConnections>(),
                        ComponentType.ReadOnly<SS_WaypointDestinationConnectionsToBeUpdated>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    ]
                },
                new()
                {
                    All = [
                        ComponentType.ReadOnly<Waypoint>(),
                        ComponentType.ReadOnly<Connected>(),
                        ComponentType.ReadOnly<SS_WaypointDestinationConnectionsDirtyPre>()
                    ],
                    None = [
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    ]
                },
            });
            m_dirtyRoutes = GetEntityQuery(new EntityQueryDesc[] {
                new()
                {
                    All = [
                        ComponentType.ReadOnly<RouteWaypoint>(),
                        ComponentType.ReadOnly<SS_WaypointDestinationConnectionsDirtyPre>()
                    ],
                    None = [
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    ]
                }
            });
            m_dirtyStops = GetEntityQuery(new EntityQueryDesc[]
            {
                new()
                {
                    All = [
                        ComponentType.ReadOnly<Waypoint>(),
                        ComponentType.ReadOnly<Connected>(),
                        ComponentType.ReadOnly<SS_WaypointDestinationConnectionsToBeUpdated>(),
                    ],
                    None = [
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    ]
                }
            });
            RequireAnyForUpdate(m_dirtyStops, m_dirtyRoutes, m_PathReadyQuery, m_unmappedOrPreDirtyStops);
        }
        protected override void OnUpdate()
        {
            if (GameManager.instance.isGameLoading) return;
            if (!m_PathReadyQuery.IsEmpty)
            {
                new RouteModifiedJob
                {
                    m_PathUpdatedType = GetComponentTypeHandle<PathUpdated>(true),
                    m_routeWaypointLookup = GetBufferLookup<RouteWaypoint>(true),
                    m_OwnerLookup = GetComponentLookup<Owner>(true),
                    m_routeLookup = GetComponentLookup<Route>(true),
                    m_cmdBuffer = m_ModificationBarrier1.CreateCommandBuffer().AsParallelWriter(),
                    frameRequest = ++modificationIdx
                }.ScheduleParallel(m_PathReadyQuery, Dependency).Complete();
            }
            if (!m_dirtyRoutes.IsEmpty)
            {
                new RouteConnectionModifiedJob
                {
                    m_entityTypeHandle = GetEntityTypeHandle(),
                    m_routeLookup = GetComponentLookup<Route>(true),
                    m_routeWaypointLookup = GetBufferLookup<RouteWaypoint>(true),
                    m_cmdBuffer = m_ModificationBarrier1.CreateCommandBuffer().AsParallelWriter(),
                    frameRequest = modificationIdx
                }.ScheduleParallel(m_dirtyRoutes, Dependency).Complete();
            }
            if (!m_unmappedOrPreDirtyStops.IsEmpty)
            {
                new UnmappedRouteWaypointsJob
                {
                    entityTypeHandle = GetEntityTypeHandle(),
                    m_cmdBuffer = m_ModificationBarrier1.CreateCommandBuffer().AsParallelWriter(),
                    frameRequest = modificationIdx
                }.ScheduleParallel(m_unmappedOrPreDirtyStops, Dependency).Complete();
            }
            if (!m_dirtyStops.IsEmpty)
            {
                new DirtyRouteWaypointUpdate
                {
                    entityTypeHandle = GetEntityTypeHandle(),
                    m_cmdBuffer = m_ModificationBarrier1.CreateCommandBuffer().AsParallelWriter(),
                    frameRequest = modificationIdx,
                    m_ownerLookup = GetComponentLookup<Owner>(true),
                    m_routeWaypointLookup = GetBufferLookup<RouteWaypoint>(true),
                    m_connectedLookup = GetComponentLookup<Connected>(true),
                    m_connectedRouteLookup = GetBufferLookup<ConnectedRoute>(true),
                    m_dirtyLookup = GetComponentLookup<SS_WaypointDestinationConnectionsToBeUpdated>(true),
                    m_attachedLookup = GetComponentLookup<Attached>(true),
                    m_waypointLookup = GetComponentLookup<Waypoint>(true),
                    m_platformsLinkLookup = GetBufferLookup<SS_PlatformMappingLink>(true),
                    m_ConnectionsLookup = GetBufferLookup<SS_WaypointDestinationConnections>(true),
                    m_statusesLookup = GetComponentLookup<SS_LineStatus>(true)

                }.ScheduleParallel(m_dirtyStops, Dependency).Complete();
            }
        }
        //Can't be bursted: Harmony patching needed to work
        private struct RouteModifiedJob : IJobChunk
        {
            public ComponentTypeHandle<PathUpdated> m_PathUpdatedType;

            public ComponentLookup<Owner> m_OwnerLookup;
            public ComponentLookup<Route> m_routeLookup;
            public BufferLookup<RouteWaypoint> m_routeWaypointLookup;

            public EntityCommandBuffer.ParallelWriter m_cmdBuffer;
            public uint frameRequest;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {

                var pathUpdatedArray = chunk.GetNativeArray(ref m_PathUpdatedType);
                for (int i = 0; i < pathUpdatedArray.Length; i++)
                {
                    PathUpdated pathUpdated = pathUpdatedArray[i];
                    if (m_OwnerLookup.TryGetComponent(pathUpdated.m_Owner, out var owner) && m_routeWaypointLookup.TryGetBuffer(owner.m_Owner, out var waypoints))
                    {
                        for (int j = 0; j < waypoints.Length; j++)
                        {
                            m_cmdBuffer.RemoveComponent<SS_WaypointDestinationConnectionsToBeUpdated>(unfilteredChunkIndex, waypoints[j].m_Waypoint);
                            m_cmdBuffer.AddComponent(unfilteredChunkIndex, waypoints[j].m_Waypoint, new SS_WaypointDestinationConnectionsToBeUpdated
                            {
                                requestFrame = frameRequest,
                                untilWaypoint = WERouteFn.GetWaypointStaticDestinationEntity(waypoints[j].m_Waypoint)
                            });
                        }
                    }
                }
            }
        }
        //Can't be bursted: Harmony patching needed to work
        private struct RouteConnectionModifiedJob : IJobChunk
        {
            public EntityTypeHandle m_entityTypeHandle;

            public ComponentLookup<Route> m_routeLookup;
            public BufferLookup<RouteWaypoint> m_routeWaypointLookup;

            public EntityCommandBuffer.ParallelWriter m_cmdBuffer;
            public uint frameRequest;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {

                var entityArray = chunk.GetNativeArray(m_entityTypeHandle);

                for (int i = 0; i < entityArray.Length; i++)
                {
                    var entity = entityArray[i];
                    if (m_routeWaypointLookup.TryGetBuffer(entity, out var waypoints))
                    {
                        for (int j = 0; j < waypoints.Length; j++)
                        {
                            m_cmdBuffer.RemoveComponent<SS_WaypointDestinationConnectionsToBeUpdated>(unfilteredChunkIndex, waypoints[j].m_Waypoint);
                            m_cmdBuffer.AddComponent(unfilteredChunkIndex, waypoints[j].m_Waypoint, new SS_WaypointDestinationConnectionsToBeUpdated
                            {
                                requestFrame = frameRequest,
                                untilWaypoint = WERouteFn.GetWaypointStaticDestinationEntity(waypoints[j].m_Waypoint)
                            });
                        }
                    }
                    m_cmdBuffer.RemoveComponent<SS_WaypointDestinationConnectionsDirtyPre>(unfilteredChunkIndex, entity);
                }
            }
        }
//Can't be bursted: Harmony patching needed to work
        private struct UnmappedRouteWaypointsJob : IJobChunk
        {
            public EntityTypeHandle entityTypeHandle;
            public EntityCommandBuffer.ParallelWriter m_cmdBuffer;
            public uint frameRequest;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entityArray = chunk.GetNativeArray(entityTypeHandle);
                for (int i = 0; i < entityArray.Length; i++)
                {
                    var entity = entityArray[i];
                    m_cmdBuffer.RemoveComponent<SS_WaypointDestinationConnectionsToBeUpdated>(unfilteredChunkIndex, entity);
                    m_cmdBuffer.AddComponent(unfilteredChunkIndex, entity, new SS_WaypointDestinationConnectionsToBeUpdated
                    {
                        requestFrame = frameRequest,
                        untilWaypoint = WERouteFn.GetWaypointStaticDestinationEntity(entity)
                    });
                    m_cmdBuffer.RemoveComponent<SS_WaypointDestinationConnectionsDirtyPre>(unfilteredChunkIndex, entity);
                }
            }
        }
        [BurstCompile]
        private struct DirtyRouteWaypointUpdate : IJobChunk
        {
            public EntityTypeHandle entityTypeHandle;
            public EntityCommandBuffer.ParallelWriter m_cmdBuffer;
            public uint frameRequest;
            public ComponentLookup<Owner> m_ownerLookup;
            public ComponentLookup<Attached> m_attachedLookup;
            public BufferLookup<RouteWaypoint> m_routeWaypointLookup;
            public ComponentLookup<Waypoint> m_waypointLookup;
            public ComponentLookup<Connected> m_connectedLookup;
            public BufferLookup<ConnectedRoute> m_connectedRouteLookup;
            public ComponentLookup<SS_WaypointDestinationConnectionsToBeUpdated> m_dirtyLookup;
            public BufferLookup<SS_WaypointDestinationConnections> m_ConnectionsLookup;
            public BufferLookup<SS_PlatformMappingLink> m_platformsLinkLookup;
            public ComponentLookup<SS_LineStatus> m_statusesLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entityArray = chunk.GetNativeArray(entityTypeHandle);
                NativeHashMap<Entity, SS_WaypointDestinationConnections> mappedLines = new(25, Allocator.Temp);
                try
                {
                    for (int i = 0; i < entityArray.Length;)
                    {
                        var entity = entityArray[i];
                        var dirtyParams = m_dirtyLookup[entity];
                        if (dirtyParams.requestFrame != frameRequest)
                        {
                            m_cmdBuffer.RemoveComponent<SS_WaypointDestinationConnectionsToBeUpdated>(unfilteredChunkIndex, entity);
                            m_cmdBuffer.AddComponent<SS_WaypointDestinationConnectionsDirtyPre>(unfilteredChunkIndex, entity);
                            goto continueMainLoop;
                        }
                        if (m_ConnectionsLookup.TryGetBuffer(entity, out var connections) && connections.Length > 0 && connections[0].requestFrame == frameRequest)
                        {
                            m_cmdBuffer.RemoveComponent<SS_WaypointDestinationConnectionsToBeUpdated>(unfilteredChunkIndex, entity);
                            goto continueMainLoop;
                        }
                        if (m_ownerLookup.TryGetComponent(entity, out var owner) &&
                            m_routeWaypointLookup.TryGetBuffer(owner.m_Owner, out var waypoints))
                        {
                            var lineOwner = owner.m_Owner;
                            var start = m_waypointLookup[entity].m_Index + 1;
                            var end = m_waypointLookup[dirtyParams.untilWaypoint].m_Index;
                            if (end < start) end += waypoints.Length;

                            mappedLines.Clear();
                            for (int j = start; j <= end; j++)
                            {
                                var waypoint = waypoints[j % waypoints.Length].m_Waypoint;
                                if (m_connectedLookup.TryGetComponent(waypoint, out var connectedPlatform))
                                {
                                    if (connectedPlatform.m_Connected == Entity.Null)
                                    {
                                        var buffer = m_cmdBuffer.AddBuffer<SS_WaypointDestinationConnections>(unfilteredChunkIndex, entity);
                                        buffer.Clear();
                                        m_cmdBuffer.RemoveComponent<SS_WaypointDestinationConnectionsToBeUpdated>(unfilteredChunkIndex, entity);
                                        goto continueMainLoop;
                                    }
                                    var buildingEntity = EntityUtils.FindTopOwnership(connectedPlatform.m_Connected, ref m_ownerLookup, ref m_attachedLookup);
                                    if (m_platformsLinkLookup.TryGetBuffer(buildingEntity, out var platformLinks))
                                    {
                                        for (int l = 0; l < platformLinks.Length; l++)
                                        {
                                            var refPlatform = platformLinks[l].platformData;
                                            IterateConnectedRoutes(ref mappedLines, ref m_statusesLookup, lineOwner, refPlatform);
                                            m_cmdBuffer.AddComponent<SS_PlatformConnectionsUpdated>(unfilteredChunkIndex, refPlatform);
                                        }
                                    }
                                    else
                                    {
                                        if (!IterateConnectedRoutes(ref mappedLines, ref m_statusesLookup, lineOwner, buildingEntity))
                                        {
                                            goto continueMainLoop;
                                        }
                                        m_cmdBuffer.AddComponent<SS_PlatformConnectionsUpdated>(unfilteredChunkIndex, buildingEntity);
                                    }
                                }
                            }
                            {
                                var buffer = m_cmdBuffer.AddBuffer<SS_WaypointDestinationConnections>(unfilteredChunkIndex, entity);
                                buffer.Clear();
                                using var finalConnections = mappedLines.GetKeyArray(Allocator.Temp);
                                for (int k = 0; k < finalConnections.Length; k++)
                                {
                                    var value = mappedLines[finalConnections[k]];
                                    value.requestFrame = frameRequest;
                                    buffer.Add(value);
                                }
                                m_cmdBuffer.RemoveComponent<SS_WaypointDestinationConnectionsToBeUpdated>(unfilteredChunkIndex, entity);
                            }
                        }
                    continueMainLoop:
                        i++;
                    }
                }
                finally
                {
                    mappedLines.Dispose();
                }

            }

            private bool IterateConnectedRoutes(ref NativeHashMap<Entity, SS_WaypointDestinationConnections> mappedLines, ref ComponentLookup<SS_LineStatus> statusLookup, Entity lineOwner, Entity refPlatform)
            {
                if (m_connectedRouteLookup.TryGetBuffer(refPlatform, out var connectedRoutes))
                {
                    for (int m = 0; m < connectedRoutes.Length; m++)
                    {
                        if (m_ownerLookup.TryGetComponent(connectedRoutes[m].m_Waypoint, out var ownerLineConnected)
                            && ownerLineConnected.m_Owner != lineOwner
                            && !mappedLines.ContainsKey(ownerLineConnected.m_Owner))
                        {
                            statusLookup.TryGetComponent(ownerLineConnected.m_Owner, out var status);
                            mappedLines[ownerLineConnected.m_Owner] = new()
                            {
                                isCargo = status.isCargo,
                                isPassenger = status.isPassenger,
                                TransportType = status.type,
                                line = ownerLineConnected.m_Owner
                            };
                        }
                    }
                    return true;
                }

                return false;
            }
        }
    }
}
