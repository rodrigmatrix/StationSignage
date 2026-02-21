using Unity.Entities;
using Unity.Mathematics;

namespace StationSignage.Components
{
    public struct SS_PlatformMappingLink(Entity platformData, float3 absolutePos, float3 relativePos, bool isBasePlatform) : IBufferElementData
    {
        public Entity platformData = platformData;
        public float3 absolutePos = absolutePos;
        public float3 relativePosition = relativePos;
        public bool isBasePlatform = isBasePlatform;

        public override int GetHashCode()
        {
            return platformData.GetHashCode();
        }
    }
}
