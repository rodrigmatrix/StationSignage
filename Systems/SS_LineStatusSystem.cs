using Game;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Game.Settings;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Game.Vehicles;
using StationSignage.Components;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace StationSignage.Systems
{
    public partial class SS_LineStatusSystem : GameSystemBase
    {
        internal static SS_LineStatusSystem Instance { get; private set; }
        public uint GameSimulationFrame => _simulationSystem.frameIndex;

        private SimulationSystem _simulationSystem;
        private EntityQuery _linesRequiringUpdateQuery;
        private EntityQuery _linesStatusesQuery;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 8;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            _simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();            
            _linesStatusesQuery = GetEntityQuery(new EntityQueryDesc[] {
                new() {
                    All =
                    [
                        ComponentType.ReadOnly<SS_LineStatus>(),
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    ]
                }
            });
            _linesRequiringUpdateQuery = GetEntityQuery(new EntityQueryDesc[] {
                new() {
                    All =
                    [
                        ComponentType.ReadOnly<Route>(),
                        ComponentType.ReadOnly<RouteNumber>(),
                        ComponentType.ReadOnly<TransportLine>(),
                        ComponentType.ReadOnly<RouteWaypoint>(),
                        ComponentType.ReadOnly<PrefabRef>(),
                        ComponentType.ReadOnly<SS_LineStatus>(),
                        ComponentType.ReadOnly<SS_DirtyTransportLine>()
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    ]
                },
                new() {
                    All =
                    [
                        ComponentType.ReadOnly<Route>(),
                        ComponentType.ReadOnly<RouteNumber>(),
                        ComponentType.ReadOnly<TransportLine>(),
                        ComponentType.ReadOnly<RouteWaypoint>(),
                        ComponentType.ReadOnly<PrefabRef>(),
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<SS_LineStatus>(),
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    ]
                }
            });
        }

        protected override void OnUpdate()
        {
            if (!_linesRequiringUpdateQuery.IsEmptyIgnoreFilter)
            {
                new LineStatusUpdateJob
                {
                    entityType = GetEntityTypeHandle(),
                    routeType = GetComponentTypeHandle<Route>(true),
                    routeVehicleLookup = GetBufferLookup<RouteVehicle>(true),
                    passengerLookup = GetBufferLookup<Passenger>(true),
                    numberLookup = GetComponentLookup<RouteNumber>(true),
                    prefabRefLookup = GetComponentLookup<PrefabRef>(true),
                    transportLineLookup = GetComponentLookup<TransportLineData>(true),
                    cmdBuffer = World.GetOrCreateSystemManaged<EndFrameBarrier>().CreateCommandBuffer().AsParallelWriter()
                }.ScheduleParallel(_linesRequiringUpdateQuery, Dependency).Complete();
            }

        }


        private readonly Dictionary<(TransportType, bool, bool), List<Entity>> CachedCityLines = [];
        private uint cacheFrame;

        public List<Entity> GetCityLines(TransportType tt, bool acceptCargo, bool acceptPassenger)
        {
            if (cacheFrame != GameSimulationFrame >> 5)
            {
                CachedCityLines.Clear();
                cacheFrame = GameSimulationFrame >> 5;
            }
            if (!CachedCityLines.TryGetValue((tt, acceptCargo, acceptPassenger), out var result))
            {
                var response = new NativeList<LineStatusGetJobResponseItem>(_linesStatusesQuery.CalculateEntityCount(), Allocator.Temp);
                try
                {
                    new LineStatusGetJob
                    {
                        response = response.AsParallelWriter(),
                        entityType = GetEntityTypeHandle(),
                        routeNumberType = GetComponentTypeHandle<RouteNumber>(true),
                        statusLookup = GetComponentTypeHandle<SS_LineStatus>(true),
                        accepted = tt,
                        acceptCargo = acceptCargo,
                        acceptPassenger = acceptPassenger
                    }.ScheduleParallel(_linesStatusesQuery, Dependency).Complete();
                    result = CachedCityLines[(tt, acceptCargo, acceptPassenger)] = [.. response.AsArray()
                    .OrderBy(x=> x.status.type)
                    .ThenBy(x => x.route.m_Number)
                    .Select(x=>x.entity)];
                }
                finally
                {
                    response.Dispose();
                }
            }
            return result;
        }

        private struct LineStatusGetJobResponseItem
        {
            public Entity entity;
            public SS_LineStatus status;
            public RouteNumber route;
        }
        [BurstCompile]
        private struct LineStatusGetJob : IJobChunk
        {
            public NativeList<LineStatusGetJobResponseItem>.ParallelWriter response;
            public EntityTypeHandle entityType;
            public ComponentTypeHandle<RouteNumber> routeNumberType;
            public ComponentTypeHandle<SS_LineStatus> statusLookup;
            public TransportType accepted;
            public bool acceptCargo;
            public bool acceptPassenger;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(entityType);
                var routeNumbers = chunk.GetNativeArray(ref routeNumberType);
                var statuses = chunk.GetNativeArray(ref statusLookup);
                for (int i = 0; i < entities.Length; i++)
                {
                    var status = statuses[i];
                    if ((accepted == TransportType.None || status.type == accepted) &&
                       acceptCargo == status.isCargo &&
                       acceptPassenger == status.isPassenger)
                    {
                        var entity = entities[i];

                        response.AddNoResize(new LineStatusGetJobResponseItem
                        {
                            entity = entity,
                            status = status,
                            route = routeNumbers[i]
                        });
                    }
                }
            }
        }
        [BurstCompile]
        private struct LineStatusUpdateJob : IJobChunk
        {
            public EntityTypeHandle entityType;
            public ComponentTypeHandle<Route> routeType;
            public BufferLookup<RouteVehicle> routeVehicleLookup;
            public BufferLookup<Passenger> passengerLookup;
            public ComponentLookup<RouteNumber> numberLookup;
            public ComponentLookup<PrefabRef> prefabRefLookup;
            public ComponentLookup<TransportLineData> transportLineLookup;
            public EntityCommandBuffer.ParallelWriter cmdBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(entityType);
                var routes = chunk.GetNativeArray(ref routeType);
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var route = routes[i];

                    var tlData = transportLineLookup[prefabRefLookup[entity]];
                    var lineType = tlData.m_TransportType;

                    var newStatus = new SS_LineStatus
                    {
                        actualInterval = 1,
                        expectedInterval = 1,
                        lineOperationStatus = GetOperationStatus(entity, route),
                        operatorSP = GetSaoPauloOperator(lineType, numberLookup[entity].m_Number),
                        type = lineType,
                        isCargo = tlData.m_CargoTransport,
                        isPassenger = tlData.m_PassengerTransport
                    };
                    cmdBuffer.AddComponent(unfilteredChunkIndex, entity, newStatus);
                    cmdBuffer.RemoveComponent<SS_DirtyTransportLine>(unfilteredChunkIndex, entity);
                }
            }

            private static ServiceOperator GetSaoPauloOperator(TransportType transportType, int routeNumber)
                => transportType switch
                {
                    TransportType.Subway => routeNumber switch
                    {
                        4 => ServiceOperator.Operator03,
                        5 or 8 or 9 or 15 => ServiceOperator.Operator02,
                        6 => ServiceOperator.Operator04,
                        _ => ServiceOperator.Operator01
                    },
                    TransportType.Train => routeNumber switch
                    {
                        5 or 8 or 9 or 15 => ServiceOperator.Operator02,
                        _ => ServiceOperator.Operator05
                    },
                    _ => ServiceOperator.Default
                };

            private LineOperationStatus GetOperationStatus(Entity e, Route route)
            {
                if (RouteUtils.CheckOption(route, RouteOption.Inactive))
                {
                    return LineOperationStatus.NotOperating;
                }
                var vehicles = routeVehicleLookup[e];
                if (vehicles.Length == 0)
                {
                    return LineOperationStatus.OperationStopped;
                }

                if (vehicles.Length == 1)
                {
                    return LineOperationStatus.ReducedSpeed;
                }

                for (int i = 0; i < vehicles.Length; i++)
                {
                    if (passengerLookup[vehicles[i].m_Vehicle].Length != 0)
                    {
                        return LineOperationStatus.NormalOperation;
                    }
                }

                return LineOperationStatus.NoUsage;
            }
        }
    }
}