using Colossal.Serialization.Entities;
using Game.Prefabs;
using StationSignage.Enums;
using System;
using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_WaypointDestinationConnections : IBufferElementData, ISerializable, IEquatable<SS_WaypointDestinationConnections>
    {
        public Entity line;
        public uint requestFrame;
        private TransportType transportType;
        public bool isCargo;
        public bool isPassenger;

        private const uint CURRENT_VERSION = 0;

        public TransportType TransportType
        {
            readonly get => transportType; set
            {
                transportType = value;
                Importance = value.ToImportance();
            }
        }

        public TransportTypeByImportance Importance { get; private set; }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                throw new System.Exception($"Unsupported version {version} for SS_WaypointDestinationConnections.");
            }
            reader.Read(out line);
            reader.Read(out requestFrame);
            reader.Read(out int transportType);
            this.TransportType = (TransportType)transportType;
            reader.Read(out isCargo);
            reader.Read(out isPassenger);
            Importance = this.TransportType.ToImportance();
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(line);
            writer.Write(requestFrame);
            writer.Write((int)TransportType);
            writer.Write(isCargo);
            writer.Write(isPassenger);
        }

        public override bool Equals(object obj) => obj is SS_WaypointDestinationConnections connections && Equals(connections);

        public bool Equals(SS_WaypointDestinationConnections other) => line.Equals(other.line);

        public override readonly int GetHashCode() => HashCode.Combine(line);

        public static bool operator ==(SS_WaypointDestinationConnections left, SS_WaypointDestinationConnections right) => left.Equals(right);

        public static bool operator !=(SS_WaypointDestinationConnections left, SS_WaypointDestinationConnections right) => !(left == right);
    }
}
