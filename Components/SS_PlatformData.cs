using Game.Prefabs;
using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_PlatformData : IComponentData
    {
        public byte overallNumber;
        public TransportType type;
        public byte transportTypePlatformNumber;
        public byte railsPlatformNumber;
    }
}
