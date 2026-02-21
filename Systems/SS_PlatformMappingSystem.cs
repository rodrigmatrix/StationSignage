using Colossal.Entities;
using Game;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.SceneFlow;
using Game.Tools;
using StationSignage.Components;
using StationSignage.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Transform = Game.Objects.Transform;
using TransportStop = Game.Routes.TransportStop;

public partial class SS_PlatformMappingSystem : SystemBase
{

    private EntityQuery m_connectableRoutesNotMapped;
    private EntityQuery m_connectionsUpdatedPlatforms;
    private EntityQuery m_resetWaypointsQuery;
    private EndFrameBarrier m_endFrameBarrier;
    public static uint CacheVersion { get; private set; }
    private byte dirtyCooldown = 0;

    protected override void OnCreate()
    {
        m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
        m_connectableRoutesNotMapped = GetEntityQuery(new EntityQueryDesc[] {
                new() {
                    Any =
                    [
                        ComponentType.ReadOnly<TransportStop>(),
                        ComponentType.ReadOnly<TrainStop>(),
                        ComponentType.ReadOnly<AirplaneStop>(),
                        ComponentType.ReadOnly<BusStop>(),
                        ComponentType.ReadOnly<TramStop>(),
                        ComponentType.ReadOnly<ShipStop>(),
                        ComponentType.ReadOnly<SubwayStop>(),
                    ],
                    All =
                    [
                        ComponentType.ReadOnly<Owner>(),
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<SS_PlatformData>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    ]
                },
                new() {
                    Any =
                    [
                        ComponentType.ReadOnly<TransportStop>(),
                        ComponentType.ReadOnly<TrainStop>(),
                        ComponentType.ReadOnly<AirplaneStop>(),
                        ComponentType.ReadOnly<BusStop>(),
                        ComponentType.ReadOnly<TramStop>(),
                        ComponentType.ReadOnly<ShipStop>(),
                        ComponentType.ReadOnly<SubwayStop>(),
                    ],
                    All =
                    [
                        ComponentType.ReadOnly<Attached>(),
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<SS_PlatformData>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    ]
                }
            }
        );

        m_connectionsUpdatedPlatforms = GetEntityQuery(new EntityQueryDesc[] {
              new() {
                    All =
                    [
                        ComponentType.ReadOnly<SS_PlatformConnectionsUpdated>(),
                        ComponentType.ReadOnly<ConnectedRoute>(),
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    ]
                },
              new() {
                    All =
                    [
                        ComponentType.ReadOnly<ConnectedRoute>(),
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<SS_WaypointDestinationConnections>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    ]
                },
        });

        m_resetWaypointsQuery = GetEntityQuery([
            new EntityQueryDesc
            {
                All = [ComponentType.ReadOnly<SS_WaypointDestinationConnections>()]
            }
        ]);
    }
    public void MarkToResetWaypointsDestinations()
    {
        shallReset = true;
    }

    private bool shallReset;

