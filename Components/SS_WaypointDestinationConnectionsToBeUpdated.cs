using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_WaypointDestinationConnectionsToBeUpdated : IComponentData
    {
        public Entity untilWaypoint;
        public uint requestFrame;
    }
}
