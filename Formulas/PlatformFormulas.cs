using Colossal.Entities;
using Game.Common;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using StationSignage.Components;
using StationSignage.Components.Shareable;
using StationSignage.Enums;
using StationSignage.Systems;
using StationSignage.Utils;
using StationSignage.WE_TFMBridge;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Color = UnityEngine.Color;

namespace StationSignage.Formulas
{
    public static class PlatformFormulas
    {
        private static EntityManager? entityManager;

        private static EntityManager EntityManager
        {
            get
            {
                if (!entityManager.HasValue)
                {
                    entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                }
                return entityManager.Value;
            }
        }

        public static Entity GetPlatform(Entity building, Dictionary<string, string> vars)
        {
            if (WE_TFMComponentGetterBridge.TryGetComponent(building, out SS_PlatformData _)) return building;
            building = EntityUtils.FindTopOwnership(building, EntityManager);
            var targetPlatform = vars.TryGetValue(LinesFormulas.PLATFORM_VAR, out var platformStr);
            if (!targetPlatform) return Entity.Null;
            if (platformStr.StartsWith("#"))
            {
                vars.TryGetValue(platformStr[1..], out var newPlatVal);
                platformStr = newPlatVal;
            }
            if (platformStr != null && byte.TryParse(platformStr, out var platform))
            {

                if (WE_TFMComponentGetterBridge.TryGetBuffer(building, out DynamicBuffer<SS_PlatformMappingLink> buffer) && platform <= buffer.Length)
                {
                    return buffer[(int)(platform - 1)].platformData;
                }
            }
            return Entity.Null; // Return null if no matching platform is found
        }
        //StationSignage:_Plat_StationNameSimple
        public static int? GetPlatformInt(Dictionary<string, string> vars)
        {
            if (!vars.TryGetValue("vPlatform", out var platformString))
            {
                vars.TryGetValue("platform", out platformString);
            }
            var platformPosition = vars.GetValueOrDefault("platformPosition");
            platformString = platformPosition switch
            {
                "left" => vars.GetValueOrDefault("platformLeft"),
                "right" => vars.GetValueOrDefault("platformRight"),
                _ => platformString
            };
            int? platformInt = null;
            if (platformString != null)
            {
                platformInt = int.Parse(platformString);
            }
            return platformInt;
        }

        public static Entity GetIncomingTrainDestinationForPlatform(Entity platform)
        {
            if (WE_TFMComponentGetterBridge.TryGetComponent(platform, out SS_VehicleIncomingOrderData buffer))
            {
                if (buffer.nextVehicle0 != Entity.Null)
                {
                    return EntityManager.TryGetComponent<PathInformation>(buffer.nextVehicle0, out var pathInfo)
                        ? pathInfo.m_Destination
                        : default;
                }
            }
            return EntityManager.TryGetBuffer<ConnectedRoute>(platform, true, out var connectedRoutes) && connectedRoutes.Length > 0
                ? connectedRoutes[0].m_Waypoint
                : Entity.Null;
        }

        public static Entity GetFirstLineOrDefault(Entity platformStop)
        {
            return EntityManager.TryGetBuffer<ConnectedRoute>(platformStop, true, out var buffer) && buffer.Length != 0
                ? EntityManager.GetComponentData<Owner>(buffer[0].m_Waypoint).m_Owner
                : Entity.Null;
        }

        public static SS_WaypointDestinationConnections GetDestinationByIdx(Entity platformStop, Dictionary<string, string> vars)
        {
            return vars.TryGetValue(LinesFormulas.CURRENT_INDEX_VAR, out var idxStr) && byte.TryParse(idxStr, out var idx)
                ? GetPlatformConnections(platformStop, vars).ElementAtOrDefault(idx)
                : default;
        }

