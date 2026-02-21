using Colossal.Serialization.Entities;
using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_VehicleIncomingData : IComponentData, ISerializable
    {
        private const uint CURRENT_VERSION = 0;
        public Entity nextVehicle0;
        public Entity nextVehicle1;
        public Entity nextVehicle2;
        public Entity nextVehicle3;

        public readonly void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(nextVehicle0);
            writer.Write(nextVehicle1);
            writer.Write(nextVehicle2);
            writer.Write(nextVehicle3);
        }
        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                throw new System.Exception($"Unsupported version {version} for {nameof(SS_VehicleIncomingData)}. Current version is {CURRENT_VERSION}.");
            }
            reader.Read(out nextVehicle0);
            reader.Read(out nextVehicle1);
            reader.Read(out nextVehicle2);
            reader.Read(out nextVehicle3);
        }
    }

}
