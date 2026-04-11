using StationSignage.Components.Shareable;
using StationSignage.Utils;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace StationSignage.WE_TFMBridge
{
    public static class WE_TFMBuildingLineCacheBridge
    {
        [PatchGenericMethod(typeof(LineDescriptor))]
        public unsafe static List<LineDescriptor> GetLines(Entity selectedEntity, bool iterateToOwner) => throw new NotImplementedException("Stub only!");
    }
}
