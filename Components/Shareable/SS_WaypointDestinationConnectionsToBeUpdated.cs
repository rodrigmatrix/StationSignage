using System.Runtime.InteropServices;
using Unity.Entities;

namespace WE_TFM.Components
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SS_WaypointDestinationConnectionsToBeUpdated 
    {
        public Entity untilWaypoint;
        public uint requestFrame;
    }
}
