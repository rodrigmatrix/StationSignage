using Colossal.Entities;
using Game;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Vehicles;
using StationSignage.Components;
using StationSignage.Systems;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Mathematics;

namespace StationSignage.Formulas
{
    public partial class SS_IncomingVehicleSystem : GameSystemBase
    {
        public uint CurrentFrame => m_simulationSystem.frameIndex >> 2;
        private SimulationSystem m_simulationSystem;
        private EndFrameBarrier m_endFrameBarrier;
        private EntityQuery m_dirtyTvInfoVehicles;
        public static SS_IncomingVehicleSystem Instance { get; private set; }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 2;

        public SS_VehicleTvData GetTvInformation(Entity platform)
        {
            EntityManager.TryGetComponent(platform, out SS_VehicleIncomingData vehicleData);
            if (vehicleData.nextVehicle0 == Entity.Null)
            {
                return GetDefaultPlatformData(platform);
            }
            var found = EntityManager.TryGetComponent<SS_VehicleTvData>(vehicleData.nextVehicle0, out var data);
            if (!found || !data.IsValid())
            {
                m_endFrameBarrier.CreateCommandBuffer().AddComponent<SS_VehicleTvDataDirty>(vehicleData.nextVehicle0);
            }
            return !found ? GetDefaultPlatformData(platform) : data;
        }

        private SS_VehicleTvData GetDefaultPlatformData(Entity platform)
        {
            var hasData = EntityManager.TryGetComponent<SS_VehicleTvData>(platform, out var tvData);
            if (!hasData || !tvData.IsValid())
            {
                EntityManager.TryGetBuffer<ConnectedRoute>(platform, true, out var routes);
                if (routes.Length == 0)
                {
                    tvData = new SS_VehicleTvData(
                    VehicleStatusDescription.ClosedPlatform,
                    VehicleStatusDescription.ClosedPlatform,
                    0,
                    CurrentFrame);
                }
                else
                {
                    var totalAverage = 0;
                    for (var i = 0; i < routes.Length; i++)
                    {
                        if (EntityManager.TryGetComponent(routes[i].m_Waypoint, out WaitingPassengers waitingPassengers))
                        {
                            totalAverage += waitingPassengers.m_AverageWaitingTime;
                        }
                    }
                    tvData = new SS_VehicleTvData(
                    VehicleStatusDescription.NextTrain,
                    VehicleStatusDescription.AverageWaitTime,
                    (ushort)(totalAverage / routes.Length),
                    CurrentFrame);
                }
                if (!hasData)
                {
                    m_endFrameBarrier.CreateCommandBuffer().AddComponent(platform, tvData);
                }
                else
                {
                    m_endFrameBarrier.CreateCommandBuffer().SetComponent(platform, tvData);
                }
            }
            return tvData;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            m_simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_dirtyTvInfoVehicles = GetEntityQuery(new EntityQueryDesc[] {
                new()
                {
                    All = [
                        ComponentType.ReadOnly<SS_VehicleTvDataDirty>(),
                        ComponentType.ReadOnly<PathInformation>(),
                        ComponentType.ReadOnly<PrefabRef>(),
                        ComponentType.ReadOnly<Passenger>(),
                        ComponentType.ReadOnly<PathElement>(),
                        ComponentType.ReadOnly<PathOwner>(),
                    ]
                }
            });
        }

        private readonly Queue<Action> runOnMain = [];

        protected override void OnUpdate()
        {
            while (runOnMain.TryDequeue(out var item))
            {
                item();
            }
            if (!m_dirtyTvInfoVehicles.IsEmpty)
            {
                new VehicleTvDataUpdater
                {
                    entityType = GetEntityTypeHandle(),
                    prefabRefLookup = GetComponentLookup<PrefabRef>(true),
                    pathInformationLookup = GetComponentLookup<PathInformation>(true),
                    pathOwnerLookup = GetComponentLookup<PathOwner>(true),
                    passengerLookup = GetBufferLookup<Passenger>(true),
                    pathElementLookup = GetBufferLookup<PathElement>(true),
                    layoutElementLookup = GetBufferLookup<LayoutElement>(true),
                    cmdBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    publicTransportVehicleDataLookup = GetComponentLookup<PublicTransportVehicleData>(true),
                    frame = CurrentFrame,
                    tvDataLookup = GetComponentLookup<SS_VehicleTvData>(true),
                    publicTransportLookup = GetComponentLookup<Game.Vehicles.PublicTransport>(true),
                    m_carLaneLookup = GetComponentLookup<CarCurrentLane>(true),
                    m_waterLaneLookup = GetComponentLookup<WatercraftCurrentLane>(true),
                    m_airLaneLookup = GetComponentLookup<AircraftCurrentLane>(true),
                    m_trainLaneLookup = GetComponentLookup<TrainCurrentLane>(true),
                    m_curveLookup = GetComponentLookup<Curve>(true)
                }.ScheduleParallel(m_dirtyTvInfoVehicles, Dependency).Complete();
            }
        }