    private readonly Dictionary<TransportType, int> TransportTypePriority = new()
    {
        [TransportType.Train] = 0,
        [TransportType.Subway] = 0,
        [TransportType.Ship] = 1,
        [TransportType.Airplane] = 1,
        [TransportType.Tram] = 2,
        [TransportType.Bus] = 3,
        [TransportType.Ferry] = 1,
        [TransportType.Taxi] = 4
    };
    protected override void OnUpdate()
    {
        if (GameManager.instance.isGameLoading) return;
        if (--dirtyCooldown == 1)
        {
            CacheVersion++;
        }
        if (shallReset)
        {
            m_endFrameBarrier.CreateCommandBuffer().RemoveComponent<SS_WaypointDestinationConnections>(m_resetWaypointsQuery, EntityQueryCaptureMode.AtPlayback);
            shallReset = false;
        }
        if (!m_connectableRoutesNotMapped.IsEmptyIgnoreFilter)
        {
            using var output = new NativeParallelHashSet<PairEntityRoute>(m_connectableRoutesNotMapped.CalculateEntityCount() * 2, Allocator.Temp);
            var cmdBuffer = m_endFrameBarrier.CreateCommandBuffer();
            Dependency = new StopMappingJob
            {
                m_attachedLookup = GetComponentLookup<Attached>(true),
                m_ownerLookup = GetComponentLookup<Owner>(true),
                m_EntityType = GetEntityTypeHandle(),
                m_cmdBuffer = cmdBuffer.AsParallelWriter(),
                m_hashSet = output.AsParallelWriter(),
                m_transformLkp = GetComponentLookup<Transform>(true)
            }.ScheduleParallel(m_connectableRoutesNotMapped, Dependency);
            Dependency.Complete();
            using var outputArray = output.ToNativeArray(Allocator.Temp);

            var valuesToSet = outputArray.ToArray().GroupBy(x => x.target).ToDictionary(x => x.Key, x => x.Select(x => x.route).ToHashSet());
            Dictionary<TransportType, byte> transportTypeCounter = new();
            foreach (var kvp in valuesToSet)
            {
                var valuesToAdd = kvp.Value;
                DynamicBuffer<SS_PlatformMappingLink> buffer;
                if (EntityManager.TryGetBuffer<SS_PlatformMappingLink>(kvp.Key, true, out var currentBuffer))
                {
                    for (int i = 0; i < currentBuffer.Length; i++)
                    {
                        valuesToAdd.Add(currentBuffer[i]);
                    }
                    buffer = cmdBuffer.SetBuffer<SS_PlatformMappingLink>(kvp.Key);
                    buffer.Clear();
                }
                else
                {
                    buffer = cmdBuffer.AddBuffer<SS_PlatformMappingLink>(kvp.Key);
                }
                var buildingTransform = EntityManager.GetComponentData<Transform>(kvp.Key);

                var matrixTransform = Matrix4x4.TRS(buildingTransform.m_Position, buildingTransform.m_Rotation, Vector3.one).inverse;

                var sortedValues = valuesToAdd
                    .Select(routePlatformData => (routePlatformData, routePlatformData.relativePosition, GetTransportType(routePlatformData.platformData), routePlatformData.isBasePlatform))
                    .OrderBy(item => item.isBasePlatform ? 0 : 1)
                    .ThenBy(item => TransportTypePriority.TryGetValue(item.Item3, out var priority) ? priority : 9999)
                    .ThenByDescending(item => Math.Round(item.relativePosition.y / 8))
                    .ThenByDescending(item => Math.Round(item.relativePosition.z / 4))
                    .ThenBy(item => Math.Round(item.relativePosition.x / 4))
                    .ToList();

                byte counterPlatform = 0;
                byte counterRail = 0;
                transportTypeCounter.Clear();

                foreach (var route in sortedValues)
                {
                    buffer.Add(route.routePlatformData);
                    transportTypeCounter.TryGetValue(route.Item3, out var counter);
                    var data = new SS_PlatformData
                    {
                        type = route.Item3,
                        overallNumber = ++counterPlatform,
                        railsPlatformNumber = route.Item3 == TransportType.Train || route.Item3 == TransportType.Subway ? ++counterRail : (byte)0,
                        transportTypePlatformNumber = ++counter
                    };
                    transportTypeCounter[route.Item3] = counter;
                    cmdBuffer.AddComponent(route.routePlatformData.platformData, data);
                    cmdBuffer.AddComponent<SS_PlatformConnectionsUpdated>(route.routePlatformData.platformData);
                }

            }
        }
        if (!m_connectionsUpdatedPlatforms.IsEmpty)
        {
            new ConnectionsSortingJob
            {
                m_EntityType = GetEntityTypeHandle(),
                cmdBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_connectedRouteLookup = GetBufferLookup<ConnectedRoute>(true),
                m_waypointDestinationsLookup = GetBufferLookup<SS_WaypointDestinationConnections>(true),
                m_routeNumberLookup = GetComponentLookup<RouteNumber>(true)
            }.ScheduleParallel(m_connectionsUpdatedPlatforms, Dependency).Complete();
            dirtyCooldown = 5;

        }
    }


