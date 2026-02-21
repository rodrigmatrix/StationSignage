using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using StationSignage.BridgeWE;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace StationSignage.Systems
{
    public struct LineDescriptor : System.IEquatable<LineDescriptor>
    {
        public Entity Entity;
        public TransportType TransportType;
        public bool IsCargo;
        public bool IsPassenger;
        public FixedString32Bytes Acronym;
        public int Number;
        public UnityEngine.Color Color;

        public LineDescriptor(
            Entity entity,
            TransportType transportType,
            bool isCargo,
            bool isPassenger,
            FixedString32Bytes acronym,
            int number,
            UnityEngine.Color color)
        {
            Entity = entity;
            TransportType = transportType;
            IsCargo = isCargo;
            IsPassenger = isPassenger;
            Acronym = acronym;
            Number = number;
            Color = color;
        }

        public bool Equals(LineDescriptor other)
        {
            return Entity.Equals(other.Entity);
        }

        public override bool Equals(object obj)
        {
            return obj is LineDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Entity.GetHashCode();
        }
    }

    [BurstCompile]
    public struct ExtractLinesJob : IJob
    {
        [ReadOnly] public EntityManager EntityManager;
        [ReadOnly] public Entity SelectedEntity;
        public NativeList<LineDescriptor> Lines;

        public void Execute()
        {
            if (EntityManager.TryGetBuffer(SelectedEntity, true, out DynamicBuffer<ConnectedRoute> routes))
            {
                for (int i = 0; i < routes.Length; i++)
                {
                    var route = routes[i];
                    if (EntityManager.TryGetComponent<Owner>(route.m_Waypoint, out var owner)
                        && EntityManager.TryGetComponent<PrefabRef>(owner.m_Owner, out var prefabRef)
                        && EntityManager.TryGetComponent<TransportLineData>(prefabRef.m_Prefab, out var lineData)
                        && EntityManager.TryGetComponent<Game.Routes.Color>(owner.m_Owner, out var lineColor)
                        && EntityManager.TryGetComponent<RouteNumber>(owner.m_Owner, out var lineNumber))
                    {
                        // Get line acronym - this needs to be done outside burst since it calls managed code
                        var acronym = new FixedString32Bytes();
                        // For now, we'll leave acronym empty in burst context
                        // The acronym will need to be resolved outside of burst

                        var descriptor = new LineDescriptor(
                            owner.m_Owner,
                            lineData.m_TransportType,
                            lineData.m_CargoTransport,
                            lineData.m_PassengerTransport,
                            acronym,
                            lineNumber.m_Number,
                            lineColor.m_Color
                        );

                        Lines.Add(descriptor);
                    }
                }
            }
        }
    }

    public partial class SS_BuildingLineCacheSystem : SystemBase
    {
        public static SS_BuildingLineCacheSystem Instance { get; private set; }

        private readonly Dictionary<Entity, (List<LineDescriptor> desc, int frameCalculated)> m_buildingCacheData = [];
        protected override void OnCreate()
        {
            Instance = this;
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        protected override void OnUpdate()
        {
            // Periodically clean old cache entries
            if (UnityEngine.Time.frameCount % 2400 == 0)
            {
                CleanOldCacheEntries();
            }
        }

        private void CleanOldCacheEntries()
        {
            var keysToRemove = new List<Entity>();
            foreach (var kvp in m_buildingCacheData)
            {
                if (UnityEngine.Time.frameCount - kvp.Value.frameCalculated >= 2400)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                m_buildingCacheData.Remove(key);
            }
        }

        public List<LineDescriptor> GetLines(Entity selectedEntity, bool iterateToOwner)
        {
            var entityManager = EntityManager;

            if (iterateToOwner)
            {
                while (entityManager.TryGetComponent<Owner>(selectedEntity, out var buildingOwner))
                {
                    selectedEntity = buildingOwner.m_Owner;
                }
            }

            if (m_buildingCacheData.TryGetValue(selectedEntity, out var data) && UnityEngine.Time.frameCount - data.frameCalculated < 600)
            {
                return data.desc;
            }

            var lineSet = new HashSet<LineDescriptor>();

            // Schedule burst job to extract lines
            var job = new ExtractLinesJob
            {
                EntityManager = entityManager,
                SelectedEntity = selectedEntity,
                Lines = new NativeList<LineDescriptor>(Allocator.TempJob)
            };

            job.Run();

            // Copy results and resolve acronyms from managed code
            foreach (var line in job.Lines)
            {
                var descriptor = line;
                // Resolve acronym outside of burst
                descriptor.Acronym = WERouteFn.GetTransportLineNumber(line.Entity);
                lineSet.Add(descriptor);
            }
            job.Lines.Dispose();

            // Process sub-objects
            if (entityManager.TryGetBuffer(selectedEntity, true, out DynamicBuffer<Game.Objects.SubObject> subObjects))
            {
                foreach (var subObject in subObjects)
                {
                    var subJob = new ExtractLinesJob
                    {
                        EntityManager = entityManager,
                        SelectedEntity = subObject.m_SubObject,
                        Lines = new NativeList<LineDescriptor>(Allocator.TempJob)
                    };
                    subJob.Run();
                    foreach (var line in subJob.Lines)
                    {
                        var descriptor = line;
                        descriptor.Acronym = WERouteFn.GetTransportLineNumber(line.Entity);
                        lineSet.Add(descriptor);
                    }
                    subJob.Lines.Dispose();
                }
            }

            // Process upgrades
            if (entityManager.TryGetBuffer(selectedEntity, true, out DynamicBuffer<InstalledUpgrade> upgrades))
            {
                foreach (var upgrade in upgrades)
                {
                    var upgradeLines = GetLines(upgrade.m_Upgrade, false);
                    foreach (var line in upgradeLines)
                    {
                        lineSet.Add(line);
                    }
                }
            }

            var lineNumberList = new List<LineDescriptor>(lineSet);
            m_buildingCacheData[selectedEntity] = (lineNumberList, UnityEngine.Time.frameCount);
            return lineNumberList;
        }
    }
}
