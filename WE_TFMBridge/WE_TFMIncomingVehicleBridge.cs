using StationSignage.Components.Shareable;
using StationSignage.Utils;
using System;
using Unity.Entities;

namespace StationSignage.WE_TFMBridge
{
    public static class WE_TFMIncomingVehicleBridge
    {
        [PatchGenericMethod(typeof(SS_VehicleIncomingDetailData))]
        public static SS_VehicleIncomingDetailData GetIncomingDetailInformation(Entity platform) => throw new NotImplementedException("Stub only!");
    }
}