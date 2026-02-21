using Colossal.Serialization.Entities;
using StationSignage.Formulas;
using Unity.Entities;
using static StationSignage.Formulas.SS_IncomingVehicleSystem;

namespace StationSignage.Components
{
    public struct SS_VehicleTvData : IComponentData, ISerializable
    {
        public VehicleStatusDescription title;
        public VehicleStatusDescription subtitle;
        public ushort distanceHm;// * 100m or sec waiting
        public byte totalCars = 8;
        public byte occupancyLevels0;
        public byte occupancyLevels1;
        public byte occupancyLevels2;
        public byte occupancyLevels3;
        public byte occupancyLevels4;
        public byte occupancyLevels5;
        public byte occupancyLevels6;
        public byte occupancyLevels7;
        public uint cacheFrame;

        public byte this[int index]
        {
            readonly get
            {
                return index switch
                {
                    0 => occupancyLevels0,
                    1 => occupancyLevels1,
                    2 => occupancyLevels2,
                    3 => occupancyLevels3,
                    4 => occupancyLevels4,
                    5 => occupancyLevels5,
                    6 => occupancyLevels6,
                    7 => occupancyLevels7,
                    _ => throw new System.IndexOutOfRangeException("Index out of range for occupancy levels.")
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        occupancyLevels0 = value;
                        break;
                    case 1:
                        occupancyLevels1 = value;
                        break;
                    case 2:
                        occupancyLevels2 = value;
                        break;
                    case 3:
                        occupancyLevels3 = value;
                        break;
                    case 4:
                        occupancyLevels4 = value;
                        break;
                    case 5:
                        occupancyLevels5 = value;
                        break;
                    case 6:
                        occupancyLevels6 = value;
                        break;
                    case 7:
                        occupancyLevels7 = value;
                        break;
                    default:
                        throw new System.IndexOutOfRangeException("Index out of range for occupancy levels.");
                }
            }
        }

        public const uint CURRENT_VERSION = 0;

        public SS_VehicleTvData(VehicleStatusDescription title, VehicleStatusDescription subtitle, ushort distanceHm, uint frame) : this()
        {
            this.title = title;
            this.subtitle = subtitle;
            this.distanceHm = distanceHm;
            cacheFrame = frame;
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                throw new System.Exception($"Unsupported version {version} for {nameof(SS_VehicleTvData)}. Current version is {CURRENT_VERSION}.");
            }
            reader.Read(out int title);
            this.title = (VehicleStatusDescription)title;
            reader.Read(out int subtitle);
            this.subtitle = (VehicleStatusDescription)subtitle;
            reader.Read(out distanceHm);
            reader.Read(out totalCars);
            reader.Read(out occupancyLevels0);
            reader.Read(out occupancyLevels1);
            reader.Read(out occupancyLevels2);
            reader.Read(out occupancyLevels3);
            reader.Read(out occupancyLevels4);
            reader.Read(out occupancyLevels5);
            reader.Read(out occupancyLevels6);
            reader.Read(out occupancyLevels7);
            reader.Read(out cacheFrame);
        }

        public readonly bool IsValid() => cacheFrame == SS_IncomingVehicleSystem.Instance.CurrentFrame;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write((int)title);
            writer.Write((int)subtitle);
            writer.Write(distanceHm);
            writer.Write(totalCars);
            writer.Write(occupancyLevels0);
            writer.Write(occupancyLevels1);
            writer.Write(occupancyLevels2);
            writer.Write(occupancyLevels3);
            writer.Write(occupancyLevels4);
            writer.Write(occupancyLevels5);
            writer.Write(occupancyLevels6);
            writer.Write(occupancyLevels7);
            writer.Write(cacheFrame);
        }
    }

}