        public static List<SS_WaypointDestinationConnections> GetPlatformConnections(Entity platformStop, Dictionary<string, string> vars)
        {
            if (WE_TFMPlatformMappingBridge.GetCacheVersion() != connectionsVersionCache)
            {
                _cachedConnections.Clear();
                connectionsVersionCache = WE_TFMPlatformMappingBridge.GetCacheVersion();
            }
            TransportTypeByImportance lowestPriority = TransportTypeByImportance.LessPrioritary;
            TransportTypeByImportance highestPriority = TransportTypeByImportance.MostPrioritary;
            if (vars.TryGetValue("lowPr", out var filterStr) && Enum.TryParse<TransportTypeByImportance>(filterStr, out var parsedVal))
            {
                lowestPriority = parsedVal;
            }
            if (vars.TryGetValue("highPr", out filterStr) && Enum.TryParse(filterStr, out parsedVal))
            {
                highestPriority = parsedVal;
            }
            if (lowestPriority < highestPriority) (lowestPriority, highestPriority) = (highestPriority, lowestPriority);
            if (!_cachedConnections.TryGetValue((platformStop, lowestPriority, highestPriority), out var connections))
            {
                HashSet<Entity> excludedLines = [];
                var owner = EntityUtils.FindTopOwnership(platformStop, EntityManager);
                if (WE_TFMComponentGetterBridge.TryGetBuffer(owner, out DynamicBuffer<SS_PlatformMappingLink> platformData))
                {
                    for (int i = 0; i < platformData.Length; i++)
                    {
                        var platform = platformData[i].platformData;
                        if (EntityManager.TryGetBuffer<ConnectedRoute>(platform, true, out var routesConn))
                        {
                            for (int j = 0; j < routesConn.Length; j++)
                            {
                                if (EntityManager.TryGetComponent<Owner>(routesConn[j].m_Waypoint, out var passingLine))
                                {
                                    excludedLines.Add(passingLine.m_Owner);
                                }
                            }
                        }
                    }
                }
                if (EntityManager.TryGetBuffer<ConnectedRoute>(owner, true, out var routes))
                {
                    for (int j = 0; j < routes.Length; j++)
                    {
                        if (EntityManager.TryGetComponent<Owner>(routes[j].m_Waypoint, out var passingLine))
                        {
                            excludedLines.Add(passingLine.m_Owner);
                        }
                    }
                }
                WE_TFMComponentGetterBridge.TryGetBuffer(platformStop, out DynamicBuffer<SS_WaypointDestinationConnections> connectionsBuffer);
                connections = [];
                for (int i = 0; i < connectionsBuffer.Length; i++)
                {
                    var connection = connectionsBuffer[i];
                    if (!excludedLines.Contains(connection.line) && connection.Importance <= lowestPriority && connection.Importance >= highestPriority && !connection.isCargo)
                    {
                        connections.Add(connection);
                    }
                }
                _cachedConnections[(platformStop, lowestPriority, highestPriority)] = connections;
            }
            return connections;
        }

        public static Entity GetStationTransferIdx(Entity building, Dictionary<string, string> vars)
        {
            vars.TryGetValue("$idx", out var idxStr);
            int.TryParse(idxStr, out var idx);
            return GetStationTransfers(building, vars).ElementAtOrDefault(idx);
        }

        public static List<Entity> GetStationTransfers(Entity building, Dictionary<string, string> vars)
        {
            if (WE_TFMComponentGetterBridge.TryGetComponent(building, out SS_PlatformData _)) return [];
            building = EntityUtils.FindTopOwnership(building, EntityManager);
            var platformInt = GetPlatformInt(vars);
            var transfers = new List<Entity>();
            if (!WE_TFMComponentGetterBridge.TryGetBuffer(building, out DynamicBuffer<SS_PlatformMappingLink> buffer)) return transfers;
            for (var i = 0; i < buffer.Length; i++)
            {
                var platformData = buffer[i].platformData;
                var hasLine = GetFirstLineOrDefault(platformData) != Entity.Null;
                if (platformInt != null && platformInt != i && hasLine)
                {
                    transfers.Add(platformData);
                }
            }

            return transfers;
        }

        private static Dictionary<(Entity platform, TransportTypeByImportance low, TransportTypeByImportance high), List<SS_WaypointDestinationConnections>> _cachedConnections = [];
        private static uint connectionsVersionCache = 0;


        public static ServiceOperator GetFirstLineOperatorOrDefault(Entity platformStop)
        {
            bool isMetro = EntityManager.HasComponent<SubwayStop>(platformStop);
            return !EntityManager.TryGetBuffer<ConnectedRoute>(platformStop, true, out var buffer) || buffer.Length == 0
                ? SS_SettingSystem.Instance.LineOperatorCity switch
                {
                    Settings.LineOperatorCityOptions.Generic => ServiceOperator.Default,
                    Settings.LineOperatorCityOptions.SaoPaulo => isMetro ? ServiceOperator.Operator01 : EntityManager.HasComponent<TrainStop>(platformStop) ? ServiceOperator.Operator05 : ServiceOperator.Default,
                    Settings.LineOperatorCityOptions.NewYork => isMetro ? ServiceOperator.MTAOperator : ServiceOperator.Default,
                    Settings.LineOperatorCityOptions.London => isMetro ? ServiceOperator.UndergroundOperator : ServiceOperator.Default,
                    _ => ServiceOperator.Default
                }
                : SS_SettingSystem.Instance.LineOperatorCity switch
                {
                    Settings.LineOperatorCityOptions.Generic => ServiceOperator.Default,
                    Settings.LineOperatorCityOptions.SaoPaulo => GetSaoPauloOperator(
                        WE_TFMComponentGetterBridge.TryGetComponent(EntityManager.GetComponentData<Owner>(buffer[0].m_Waypoint).m_Owner, out SS_LineStatus status) ? status.type : default,
                        EntityManager.GetComponentData<RouteNumber>(EntityManager.GetComponentData<Owner>(buffer[0].m_Waypoint).m_Owner).m_Number
                        ),
                    Settings.LineOperatorCityOptions.NewYork => isMetro ? ServiceOperator.MTAOperator : ServiceOperator.Default,
                    Settings.LineOperatorCityOptions.London => isMetro ? ServiceOperator.UndergroundOperator : ServiceOperator.Default,
                    _ => ServiceOperator.Default
                };
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


        public static Color GetMainStationColor(Entity buildingRef, Dictionary<string, string> vars)
        {
            var lineList = WE_TFMBuildingLineCacheBridge.GetLines(buildingRef, true);
            if (lineList == null || lineList.Count != 1) return Color.white;
            return lineList[0].Color;
        }
    }
}
