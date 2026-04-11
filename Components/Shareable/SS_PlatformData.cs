using Game.Prefabs;
using Unity.Entities;

namespace StationSignage.Components.Shareable
{
    public struct SS_PlatformData 
    {
        public byte overallNumber;
        public TransportType type;
        public byte transportTypePlatformNumber;
        public byte railsPlatformNumber;
    }
}
