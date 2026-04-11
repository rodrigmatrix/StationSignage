using StationSignage.Components.Shareable;
using StationSignage.Utils;
using System;
using Unity.Entities;
using WE_TFM.Components;

namespace StationSignage.WE_TFMBridge
{
    public static class WE_TFMComponentGetterBridge
    {

        [PatchGenericMethod("TryGetComponent_DirtyVehicle", typeof(SS_DirtyVehicle))] public static bool TryGetComponent(Entity target, out SS_DirtyVehicle component) => throw new NotImplementedException("Stub only!");
        [PatchGenericMethod("TryGetComponent_PlatformData", typeof(SS_PlatformData))] public static bool TryGetComponent(Entity target, out SS_PlatformData component) => throw new NotImplementedException("Stub only!");
        [PatchGenericMethod("TryGetComponent_VehicleIncomingDetailData", typeof(SS_LineStatus))] public static bool TryGetComponent(Entity target, out SS_LineStatus component) => throw new NotImplementedException("Stub only!");
        [PatchGenericMethod("TryGetComponent_VehicleIncomingOrderData", typeof(SS_VehicleIncomingDetailData))] public static bool TryGetComponent(Entity target, out SS_VehicleIncomingDetailData component) => throw new NotImplementedException("Stub only!");
        [PatchGenericMethod("TryGetComponent_LineStatus", typeof(SS_VehicleIncomingOrderData))] public static bool TryGetComponent(Entity target, out SS_VehicleIncomingOrderData component) => throw new NotImplementedException("Stub only!");
        [PatchGenericMethod("TryGetComponent_WaypointDestinationConnectionsToBeUpdated", typeof(SS_WaypointDestinationConnectionsToBeUpdated))] public static bool TryGetComponent(Entity target, out SS_WaypointDestinationConnectionsToBeUpdated component) => throw new NotImplementedException("Stub only!");
        [PatchGenericMethod("TryGetComponent_PlatformMappingLink", typeof(SS_PlatformMappingLink))] public static bool TryGetBuffer(Entity target, out DynamicBuffer<SS_PlatformMappingLink> buffer) => throw new NotImplementedException("Stub only!");
        [PatchGenericMethod("TryGetComponent_WaypointDestinationConnections", typeof(SS_WaypointDestinationConnections))] public static bool TryGetBuffer(Entity target, out DynamicBuffer<SS_WaypointDestinationConnections> component) => throw new NotImplementedException("Stub only!");


    }
}
