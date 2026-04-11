using Colossal.Serialization.Entities;
using Game.Prefabs;
using StationSignage.Enums;
using System;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace StationSignage.Components.Shareable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SS_WaypointDestinationConnections : IBufferElementData,  IEquatable<SS_WaypointDestinationConnections>
    {
        public Entity line;
        public uint requestFrame;
        private TransportType transportType;
        public bool isCargo;
        public bool isPassenger;
        public TransportTypeByImportance Importance { get;  set; }
      

        public override bool Equals(object obj) => obj is SS_WaypointDestinationConnections connections && Equals(connections);

        public bool Equals(SS_WaypointDestinationConnections other) => line.Equals(other.line);

        public override readonly int GetHashCode() => HashCode.Combine(line);

        public static bool operator ==(SS_WaypointDestinationConnections left, SS_WaypointDestinationConnections right) => left.Equals(right);

        public static bool operator !=(SS_WaypointDestinationConnections left, SS_WaypointDestinationConnections right) => !(left == right);
    }
}