    [BurstCompile]
    private struct ConnectionsSortingJob : IJobChunk
    {
        public EntityTypeHandle m_EntityType;
        public EntityCommandBuffer.ParallelWriter cmdBuffer;
        public BufferLookup<ConnectedRoute> m_connectedRouteLookup;
        public BufferLookup<SS_WaypointDestinationConnections> m_waypointDestinationsLookup;
        public ComponentLookup<RouteNumber> m_routeNumberLookup;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(m_EntityType);

            NativeHashSet<WaypointDestinationSortable> waypointDestinationsSet = new NativeHashSet<WaypointDestinationSortable>(8, Allocator.Temp);
            NativeHashSet<Entity> linesToExclude = new NativeHashSet<Entity>(8, Allocator.Temp);
            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    cmdBuffer.RemoveComponent<SS_PlatformConnectionsUpdated>(unfilteredChunkIndex, entity);
                    if (m_connectedRouteLookup.TryGetBuffer(entity, out var connectedRoutes))
                    {
                        linesToExclude.Clear();
                        waypointDestinationsSet.Clear();
                        for (int j = 0; j < connectedRoutes.Length; j++)
                        {
                            var route = connectedRoutes[j];
                            linesToExclude.Add(route.m_Waypoint);
                            if (m_waypointDestinationsLookup.TryGetBuffer(route.m_Waypoint, out var waypointDestinationsBuffer))
                            {
                                for (int k = 0; k < waypointDestinationsBuffer.Length; k++)
                                {
                                    var waypointDestination = waypointDestinationsBuffer[k];
                                    var transportTypeImportance = waypointDestination.TransportType.ToImportance();
                                    waypointDestinationsSet.Add(new WaypointDestinationSortable(
                                        waypointDestination,
                                        m_routeNumberLookup.TryGetComponent(waypointDestination.line, out var number) ? number.m_Number : int.MaxValue));
                                }
                            }
                        }
                        using var sortedData = waypointDestinationsSet.ToNativeArray(Allocator.Temp);
                        sortedData.Sort();
                        cmdBuffer.RemoveComponent<SS_WaypointDestinationConnections>(unfilteredChunkIndex, entity);
                        var results = cmdBuffer.AddBuffer<SS_WaypointDestinationConnections>(unfilteredChunkIndex, entity);
                        results.Clear();
                        for (int j = 0; j < sortedData.Length; j++)
                        {
                            var waypointDestination = sortedData[j];
                            results.Add(waypointDestination.connection);
                        }
                    }
                }
            }
            finally
            {
                waypointDestinationsSet.Dispose();
            }
        }

        private readonly struct WaypointDestinationSortable(SS_WaypointDestinationConnections connection, int routeNumber)
            : IComparable<WaypointDestinationSortable>, IComparer<WaypointDestinationSortable>, IEquatable<WaypointDestinationSortable>
        {
            public readonly SS_WaypointDestinationConnections connection = connection;
            private readonly int routeNumber = routeNumber;

            private readonly ulong AsULong => ((uint)routeNumber) | ((ulong)connection.Importance << 32) | ((ulong)(connection.isPassenger ? 1 : 0) << 40) | ((ulong)(connection.isCargo ? 1 : 0) << 41);

            public int Compare(WaypointDestinationSortable x, WaypointDestinationSortable y)
            {
                return x.CompareTo(y);
            }

            public int CompareTo(WaypointDestinationSortable y) => AsULong.CompareTo(y.AsULong);

            public bool Equals(WaypointDestinationSortable other) => CompareTo(other) == 0;

            public override int GetHashCode()
            {
                return HashCode.Combine(AsULong);
            }

            public static bool operator ==(WaypointDestinationSortable left, WaypointDestinationSortable right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(WaypointDestinationSortable left, WaypointDestinationSortable right)
            {
                return !(left == right);
            }

            public static bool operator <(WaypointDestinationSortable left, WaypointDestinationSortable right)
            {
                return left.CompareTo(right) < 0;
            }

            public static bool operator >(WaypointDestinationSortable left, WaypointDestinationSortable right)
            {
                return left.CompareTo(right) > 0;
            }

            public static bool operator <=(WaypointDestinationSortable left, WaypointDestinationSortable right)
            {
                return left.CompareTo(right) <= 0;
            }

            public static bool operator >=(WaypointDestinationSortable left, WaypointDestinationSortable right)
            {
                return left.CompareTo(right) >= 0;
            }
        }

    }


    public TransportType GetTransportType(Entity platformData)
    {
        if (EntityManager.HasComponent<TrainStop>(platformData)) return TransportType.Train;
        if (EntityManager.HasComponent<SubwayStop>(platformData)) return TransportType.Subway;
        if (EntityManager.HasComponent<AirplaneStop>(platformData)) return TransportType.Airplane;
        if (EntityManager.HasComponent<BusStop>(platformData)) return TransportType.Bus;
        if (EntityManager.HasComponent<TramStop>(platformData)) return TransportType.Tram;
        if (EntityManager.HasComponent<ShipStop>(platformData)) return TransportType.Ship;
        return TransportType.None;

    }

    private readonly struct PairEntityRoute(Entity target, SS_PlatformMappingLink route) : IEquatable<PairEntityRoute>
    {
        public readonly Entity target = target;
        public readonly SS_PlatformMappingLink route = route;

        public bool Equals(PairEntityRoute other) => target == other.target &&
                   route.platformData == other.route.platformData;

#pragma warning disable IDE0070 // Use 'System.HashCode'
        public override int GetHashCode()
#pragma warning restore IDE0070 // Use 'System.HashCode'
        {
            unchecked
            {
                int hashCode = 17;
                hashCode = (hashCode * 23) + target.Index;
                hashCode = (hashCode * 23) + target.Version;
                hashCode = (hashCode * 23) + route.platformData.Index;
                hashCode = (hashCode * 23) + route.platformData.Version;
                return hashCode;
            }
        }

        public static bool operator ==(PairEntityRoute left, PairEntityRoute right) => left.Equals(right);

        public static bool operator !=(PairEntityRoute left, PairEntityRoute right) => !(left == right);
    }

    [BurstCompile]
    private struct StopMappingJob : IJobChunk
    {
        public EntityTypeHandle m_EntityType;
        public ComponentLookup<Owner> m_ownerLookup;
        public ComponentLookup<Attached> m_attachedLookup;
        public ComponentLookup<Transform> m_transformLkp;
        public EntityCommandBuffer.ParallelWriter m_cmdBuffer;
        public NativeParallelHashSet<PairEntityRoute>.ParallelWriter m_hashSet;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(m_EntityType);

            for (int i = 0; i < entities.Length; i++)
            {
                var ownTransform = m_transformLkp[entities[i]];
                if (m_ownerLookup.TryGetComponent(entities[i], out var owner) && owner.m_Owner != Entity.Null)
                {
                    var isBasePlatform = true;
                    while (m_ownerLookup.TryGetComponent(owner.m_Owner, out var parentOwner))
                    {
                        owner = parentOwner;
                        isBasePlatform = false;
                    }
                    var parentTransform = m_transformLkp[owner.m_Owner];
                    var parentMatrix = Matrix4x4.TRS(parentTransform.m_Position, parentTransform.m_Rotation, Vector3.one).inverse;
                    m_hashSet.Add(new(owner.m_Owner, new SS_PlatformMappingLink(entities[i], ownTransform.m_Position, parentMatrix.MultiplyPoint(ownTransform.m_Position), isBasePlatform)));
                }
                if (m_attachedLookup.TryGetComponent(entities[i], out Attached parent) && parent.m_Parent != Entity.Null)
                {
                    var target = parent.m_Parent;
                    var isBasePlatform = true;
                    while (m_ownerLookup.TryGetComponent(target, out var parentOwner))
                    {
                        target = parentOwner.m_Owner;
                        isBasePlatform = false;
                    }
                    var parentTransform = m_transformLkp[target];
                    var parentMatrix = Matrix4x4.TRS(parentTransform.m_Position, parentTransform.m_Rotation, Vector3.one).inverse;
                    m_hashSet.Add(new(target, new SS_PlatformMappingLink(entities[i], ownTransform.m_Position, parentMatrix.MultiplyPoint(ownTransform.m_Position), isBasePlatform)));
                }
            }

        }
    }
}