        public enum VehicleStatusDescription
        {
            TrainOnPlatform,
            BoardingNow,
            PrepareForBoarding,
            NextTrain,
            DistanceToStation,
            AverageWaitTime,
            ClosedPlatform
        }

        [BurstCompile]
        private struct VehicleTvDataUpdater : IJobChunk
        {
            public EntityTypeHandle entityType;
            public ComponentLookup<PrefabRef> prefabRefLookup;
            public ComponentLookup<PathInformation> pathInformationLookup;
            public ComponentLookup<PathOwner> pathOwnerLookup;
            public ComponentLookup<SS_VehicleTvData> tvDataLookup;
            public BufferLookup<Passenger> passengerLookup;
            public BufferLookup<PathElement> pathElementLookup;
            public BufferLookup<LayoutElement> layoutElementLookup;
            public EntityCommandBuffer.ParallelWriter cmdBuffer;
            public ComponentLookup<PublicTransportVehicleData> publicTransportVehicleDataLookup;
            public ComponentLookup<Game.Vehicles.PublicTransport> publicTransportLookup;
            public uint frame;
            public ComponentLookup<CarCurrentLane> m_carLaneLookup;
            public ComponentLookup<WatercraftCurrentLane> m_waterLaneLookup;
            public ComponentLookup<AircraftCurrentLane> m_airLaneLookup;
            public ComponentLookup<TrainCurrentLane> m_trainLaneLookup;
            public ComponentLookup<Curve> m_curveLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    SS_VehiclePathWatchSystem.CalculateDistance(
                            ref pathInformationLookup, ref pathElementLookup, ref m_carLaneLookup,
                            ref m_waterLaneLookup, ref m_airLaneLookup, ref m_trainLaneLookup, ref m_curveLookup,
                            entity, out float distance, out _
                            );
                    var result = new SS_VehicleTvData
                    {
                        distanceHm = (ushort)math.clamp(distance / 100, 0, ushort.MaxValue),
                    };
                    switch (result.distanceHm)
                    {
                        case 0:
                            if ((publicTransportLookup[entity].m_State & PublicTransportFlags.Boarding) != 0)
                            {
                                result.title = VehicleStatusDescription.TrainOnPlatform;
                                result.subtitle = VehicleStatusDescription.BoardingNow;
                            }
                            else
                            {
                                result.title = VehicleStatusDescription.TrainOnPlatform;
                                result.subtitle = VehicleStatusDescription.PrepareForBoarding;
                            }
                            break;
                        default:
                            result.title = VehicleStatusDescription.NextTrain;
                            result.subtitle = VehicleStatusDescription.DistanceToStation;
                            break;
                    }
                    if (layoutElementLookup.TryGetBuffer(entity, out var layoutElements))
                    {
                        for (var j = 0; j < layoutElements.Length; j++)
                        {
                            if (j > 7) break;
                            var layoutElement = layoutElements[j];
                            if (publicTransportVehicleDataLookup.TryGetComponent(prefabRefLookup[layoutElement.m_Vehicle].m_Prefab, out var data))
                            {
                                var usage = passengerLookup[layoutElement.m_Vehicle].Length * 255 / data.m_PassengerCapacity;
                                result[j] = (byte)math.clamp(usage, 0, 255);
                            }
                        }
                    }
                    else
                    {
                        result.totalCars = 1;
                        if (publicTransportVehicleDataLookup.TryGetComponent(prefabRefLookup[entity].m_Prefab, out var data))
                        {
                            var usage = passengerLookup[entity].Length * 255 / data.m_PassengerCapacity;
                            result[0] = (byte)math.clamp(usage, 0, 255);
                        }
                    }
                    result.cacheFrame = frame;
                    if (tvDataLookup.HasComponent(entity))
                    {
                        cmdBuffer.SetComponent(unfilteredChunkIndex, entity, result);
                    }
                    else
                    {
                        cmdBuffer.AddComponent(unfilteredChunkIndex, entity, result);
                    }
                    cmdBuffer.RemoveComponent<SS_VehicleTvDataDirty>(unfilteredChunkIndex, entity);
                }
            }
        }
    }




}
